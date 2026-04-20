using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class PipelineTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    public PipelineTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

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

    [Fact]
    public async Task CreatePipeline_AsAdmin_ReturnsCreated()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "admin@test.com");
        await SeedUser(client, userId, "admin@test.com");
        var ws = await CreateWorkspace(client, "Test Workspace");
        var tenantClient = CreateClient(userId, "admin@test.com", ws.Id);

        var resp = await tenantClient.PostAsJsonAsync($"/workspaces/{ws.Id}/pipelines", new { name = "Sales Pipeline" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<PipelineDto>();
        body!.Name.Should().Be("Sales Pipeline");
        body.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePipeline_AsMember_ReturnsForbidden()
    {
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var adminClient = CreateClient(adminId, "admin2@test.com");
        await SeedUser(adminClient, adminId, "admin2@test.com");
        var ws = await CreateWorkspace(adminClient, "Member Test Workspace");
        var tenantAdminClient = CreateClient(adminId, "admin2@test.com", ws.Id);

        await SeedUser(CreateClient(memberId, "member@test.com"), memberId, "member@test.com");
        await tenantAdminClient.PostAsJsonAsync($"/workspaces/{ws.Id}/invites", new { email = "member@test.com", role = "member" });

        var memberClient = CreateClient(memberId, "member@test.com", ws.Id);
        var resp = await memberClient.PostAsJsonAsync($"/workspaces/{ws.Id}/pipelines", new { name = "Unauthorized" });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RenamePipeline_AsAdmin_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "admin3@test.com");
        await SeedUser(client, userId, "admin3@test.com");
        var ws = await CreateWorkspace(client, "Rename Workspace");
        var tenantClient = CreateClient(userId, "admin3@test.com", ws.Id);
        var pipeline = await CreatePipeline(tenantClient, ws.Id, "Old Name");

        var resp = await tenantClient.PatchAsJsonAsync($"/pipelines/{pipeline.Id}", new { name = "New Name" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<PipelineDto>();
        body!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task DeletePipeline_AsAdmin_ReturnsNoContent()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "admin4@test.com");
        await SeedUser(client, userId, "admin4@test.com");
        var ws = await CreateWorkspace(client, "Delete Workspace");
        var tenantClient = CreateClient(userId, "admin4@test.com", ws.Id);
        var pipeline = await CreatePipeline(tenantClient, ws.Id, "To Delete");

        var resp = await tenantClient.DeleteAsync($"/pipelines/{pipeline.Id}");

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record PipelineDto(Guid Id, string Name, double Position);
}
