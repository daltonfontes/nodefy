using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;

public static class InviteEndpoints
{
    public record CreateInviteRequest(string Email, string Role);
    public record InviteResponse(string InviteUrl, string Token, DateTimeOffset ExpiresAt);
    public record InviteInfoResponse(string WorkspaceName, string Role);
    public record AcceptInviteResponse(Guid WorkspaceId);
    private static readonly string[] AllowedRoles = ["admin", "member"];

    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        // WORK-02: admin creates invite -> returns shareable URL
        app.MapPost("/workspaces/{id:guid}/invites",
            async (Guid id, CreateInviteRequest req, AppDbContext db, IConfiguration cfg, CurrentUserAccessor caller, ITenantService tenant) =>
        {
            tenant.SetTenant(id);
            if (!AllowedRoles.Contains(req.Role)) return Results.BadRequest(new { error = "Role must be 'admin' or 'member'" });
            if (!await WorkspaceEndpoints.IsAdmin(db, id, caller.UserId)) return Results.Forbid();

            // Cryptographically secure 32-byte token (256-bit entropy)
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
            var invite = new Invitation
            {
                Id = Guid.NewGuid(),
                TenantId = id,
                Email = req.Email,
                Role = req.Role,
                Token = token,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),  // A1 in RESEARCH.md — 7 days
                CreatedAt = DateTimeOffset.UtcNow,
            };
            await db.Invitations.AddAsync(invite);
            await db.SaveChangesAsync();
            var frontendUrl = cfg["FRONTEND_URL"] ?? "http://localhost:3000";
            return Results.Created($"/invites/{token}",
                new InviteResponse($"{frontendUrl}/invite/{token}", token, invite.ExpiresAt));
        }).RequireAuthorization();

        // GET invite info (no auth required — public lookup for landing page)
        app.MapGet("/invites/{token}", async (string token, AppDbContext db) =>
        {
            // Cross-tenant lookup by token — only place IgnoreQueryFilters() is allowed in production
            var invite = await db.Invitations.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Token == token);
            if (invite is null) return Results.NotFound(new { error = "Convite não encontrado" });
            if (invite.ExpiresAt < DateTimeOffset.UtcNow) return Results.StatusCode(410);
            if (invite.AcceptedAt is not null) return Results.Conflict(new { error = "Convite já aceito" });
            var ws = await db.Workspaces.FirstOrDefaultAsync(w => w.Id == invite.TenantId);
            if (ws is null) return Results.NotFound();
            return Results.Ok(new InviteInfoResponse(ws.Name, invite.Role));
        });

        // WORK-03: accept invite (auth required — caller becomes a member)
        app.MapPost("/invites/{token}/accept", async (string token, AppDbContext db, CurrentUserAccessor caller) =>
        {
            if (caller.UserId is null) return Results.Unauthorized();
            var invite = await db.Invitations.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Token == token);
            if (invite is null) return Results.NotFound(new { error = "Convite não encontrado" });
            if (invite.ExpiresAt < DateTimeOffset.UtcNow) return Results.StatusCode(410);  // Pitfall 6
            if (invite.AcceptedAt is not null) return Results.Conflict(new { error = "Convite já aceito" });

            // Idempotency: don't add a duplicate member if one already exists
            var existing = await db.WorkspaceMembers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.TenantId == invite.TenantId && m.UserId == caller.UserId);
            if (existing is null)
            {
                db.WorkspaceMembers.Add(new WorkspaceMember
                {
                    Id = Guid.NewGuid(),
                    TenantId = invite.TenantId,
                    UserId = caller.UserId.Value,
                    Role = invite.Role,
                    JoinedAt = DateTimeOffset.UtcNow,
                });
            }
            invite.AcceptedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new AcceptInviteResponse(invite.TenantId));
        }).RequireAuthorization();

        return app;
    }
}
