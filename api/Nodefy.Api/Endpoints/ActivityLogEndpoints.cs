using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;

public static class ActivityLogEndpoints
{
    public record ActivityLogDto(Guid Id, string Action, string Payload, Guid ActorId, DateTimeOffset CreatedAt);

    public static IEndpointRouteBuilder MapActivityLogEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /cards/{cardId}/activity — returns activity log for a card, ordered by created_at ASC
        app.MapGet("/cards/{cardId:guid}/activity",
            async (Guid cardId, AppDbContext db, ITenantService tenant) =>
        {
            // Resolve tenant from card
            var card = await db.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cardId);
            if (card is null) return Results.NotFound();

            tenant.SetTenant(card.TenantId);

            var logs = await db.ActivityLogs
                .Where(a => a.CardId == cardId)
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ActivityLogDto(a.Id, a.Action, a.Payload, a.ActorId, a.CreatedAt))
                .ToListAsync();

            return Results.Ok(logs);
        }).RequireAuthorization();

        return app;
    }

    /// <summary>
    /// Adds an activity log entry for a card action.
    /// Caller MUST call SaveChangesAsync() after this — never called here.
    /// </summary>
    internal static void LogActivity(AppDbContext db, Guid tenantId, Guid cardId,
        Guid actorId, string action, object payload)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CardId = cardId,
            ActorId = actorId,
            Action = action,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        // Caller MUST call SaveChangesAsync() — never call it here
    }
}
