using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Lib;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;

public static class StageEndpoints
{
    public record CreateStageRequest(string Name);
    public record RenameStageRequest(string Name);
    public record ReorderStageRequest(double? PrevPosition, double? NextPosition);
    public record StageDto(Guid Id, string Name, double Position, Guid PipelineId);
    public record ReorderedPositionDto(Guid Id, double Position);
    public record ReorderStageResponse(StageDto Stage, List<ReorderedPositionDto> RebalancedPositions);

    public static IEndpointRouteBuilder MapStageEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /pipelines/{pipelineId}/stages — create stage (admin only)
        app.MapPost("/pipelines/{pipelineId:guid}/stages",
            async (Guid pipelineId, CreateStageRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == pipelineId);
            if (pipeline is null) return Results.NotFound();

            tenant.SetTenant(pipeline.TenantId);

            var trimmedName = req.Name?.Trim() ?? "";
            if (trimmedName.Length < 2)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório (mínimo 2 caracteres)"] });
            if (!await WorkspaceEndpoints.IsAdmin(db, pipeline.TenantId, user.UserId)) return Results.Forbid();

            var lastPosition = await db.Stages
                .Where(s => s.PipelineId == pipelineId)
                .OrderByDescending(s => s.Position)
                .Select(s => (double?)s.Position)
                .FirstOrDefaultAsync() ?? 0.0;

            var stage = new Stage
            {
                Id = Guid.NewGuid(),
                TenantId = pipeline.TenantId,
                PipelineId = pipelineId,
                Name = trimmedName,
                Position = FractionalIndex.After(lastPosition),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Stages.Add(stage);
            await db.SaveChangesAsync();
            return Results.Created($"/stages/{stage.Id}", new StageDto(stage.Id, stage.Name, stage.Position, stage.PipelineId));
        }).RequireAuthorization();

        // PATCH /stages/{id} — rename stage (admin only)
        app.MapPatch("/stages/{id:guid}",
            async (Guid id, RenameStageRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            var stage = await db.Stages.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (stage is null) return Results.NotFound();

            tenant.SetTenant(stage.TenantId);

            var trimmedName = req.Name?.Trim() ?? "";
            if (trimmedName.Length < 2)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório (mínimo 2 caracteres)"] });
            if (!await WorkspaceEndpoints.IsAdmin(db, stage.TenantId, user.UserId)) return Results.Forbid();

            stage.Name = trimmedName;
            await db.SaveChangesAsync();
            return Results.Ok(new StageDto(stage.Id, stage.Name, stage.Position, stage.PipelineId));
        }).RequireAuthorization();

        // DELETE /stages/{id} — delete stage (admin only)
        app.MapDelete("/stages/{id:guid}",
            async (Guid id, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            var stage = await db.Stages.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (stage is null) return Results.NotFound();

            tenant.SetTenant(stage.TenantId);

            if (!await WorkspaceEndpoints.IsAdmin(db, stage.TenantId, user.UserId)) return Results.Forbid();

            db.Stages.Remove(stage);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        // PATCH /stages/{id}/position — reorder stage (admin only)
        app.MapPatch("/stages/{id:guid}/position",
            async (Guid id, ReorderStageRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            var stage = await db.Stages.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (stage is null) return Results.NotFound();

            tenant.SetTenant(stage.TenantId);

            if (!await WorkspaceEndpoints.IsAdmin(db, stage.TenantId, user.UserId)) return Results.Forbid();

            var newPosition = FractionalIndex.Between(req.PrevPosition, req.NextPosition);
            stage.Position = newPosition;

            // Check if rebalance is needed across all siblings
            var siblings = await db.Stages
                .Where(s => s.PipelineId == stage.PipelineId)
                .OrderBy(s => s.Position)
                .ToListAsync();

            var allPositions = siblings.Select(s => s.Id == stage.Id ? newPosition : s.Position).ToList();
            var rebalancedList = new List<ReorderedPositionDto>();

            if (FractionalIndex.NeedsRebalance(allPositions))
            {
                var rebalanced = FractionalIndex.Rebalance(siblings.Count);
                var ordered = siblings.OrderBy(s => s.Id == stage.Id ? newPosition : s.Position).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].Position = rebalanced[i];
                    rebalancedList.Add(new ReorderedPositionDto(ordered[i].Id, rebalanced[i]));
                }
            }

            await db.SaveChangesAsync();

            var stageDto = new StageDto(stage.Id, stage.Name, stage.Position, stage.PipelineId);
            return Results.Ok(new ReorderStageResponse(stageDto, rebalancedList));
        }).RequireAuthorization();

        return app;
    }
}
