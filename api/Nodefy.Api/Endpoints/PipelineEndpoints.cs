using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;
using Nodefy.Api.Lib;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Endpoints;

public static class PipelineEndpoints
{
    public record CreatePipelineRequest(string Name);
    public record RenamePipelineRequest(string Name);
    public record PipelineDto(Guid Id, string Name, double Position);
    public record CardSummaryDto(Guid Id, string Title, decimal? MonetaryValue, Guid? AssigneeId, DateTimeOffset StageEnteredAt, double Position);
    public record StageBoardDto(Guid Id, string Name, double Position, int CardCount, decimal MonetarySum, List<CardSummaryDto> Cards);
    public record BoardDto(PipelineDto Pipeline, List<StageBoardDto> Stages);

    public static IEndpointRouteBuilder MapPipelineEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /workspaces/{workspaceId}/pipelines — list pipelines in workspace
        app.MapGet("/workspaces/{workspaceId:guid}/pipelines",
            async (Guid workspaceId, AppDbContext db, ITenantService tenant) =>
        {
            tenant.SetTenant(workspaceId);
            var pipelines = await db.Pipelines
                .OrderBy(p => p.Position)
                .Select(p => new PipelineDto(p.Id, p.Name, p.Position))
                .ToListAsync();
            return Results.Ok(pipelines);
        }).RequireAuthorization();

        // POST /workspaces/{workspaceId}/pipelines — create pipeline (admin only)
        app.MapPost("/workspaces/{workspaceId:guid}/pipelines",
            async (Guid workspaceId, CreatePipelineRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            tenant.SetTenant(workspaceId);
            if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length < 2)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório (mínimo 2 caracteres)"] });
            if (!await WorkspaceEndpoints.IsAdmin(db, workspaceId, user.UserId)) return Results.Forbid();

            var lastPosition = await db.Pipelines
                .OrderByDescending(p => p.Position)
                .Select(p => (double?)p.Position)
                .FirstOrDefaultAsync() ?? 0.0;

            var pipeline = new Pipeline
            {
                Id = Guid.NewGuid(),
                TenantId = workspaceId,
                Name = req.Name,
                Position = FractionalIndex.After(lastPosition),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.Pipelines.Add(pipeline);
            await db.SaveChangesAsync();
            return Results.Created($"/pipelines/{pipeline.Id}", new PipelineDto(pipeline.Id, pipeline.Name, pipeline.Position));
        }).RequireAuthorization();

        // GET /pipelines/{id}/board — board load with stages and card aggregates
        app.MapGet("/pipelines/{id:guid}/board",
            async (Guid id, AppDbContext db, ITenantService tenant) =>
        {
            var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (pipeline is null) return Results.NotFound();

            tenant.SetTenant(pipeline.TenantId);

            var stages = await db.Stages
                .Where(s => s.PipelineId == id)
                .OrderBy(s => s.Position)
                .ToListAsync();

            var stageDtos = new List<StageBoardDto>();
            foreach (var stage in stages)
            {
                var cards = await db.Cards
                    .Where(c => c.StageId == stage.Id)
                    .OrderBy(c => c.Position)
                    .Select(c => new CardSummaryDto(c.Id, c.Title, c.MonetaryValue, c.AssigneeId, c.StageEnteredAt, c.Position))
                    .ToListAsync();

                var cardCount = cards.Count;
                var monetarySum = cards
                    .Where(c => c.MonetaryValue.HasValue)
                    .Sum(c => c.MonetaryValue ?? 0m);

                stageDtos.Add(new StageBoardDto(stage.Id, stage.Name, stage.Position, cardCount, monetarySum, cards));
            }

            var pipelineDto = new PipelineDto(pipeline.Id, pipeline.Name, pipeline.Position);
            return Results.Ok(new BoardDto(pipelineDto, stageDtos));
        }).RequireAuthorization();

        // PATCH /pipelines/{id} — rename pipeline (admin only)
        app.MapPatch("/pipelines/{id:guid}",
            async (Guid id, RenamePipelineRequest req, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (pipeline is null) return Results.NotFound();

            tenant.SetTenant(pipeline.TenantId);

            if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length < 2)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nome é obrigatório (mínimo 2 caracteres)"] });
            if (!await WorkspaceEndpoints.IsAdmin(db, pipeline.TenantId, user.UserId)) return Results.Forbid();

            pipeline.Name = req.Name;
            await db.SaveChangesAsync();
            return Results.Ok(new PipelineDto(pipeline.Id, pipeline.Name, pipeline.Position));
        }).RequireAuthorization();

        // DELETE /pipelines/{id} — delete pipeline (admin only), cascades to stages
        app.MapDelete("/pipelines/{id:guid}",
            async (Guid id, AppDbContext db, CurrentUserAccessor user, ITenantService tenant) =>
        {
            var pipeline = await db.Pipelines.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (pipeline is null) return Results.NotFound();

            tenant.SetTenant(pipeline.TenantId);

            if (!await WorkspaceEndpoints.IsAdmin(db, pipeline.TenantId, user.UserId)) return Results.Forbid();

            db.Pipelines.Remove(pipeline);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}
