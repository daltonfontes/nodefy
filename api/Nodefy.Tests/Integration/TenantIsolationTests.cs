using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nodefy.Api.Data;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class TenantIsolationTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;
    private readonly PostgresFixture _fixture;

    public TenantIsolationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = new ApiFactory(fixture.ConnectionString);
    }

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
    public async Task GetMembers_AsTenantA_ReturnsOnlyTenantAMembers()
    {
        // Arrange: create two workspaces (two tenants)
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var emailA = $"tenant-a-{userAId}@test.com";
        var emailB = $"tenant-b-{userBId}@test.com";

        var clientA = CreateClient(userAId, emailA);
        var clientB = CreateClient(userBId, emailB);

        await SeedUser(clientA, userAId, emailA);
        await SeedUser(clientB, userBId, emailB);

        // Each user creates their own workspace = their own tenant
        var respA = await clientA.PostAsJsonAsync("/workspaces", new { name = $"Tenant A {userAId}" });
        respA.StatusCode.Should().Be(HttpStatusCode.Created);
        var wsA = await respA.Content.ReadFromJsonAsync<WorkspaceDto>();

        var respB = await clientB.PostAsJsonAsync("/workspaces", new { name = $"Tenant B {userBId}" });
        respB.StatusCode.Should().Be(HttpStatusCode.Created);
        var wsB = await respB.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Act: userA lists members of their own workspace
        var adminClientA = CreateClient(userAId, emailA, wsA!.Id);
        var membersResp = await adminClientA.GetAsync($"/workspaces/{wsA.Id}/members");
        membersResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await membersResp.Content.ReadFromJsonAsync<MemberDto[]>();

        // Assert: only userA is a member of workspaceA, not userB
        members.Should().NotBeNull();
        members!.Should().HaveCount(1);
        members![0].UserId.Should().Be(userAId);
        members!.All(m => m.UserId != userBId).Should().BeTrue();
    }

    [Fact]
    public async Task GetMembers_RawSql_AlsoReturnsZeroForOtherTenant()
    {
        // Arrange: create two tenants
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var emailA = $"rls-a-{userAId}@test.com";
        var emailB = $"rls-b-{userBId}@test.com";

        var clientA = CreateClient(userAId, emailA);
        var clientB = CreateClient(userBId, emailB);

        await SeedUser(clientA, userAId, emailA);
        await SeedUser(clientB, userBId, emailB);

        var respA = await clientA.PostAsJsonAsync("/workspaces", new { name = $"RLS Tenant A {userAId}" });
        var wsA = await respA.Content.ReadFromJsonAsync<WorkspaceDto>();

        var respB = await clientB.PostAsJsonAsync("/workspaces", new { name = $"RLS Tenant B {userBId}" });
        var wsB = await respB.Content.ReadFromJsonAsync<WorkspaceDto>();

        // Act: execute raw SQL with RLS session var set to tenantA, query for tenantB rows
        // This proves the Postgres RLS layer works independently of EF Core global filters
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        // Set RLS context to tenant A
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SET app.current_tenant = '{wsA!.Id}'";
            await cmd.ExecuteNonQueryAsync();
        }

        // Query for tenant B members via raw SQL — RLS should return 0 rows
        int count;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM workspace_members WHERE tenant_id = '{wsB!.Id}'";
            count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Assert: RLS blocks cross-tenant access
        count.Should().Be(0, "RLS policy should prevent reading tenant B rows when session is set to tenant A");
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record MemberDto(Guid UserId, string? Name, string Email, string? AvatarUrl, string Role, DateTimeOffset JoinedAt);
}
