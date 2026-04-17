using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;

public static class MemberEndpoints
{
    public record MemberDto(Guid UserId, string? Name, string Email, string? AvatarUrl, string Role, DateTimeOffset JoinedAt);
    public record UpdateRoleRequest(string Role);
    private static readonly string[] AllowedRoles = ["admin", "member"];

    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workspaces/{id:guid}/members").RequireAuthorization();

        // WORK-04: list members (admin-only)
        group.MapGet("/", async (Guid id, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            tenant.SetTenant(id);
            if (!await WorkspaceEndpoints.IsAdmin(db, id, user.UserId)) return Results.Forbid();
            var rows = await db.WorkspaceMembers
                .Join(db.Users, m => m.UserId, u => u.Id,
                      (m, u) => new MemberDto(u.Id, u.Name, u.Email, u.AvatarUrl, m.Role, m.JoinedAt))
                .ToListAsync();
            return Results.Ok(rows);
        });

        // WORK-05: change role (admin-only); refuse demoting last admin
        group.MapPatch("/{userId:guid}",
            async (Guid id, Guid userId, UpdateRoleRequest req, AppDbContext db, CurrentUserAccessor caller, ITenantService tenant) =>
        {
            tenant.SetTenant(id);
            if (!AllowedRoles.Contains(req.Role)) return Results.BadRequest(new { error = "Role must be 'admin' or 'member'" });
            if (!await WorkspaceEndpoints.IsAdmin(db, id, caller.UserId)) return Results.Forbid();
            var member = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member is null) return Results.NotFound();
            // Last-admin guard
            if (member.Role == "admin" && req.Role != "admin")
            {
                var adminCount = await db.WorkspaceMembers.CountAsync(m => m.Role == "admin");
                if (adminCount <= 1) return Results.Conflict(new { error = "Cannot demote the last admin" });
            }
            member.Role = req.Role;
            await db.SaveChangesAsync();
            return Results.Ok(new { member.UserId, member.Role });
        });

        // WORK-06: remove member (admin-only); refuse removing self
        group.MapDelete("/{userId:guid}",
            async (Guid id, Guid userId, AppDbContext db, CurrentUserAccessor caller, ITenantService tenant) =>
        {
            tenant.SetTenant(id);
            if (!await WorkspaceEndpoints.IsAdmin(db, id, caller.UserId)) return Results.Forbid();
            if (caller.UserId == userId) return Results.Conflict(new { error = "Cannot remove yourself; transfer admin role first" });
            var member = await db.WorkspaceMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member is null) return Results.NotFound();
            db.WorkspaceMembers.Remove(member);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
