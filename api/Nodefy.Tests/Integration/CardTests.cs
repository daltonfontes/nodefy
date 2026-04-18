using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class CardTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    public CardTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

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
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<WorkspaceDto>())!;
    }

    private async Task<PipelineDto> CreatePipeline(HttpClient client, Guid workspaceId, string name)
    {
        var resp = await client.PostAsJsonAsync($"/workspaces/{workspaceId}/pipelines", new { name });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<PipelineDto>())!;
    }

    private async Task<StageDto> CreateStage(HttpClient client, Guid pipelineId, string name)
    {
        var resp = await client.PostAsJsonAsync($"/pipelines/{pipelineId}/stages", new { name });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<StageDto>())!;
    }

    private async Task<CardDto> CreateCard(HttpClient client, Guid pipelineId, Guid stageId, string title)
    {
        var resp = await client.PostAsJsonAsync($"/pipelines/{pipelineId}/cards", new {
            title, stageId, description = (string?)null, monetaryValue = (decimal?)null,
            assigneeId = (Guid?)null, closeDate = (DateTimeOffset?)null
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<CardDto>())!;
    }

    [Fact]
    public async Task CreateCard_AsMember_ReturnsCreated()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "cardcreate@test.com");
        await SeedUser(client, userId, "cardcreate@test.com");
        var ws = await CreateWorkspace(client, "Card Create Workspace");
        var tc = CreateClient(userId, "cardcreate@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage = await CreateStage(tc, pipeline.Id, "Prospecção");

        var resp = await tc.PostAsJsonAsync($"/pipelines/{pipeline.Id}/cards", new {
            title = "Deal Alpha", stageId = stage.Id,
            description = "First deal", monetaryValue = 1500.00m,
            assigneeId = (Guid?)null, closeDate = (DateTimeOffset?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<CardDto>();
        body!.Title.Should().Be("Deal Alpha");
        body.MonetaryValue.Should().Be(1500.00m);
        body.StageId.Should().Be(stage.Id);
        body.ArchivedAt.Should().BeNull();
    }

    [Fact]
    public async Task EditCard_AsMember_ReturnsUpdatedFields()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "cardedit@test.com");
        await SeedUser(client, userId, "cardedit@test.com");
        var ws = await CreateWorkspace(client, "Card Edit Workspace");
        var tc = CreateClient(userId, "cardedit@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage = await CreateStage(tc, pipeline.Id, "Stage");
        var card = await CreateCard(tc, pipeline.Id, stage.Id, "Original Title");

        var resp = await tc.PatchAsJsonAsync($"/cards/{card.Id}", new { title = "Updated Title", monetaryValue = 2000.00m });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<CardDto>();
        body!.Title.Should().Be("Updated Title");
        body.MonetaryValue.Should().Be(2000.00m);
    }

    [Fact]
    public async Task ArchiveCard_AsMember_SetsArchivedAt()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "cardarchive@test.com");
        await SeedUser(client, userId, "cardarchive@test.com");
        var ws = await CreateWorkspace(client, "Card Archive Workspace");
        var tc = CreateClient(userId, "cardarchive@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage = await CreateStage(tc, pipeline.Id, "Stage");
        var card = await CreateCard(tc, pipeline.Id, stage.Id, "To Archive");

        var resp = await tc.PatchAsJsonAsync($"/cards/{card.Id}/archive", new { });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        // After archive, board endpoint should not include this card
        var boardResp = await tc.GetAsync($"/pipelines/{pipeline.Id}/board");
        var board = await boardResp.Content.ReadFromJsonAsync<BoardDto>();
        var stageData = board!.Stages.First(s => s.Id == stage.Id);
        stageData.Cards.Should().NotContain(c => c.Id == card.Id);
    }

    [Fact]
    public async Task MoveCard_UpdatesStageIdAndStageEnteredAt()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "cardmove@test.com");
        await SeedUser(client, userId, "cardmove@test.com");
        var ws = await CreateWorkspace(client, "Card Move Workspace");
        var tc = CreateClient(userId, "cardmove@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage1 = await CreateStage(tc, pipeline.Id, "Stage 1");
        var stage2 = await CreateStage(tc, pipeline.Id, "Stage 2");
        var card = await CreateCard(tc, pipeline.Id, stage1.Id, "Moving Card");
        var originalEnteredAt = card.StageEnteredAt;

        await Task.Delay(10); // ensure clock advances
        var resp = await tc.PatchAsJsonAsync($"/cards/{card.Id}/move", new {
            targetStageId = stage2.Id, prevPosition = (double?)null, nextPosition = (double?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<CardDto>();
        body!.StageId.Should().Be(stage2.Id);
        body.StageEnteredAt.Should().BeAfter(originalEnteredAt);
    }

    [Fact]
    public async Task CreateCard_WithNegativeMonetaryValue_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "cardnegative@test.com");
        await SeedUser(client, userId, "cardnegative@test.com");
        var ws = await CreateWorkspace(client, "Validation Workspace");
        var tc = CreateClient(userId, "cardnegative@test.com", ws.Id);
        var pipeline = await CreatePipeline(tc, ws.Id, "Pipeline");
        var stage = await CreateStage(tc, pipeline.Id, "Stage");

        var resp = await tc.PostAsJsonAsync($"/pipelines/{pipeline.Id}/cards", new {
            title = "Bad Card", stageId = stage.Id, monetaryValue = -100.00m
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record PipelineDto(Guid Id, string Name, double Position);
    private record StageDto(Guid Id, string Name, double Position);
    private record CardDto(Guid Id, string Title, string? Description, decimal? MonetaryValue, Guid StageId, Guid PipelineId, Guid? AssigneeId, DateTimeOffset? CloseDate, DateTimeOffset StageEnteredAt, double Position, DateTimeOffset? ArchivedAt, DateTimeOffset CreatedAt);
    private record CardSummaryDto(Guid Id, string Title, decimal? MonetaryValue, Guid? AssigneeId, DateTimeOffset StageEnteredAt, double Position);
    private record StageBoardDto(Guid Id, string Name, double Position, int CardCount, decimal MonetarySum, List<CardSummaryDto> Cards);
    private record BoardDto(PipelineDto Pipeline, List<StageBoardDto> Stages);
}
