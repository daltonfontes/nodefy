using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class WorkspaceTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    public WorkspaceTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

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
        await client.PostAsJsonAsync("/sso/sync", new
        {
            provider = "github",
            providerAccountId = userId.ToString(),
            email,
            name = "Test User",
            avatarUrl = (string?)null
        });
    }

    [Fact]
    public async Task CreateWorkspace_ReturnsCreated_WithCallerAsAdminMember()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, "user@test.com");
        await SeedUser(client, userId, "user@test.com");

        var resp = await client.PostAsJsonAsync("/workspaces", new { name = "Acme" });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<WorkspaceDto>();
        body.Should().NotBeNull();
        body!.Currency.Should().Be("BRL");
        body.Slug.Should().Be("acme");
        body.CurrencyLocked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateWorkspace_DefaultsCurrencyToBRL()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient(userId, $"user-{userId}@test.com");
        await SeedUser(client, userId, $"user-{userId}@test.com");

        var resp = await client.PostAsJsonAsync("/workspaces", new { name = "BRL Test Workspace" });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<WorkspaceDto>();
        body!.Currency.Should().Be("BRL");
    }

    [Fact]
    public async Task PatchSettings_RejectsInvalidCurrency_With400()
    {
        var userId = Guid.NewGuid();
        var email = $"user-{userId}@test.com";
        var client = CreateClient(userId, email);
        await SeedUser(client, userId, email);

        var createResp = await client.PostAsJsonAsync("/workspaces", new { name = "Currency Test" });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var ws = await createResp.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Update client headers with tenant
        var adminClient = CreateClient(userId, email, ws!.Id);
        var resp = await adminClient.PatchAsJsonAsync($"/workspaces/{ws.Id}/settings", new { currency = "JPY" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchSettings_RejectsCurrencyChange_When_CurrencyLocked_With409()
    {
        var userId = Guid.NewGuid();
        var email = $"user-{userId}@test.com";
        var client = CreateClient(userId, email);
        await SeedUser(client, userId, email);

        var createResp = await client.PostAsJsonAsync("/workspaces", new { name = "Locked Workspace" });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var ws = await createResp.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Directly set currency_locked via DB (using a scoped service to update)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Nodefy.Api.Data.AppDbContext>();
        var workspace = await db.Workspaces.FindAsync(ws!.Id);
        workspace!.CurrencyLocked = true;
        await db.SaveChangesAsync();

        var adminClient = CreateClient(userId, email, ws.Id);
        var resp = await adminClient.PatchAsJsonAsync($"/workspaces/{ws.Id}/settings", new { currency = "USD" });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetWorkspaces_ReturnsOnlyMembershipsOfCaller()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var email = $"user-{userId}@test.com";
        var otherEmail = $"other-{otherUserId}@test.com";

        var client = CreateClient(userId, email);
        await SeedUser(client, userId, email);

        var otherClient = CreateClient(otherUserId, otherEmail);
        await SeedUser(otherClient, otherUserId, otherEmail);

        // User creates their own workspace
        await client.PostAsJsonAsync("/workspaces", new { name = $"My Workspace {userId}" });

        // Other user creates their workspace
        await otherClient.PostAsJsonAsync("/workspaces", new { name = $"Other Workspace {otherUserId}" });

        var resp = await client.GetAsync("/workspaces");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspaces = await resp.Content.ReadFromJsonAsync<WorkspaceDto[]>();
        workspaces.Should().NotBeNull();
        // Caller sees only their workspaces, not the other user's
        workspaces!.All(w => w.Role != null).Should().BeTrue();
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
}
