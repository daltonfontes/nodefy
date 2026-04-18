using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class StageTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    public StageTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

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
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<StageDto>())!;
    }

    [Fact]
    public async Task CreateStage_AsAdmin_ReturnsCreated()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "stagecreate@test.com");
        await SeedUser(client, userId, "stagecreate@test.com");
        var ws = await CreateWorkspace(client, "Stage Create Workspace");
        var tenantClient = CreateClient(userId, "stagecreate@test.com", ws.Id);
        var pipeline = await CreatePipeline(tenantClient, ws.Id, "Pipeline A");

        var resp = await tenantClient.PostAsJsonAsync($"/pipelines/{pipeline.Id}/stages", new { name = "Prospecção" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<StageDto>();
        body!.Name.Should().Be("Prospecção");
        body.Position.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenameStage_AsAdmin_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "stagerename@test.com");
        await SeedUser(client, userId, "stagerename@test.com");
        var ws = await CreateWorkspace(client, "Stage Rename Workspace");
        var tenantClient = CreateClient(userId, "stagerename@test.com", ws.Id);
        var pipeline = await CreatePipeline(tenantClient, ws.Id, "Pipeline B");
        var stage = await CreateStage(tenantClient, pipeline.Id, "Old Stage");

        var resp = await tenantClient.PatchAsJsonAsync($"/stages/{stage.Id}", new { name = "New Stage" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<StageDto>();
        body!.Name.Should().Be("New Stage");
    }

    [Fact]
    public async Task DeleteStage_AsAdmin_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "stagedelete@test.com");
        await SeedUser(client, userId, "stagedelete@test.com");
        var ws = await CreateWorkspace(client, "Stage Delete Workspace");
        var tenantClient = CreateClient(userId, "stagedelete@test.com", ws.Id);
        var pipeline = await CreatePipeline(tenantClient, ws.Id, "Pipeline C");
        var stage = await CreateStage(tenantClient, pipeline.Id, "To Delete");

        var resp = await tenantClient.DeleteAsync($"/stages/{stage.Id}");

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReorderStage_AsAdmin_ReturnsUpdatedPosition()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "stagereorder@test.com");
        await SeedUser(client, userId, "stagereorder@test.com");
        var ws = await CreateWorkspace(client, "Reorder Workspace");
        var tenantClient = CreateClient(userId, "stagereorder@test.com", ws.Id);
        var pipeline = await CreatePipeline(tenantClient, ws.Id, "Pipeline D");
        var stage1 = await CreateStage(tenantClient, pipeline.Id, "Stage 1");
        var stage2 = await CreateStage(tenantClient, pipeline.Id, "Stage 2");

        // Move stage2 before stage1 by providing prevPosition=null, nextPosition=stage1.Position
        var resp = await tenantClient.PatchAsJsonAsync($"/stages/{stage2.Id}/position",
            new { prevPosition = (double?)null, nextPosition = (double?)stage1.Position });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ReorderStageResponse>();
        body!.Stage.Position.Should().BeLessThan(stage1.Position);
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record PipelineDto(Guid Id, string Name, double Position);
    private record StageDto(Guid Id, string Name, double Position);
    private record ReorderStageResponse(StageDto Stage, List<object> RebalancedPositions);
}
