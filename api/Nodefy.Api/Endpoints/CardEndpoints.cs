using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Lib;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;

public static class CardEndpoints
{
    public record CreateCardRequest(
        string Title, Guid StageId,
        string? Description, decimal? MonetaryValue,
        Guid? AssigneeId, DateTimeOffset? CloseDate);

    public record UpdateCardRequest(
        string? Title, string? Description,
        decimal? MonetaryValue, Guid? AssigneeId, DateTimeOffset? CloseDate);

    public record MoveCardRequest(Guid TargetStageId, double? PrevPosition, double? NextPosition);

    public record CardDto(
        Guid Id, string Title, string? Description, decimal? MonetaryValue,
        Guid StageId, Guid PipelineId, Guid? AssigneeId, DateTimeOffset? CloseDate,
        DateTimeOffset StageEnteredAt, double Position, DateTimeOffset? ArchivedAt,
        DateTimeOffset CreatedAt);

    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /pipelines/{pipelineId}/cards — create card (member-level)
        app.MapPost("/pipelines/{pipelineId:guid}/cards",
            async (Guid pipelineId, CreateCardRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            // Resolve tenant via pipeline (use IgnoreQueryFilters to bootstrap tenant)
            var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == pipelineId);
            if (pipeline is null) return Results.NotFound();

            tenant.SetTenant(pipeline.TenantId);

            // Member-level check — IgnoreQueryFilters because _tenantId is captured at Guid.Empty when no tenant header
            var isMember = await db.WorkspaceMembers.IgnoreQueryFilters()
                .AnyAsync(m => m.TenantId == pipeline.TenantId && m.UserId == user.UserId);
            if (!isMember) return Results.Forbid();

            // Validate inputs
            var trimmedTitle = req.Title?.Trim() ?? "";
            if (trimmedTitle.Length < 2)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Título é obrigatório (mínimo 2 caracteres)"] });
            if (req.MonetaryValue.HasValue && req.MonetaryValue < 0)
                return Results.BadRequest(new { error = "monetaryValue must be >= 0" });

            // Verify the requested stage belongs to this pipeline — IgnoreQueryFilters same reason
            var stage = await db.Stages.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.TenantId == pipeline.TenantId && s.Id == req.StageId && s.PipelineId == pipelineId);
            if (stage is null) return Results.BadRequest(new { error = "stageId does not belong to this pipeline" });

            // Compute position: after the last card in that stage
            var lastPosition = await db.Cards.IgnoreQueryFilters()
                .Where(c => c.TenantId == pipeline.TenantId && c.StageId == req.StageId && c.ArchivedAt == null)
                .OrderByDescending(c => c.Position)
                .Select(c => (double?)c.Position)
                .FirstOrDefaultAsync() ?? 0.0;

            var card = new Card
            {
                Id = Guid.NewGuid(),
                TenantId = pipeline.TenantId,
                PipelineId = pipelineId,
                StageId = req.StageId,
                Title = trimmedTitle,
                Description = req.Description,
                MonetaryValue = req.MonetaryValue,
                AssigneeId = req.AssigneeId,
                CloseDate = req.CloseDate,
                Position = FractionalIndex.After(lastPosition),
                StageEnteredAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Cards.Add(card);
            // LogActivity before SaveChangesAsync
            ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                "created", new { title = card.Title });
            await db.SaveChangesAsync();

            return Results.Created($"/cards/{card.Id}", ToDto(card));
        }).RequireAuthorization();

        // GET /cards/{id} — get card by ID
        app.MapGet("/cards/{id:guid}",
            async (Guid id, AppDbContext db, ITenantService tenant) =>
        {
            // Resolve tenant via card
            var card = await db.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (card is null) return Results.NotFound();

            tenant.SetTenant(card.TenantId);

            // Manual filter: _tenantId captured at Guid.Empty, must check explicitly
            if (card.ArchivedAt != null) return Results.NotFound();
            var filteredCard = card;

            return Results.Ok(ToDto(filteredCard));
        }).RequireAuthorization();

        // PATCH /cards/{id} — partial update card (member-level)
        app.MapPatch("/cards/{id:guid}",
            async (Guid id, UpdateCardRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            // Resolve tenant via card (IgnoreQueryFilters to bootstrap)
            var card = await db.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (card is null) return Results.NotFound();

            tenant.SetTenant(card.TenantId);

            var isMember = await db.WorkspaceMembers.IgnoreQueryFilters()
                .AnyAsync(m => m.TenantId == card.TenantId && m.UserId == user.UserId);
            if (!isMember) return Results.Forbid();

            // Validate monetary value if provided
            if (req.MonetaryValue.HasValue && req.MonetaryValue < 0)
                return Results.BadRequest(new { error = "monetaryValue must be >= 0" });

            // Partial update — only update fields that are non-null in request
            // Log one 'edited' activity entry per mutated field
            if (req.Title is not null && req.Title != card.Title)
            {
                ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                    "edited", new { field = "title", old_value = card.Title, new_value = req.Title });
                card.Title = req.Title;
            }

            if (req.Description is not null && req.Description != card.Description)
            {
                ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                    "edited", new { field = "description", old_value = card.Description, new_value = req.Description });
                card.Description = req.Description;
            }

            if (req.MonetaryValue.HasValue && req.MonetaryValue != card.MonetaryValue)
            {
                ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                    "edited", new { field = "monetary_value", old_value = card.MonetaryValue, new_value = req.MonetaryValue });
                card.MonetaryValue = req.MonetaryValue;
            }

            if (req.AssigneeId.HasValue && req.AssigneeId != card.AssigneeId)
            {
                ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                    "edited", new { field = "assignee_id", old_value = card.AssigneeId, new_value = req.AssigneeId });
                card.AssigneeId = req.AssigneeId;
            }

            if (req.CloseDate.HasValue && req.CloseDate != card.CloseDate)
            {
                ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                    "edited", new { field = "close_date", old_value = card.CloseDate, new_value = req.CloseDate });
                card.CloseDate = req.CloseDate;
            }

            await db.SaveChangesAsync();
            return Results.Ok(ToDto(card));
        }).RequireAuthorization();

        // PATCH /cards/{id}/archive — soft-delete card (member-level)
        app.MapPatch("/cards/{id:guid}/archive",
            async (Guid id, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            // Use IgnoreQueryFilters to find card by ID (bootstrap tenant)
            var card = await db.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (card is null) return Results.NotFound();

            tenant.SetTenant(card.TenantId);

            var isMember = await db.WorkspaceMembers.IgnoreQueryFilters()
                .AnyAsync(m => m.TenantId == card.TenantId && m.UserId == user.UserId);
            if (!isMember) return Results.Forbid();

            card.ArchivedAt = DateTimeOffset.UtcNow;
            // LogActivity before SaveChangesAsync
            ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                "archived", new { });
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(card));
        }).RequireAuthorization();

        // PATCH /cards/{id}/move — move card to different stage (member-level)
        app.MapPatch("/cards/{id:guid}/move",
            async (Guid id, MoveCardRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            // Use IgnoreQueryFilters to find card by ID (bootstrap tenant)
            var card = await db.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
            if (card is null) return Results.NotFound();

            tenant.SetTenant(card.TenantId);

            var isMember = await db.WorkspaceMembers.IgnoreQueryFilters()
                .AnyAsync(m => m.TenantId == card.TenantId && m.UserId == user.UserId);
            if (!isMember) return Results.Forbid();

            // Validate targetStageId belongs to same pipeline (cross-pipeline guard)
            var targetStage = await db.Stages.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.TenantId == card.TenantId && s.Id == req.TargetStageId);
            if (targetStage is null) return Results.NotFound();
            if (targetStage.PipelineId != card.PipelineId)
                return Results.BadRequest(new { error = "targetStageId belongs to a different pipeline" });

            // Load from-stage name before updating StageId (for activity log)
            var fromStage = await db.Stages.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.TenantId == card.TenantId && s.Id == card.StageId);

            // Compute position via fractional indexing
            var newPosition = FractionalIndex.Between(req.PrevPosition, req.NextPosition);

            // Atomically update stage_id, stage_entered_at, and position
            card.StageId = req.TargetStageId;
            card.StageEnteredAt = DateTimeOffset.UtcNow;
            card.Position = newPosition;

            // LogActivity before SaveChangesAsync
            ActivityLogEndpoints.LogActivity(db, card.TenantId, card.Id, user.UserId!.Value,
                "moved", new { from_stage = fromStage?.Name ?? "", to_stage = targetStage.Name });
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(card));
        }).RequireAuthorization();

        return app;
    }

    private static CardDto ToDto(Card card) => new(
        card.Id, card.Title, card.Description, card.MonetaryValue,
        card.StageId, card.PipelineId, card.AssigneeId, card.CloseDate,
        card.StageEnteredAt, card.Position, card.ArchivedAt, card.CreatedAt);
}
