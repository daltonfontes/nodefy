using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class BoardTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    public BoardTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

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

    [Fact]
    public async Task GetBoard_ReturnsPipelineWithStagesAndAggregates()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "board@test.com");
        await SeedUser(client, userId, "board@test.com");
        var ws = await CreateWorkspace(client, "Board Workspace");
        var tenantClient = CreateClient(userId, "board@test.com", ws.Id);
        var pipeline = await CreatePipeline(tenantClient, ws.Id, "Sales");
        await tenantClient.PostAsJsonAsync($"/pipelines/{pipeline.Id}/stages", new { name = "Prospecção" });
        await tenantClient.PostAsJsonAsync($"/pipelines/{pipeline.Id}/stages", new { name = "Proposta" });

        var resp = await tenantClient.GetAsync($"/pipelines/{pipeline.Id}/board");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<BoardDto>();
        body!.Stages.Should().HaveCount(2);
        body.Stages.All(s => s.CardCount >= 0).Should().BeTrue();
        body.Stages.All(s => s.MonetarySum >= 0).Should().BeTrue();
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record PipelineDto(Guid Id, string Name, double Position);
    private record BoardDto(PipelineDto Pipeline, List<StageBoardDto> Stages);
    private record StageBoardDto(Guid Id, string Name, double Position, int CardCount, decimal MonetarySum, List<object> Cards);
}
