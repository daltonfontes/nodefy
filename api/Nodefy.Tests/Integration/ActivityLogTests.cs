using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class ActivityLogTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    public ActivityLogTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

    private HttpClient CreateClient(Guid userId, string email, Guid? tenantId = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        if (tenantId.HasValue)
            client.DefaultRequestHeaders.Add("X-Test-Tenant-Id", tenantId.Value.ToString());
        return client;
    }

    private async Task SeedUser(HttpClient client, Guid userId, string email)
    {
        await client.PostAsJsonAsync("/sso/sync", new {
            provider = "github", providerAccountId = userId.ToString(),
            email, name = "Test User", avatarUrl = (string?)null
        });
    }

    private async Task<WorkspaceDto> CreateWorkspace(HttpClient client, string name)
    {
        var resp = await client.PostAsJsonAsync("/workspaces", new { name });
        return (await resp.Content.ReadFromJsonAsync<WorkspaceDto>())!;
    }

    private async Task<PipelineDto> CreatePipeline(HttpClient client, Guid workspaceId, string name)
    {
        var resp = await client.PostAsJsonAsync($"/workspaces/{workspaceId}/pipelines", new { name });
        return (await resp.Content.ReadFromJsonAsync<PipelineDto>())!;
    }

    private async Task<StageDto> CreateStage(HttpClient client, Guid pipelineId, string name)
    {
        var resp = await client.PostAsJsonAsync($"/pipelines/{pipelineId}/stages", new { name });
        return (await resp.Content.ReadFromJsonAsync<StageDto>())!;
    }

    private async Task<CardDto> CreateCard(HttpClient client, Guid pipelineId, Guid stageId, string title)
    {
        var resp = await client.PostAsJsonAsync($"/pipelines/{pipelineId}/cards", new {
            title, stageId, description = (string?)null, monetaryValue = (decimal?)null,
            assigneeId = (Guid?)null, closeDate = (DateTimeOffset?)null
        });
        return (await resp.Content.ReadFromJsonAsync<CardDto>())!;
    }

    [Fact]
    public async Task CreateCard_AppendsCreatedActivityLog()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "actlog1@test.com");
        await SeedUser(client, userId, "actlog1@test.com");
        var ws = await CreateWorkspace(client, "ActivityLog Workspace 1");
        var tc = CreateClient(userId, "actlog1@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage = await CreateStage(tc, pipeline.Id, "Stage");

        var card = await CreateCard(tc, pipeline.Id, stage.Id, "New Card");

        var logResp = await tc.GetAsync($"/cards/{card.Id}/activity");
        logResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await logResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();
        logs.Should().HaveCount(1);
        logs![0].Action.Should().Be("created");
    }

    [Fact]
    public async Task MoveCard_AppendsMovedActivityLog()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "actlog2@test.com");
        await SeedUser(client, userId, "actlog2@test.com");
        var ws = await CreateWorkspace(client, "ActivityLog Workspace 2");
        var tc = CreateClient(userId, "actlog2@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage1 = await CreateStage(tc, pipeline.Id, "Prospecção");
        var stage2 = await CreateStage(tc, pipeline.Id, "Proposta");
        var card = await CreateCard(tc, pipeline.Id, stage1.Id, "Moving Card");

        await tc.PatchAsJsonAsync($"/cards/{card.Id}/move", new {
            targetStageId = stage2.Id, prevPosition = (double?)null, nextPosition = (double?)null
        });

        var logResp = await tc.GetAsync($"/cards/{card.Id}/activity");
        var logs = await logResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();
        logs.Should().HaveCount(2);
        logs![1].Action.Should().Be("moved");
        logs[1].Payload.Should().Contain("Proposta");
    }

    [Fact]
    public async Task EditCard_AppendsEditedActivityLog()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "actlog3@test.com");
        await SeedUser(client, userId, "actlog3@test.com");
        var ws = await CreateWorkspace(client, "ActivityLog Workspace 3");
        var tc = CreateClient(userId, "actlog3@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage = await CreateStage(tc, pipeline.Id, "Stage");
        var card = await CreateCard(tc, pipeline.Id, stage.Id, "Edited Card");

        await tc.PatchAsJsonAsync($"/cards/{card.Id}", new { monetaryValue = 5000.00m });

        var logResp = await tc.GetAsync($"/cards/{card.Id}/activity");
        var logs = await logResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();
        logs.Should().HaveCountGreaterThanOrEqualTo(2);
        logs!.Should().Contain(l => l.Action == "edited");
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record PipelineDto(Guid Id, string Name, double Position);
    private record StageDto(Guid Id, string Name, double Position);
    private record CardDto(Guid Id, string Title, string? Description, decimal? MonetaryValue, Guid StageId, Guid PipelineId, Guid? AssigneeId, DateTimeOffset? CloseDate, DateTimeOffset StageEnteredAt, double Position, DateTimeOffset? ArchivedAt, DateTimeOffset CreatedAt);
    private record ActivityLogDto(Guid Id, string Action, string Payload, Guid ActorId, DateTimeOffset CreatedAt);
}
