using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Lib;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public record CreateWorkspaceRequest(string Name);
    public record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    public record UpdateSettingsRequest(string Currency);
    private static readonly string[] AllowedCurrencies = ["BRL", "USD", "EUR"];

    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/workspaces").RequireAuthorization();

        // WORK-01: Create workspace + add caller as admin member
        group.MapPost("/", async (CreateWorkspaceRequest req, AppDbContext db, CurrentUserAccessor user) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length < 2)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório (mínimo 2 caracteres)"] });
            if (user.UserId is null) return Results.Unauthorized();

            var ws = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                Slug = await UniqueSlug(db, Slug.Generate(req.Name)),
                Currency = "BRL",                  // D-03
                CurrencyLocked = false,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await using var tx = await db.Database.BeginTransactionAsync();
            db.Workspaces.Add(ws);
            await db.SaveChangesAsync(); // workspace must exist before member FK can reference it

            // SET LOCAL scopes app.current_tenant to this transaction so the RLS WITH CHECK
            // on workspace_members passes. ws.Id is a Guid — no injection risk.
            await db.Database.ExecuteSqlRawAsync($"SET LOCAL app.current_tenant = '{ws.Id}'");

            db.WorkspaceMembers.Add(new WorkspaceMember
            {
                Id = Guid.NewGuid(),
                TenantId = ws.Id,
                UserId = user.UserId.Value,
                Role = "admin",
                JoinedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Results.Created($"/workspaces/{ws.Id}",
                new WorkspaceDto(ws.Id, ws.Name, ws.Slug, ws.Currency, ws.CurrencyLocked, "admin"));
        });

        // GET workspaces caller is a member of
        group.MapGet("/", async (AppDbContext db, CurrentUserAccessor user) =>
        {
            if (user.UserId is null) return Results.Unauthorized();
            // Cross-tenant query: must use IgnoreQueryFilters because the user belongs to multiple tenants
            // and the global filter has no tenant context here. Safe because filter is by user_id.
            var rows = await db.WorkspaceMembers.IgnoreQueryFilters()
                .Where(m => m.UserId == user.UserId)
                .Join(db.Workspaces, m => m.TenantId, w => w.Id,
                      (m, w) => new WorkspaceDto(w.Id, w.Name, w.Slug, w.Currency, w.CurrencyLocked, m.Role))
                .ToListAsync();
            return Results.Ok(rows);
        });

        // PATCH /workspaces/{id}/settings — D-05: admin changes currency. D-04: 409 if locked.
        group.MapPatch("/{id:guid}/settings",
            async (Guid id, UpdateSettingsRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            tenant.SetTenant(id);  // ensure RLS sees the tenant for this request
            if (!AllowedCurrencies.Contains(req.Currency))
                return Results.BadRequest(new { error = $"Currency must be one of {string.Join(", ", AllowedCurrencies)}" });
            if (!await IsAdmin(db, id, user.UserId)) return Results.Forbid();
            var ws = await db.Workspaces.FirstOrDefaultAsync(w => w.Id == id);
            if (ws is null) return Results.NotFound();
            if (ws.CurrencyLocked) return Results.Conflict(new { error = "Currency is locked after the first card is created" });
            ws.Currency = req.Currency;
            await db.SaveChangesAsync();
            return Results.Ok(new WorkspaceDto(ws.Id, ws.Name, ws.Slug, ws.Currency, ws.CurrencyLocked, "admin"));
        });

        return app;
    }

    private static async Task<string> UniqueSlug(AppDbContext db, string baseSlug)
    {
        var slug = baseSlug;
        var n = 1;
        while (await db.Workspaces.AnyAsync(w => w.Slug == slug))
            slug = $"{baseSlug}-{++n}";
        return slug;
    }

    internal static async Task<bool> IsAdmin(AppDbContext db, Guid workspaceId, Guid? userId)
    {
        if (userId is null) return false;
        return await db.WorkspaceMembers.IgnoreQueryFilters()
            .AnyAsync(m => m.TenantId == workspaceId && m.UserId == userId && m.Role == "admin");
    }
}
