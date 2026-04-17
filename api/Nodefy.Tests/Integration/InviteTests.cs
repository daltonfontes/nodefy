using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nodefy.Api.Data;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class InviteTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;

    public InviteTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

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

    private async Task<WorkspaceDto> CreateWorkspace(HttpClient client, string name)
    {
        var resp = await client.PostAsJsonAsync("/workspaces", new { name });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await resp.Content.ReadFromJsonAsync<WorkspaceDto>())!;
    }

    [Fact]
    public async Task CreateInvite_GeneratesTokenOf32Bytes_And7DayExpiry()
    {
        var userId = Guid.NewGuid();
        var email = $"invite-admin-{userId}@test.com";
        var client = CreateClient(userId, email);
        await SeedUser(client, userId, email);
        var ws = await CreateWorkspace(client, $"Invite Test {userId}");

        var adminClient = CreateClient(userId, email, ws.Id);
        var resp = await adminClient.PostAsJsonAsync($"/workspaces/{ws.Id}/invites",
            new { email = "invited@test.com", role = "member" });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var invite = await resp.Content.ReadFromJsonAsync<InviteResponse>();
        invite.Should().NotBeNull();
        // URL-safe base64 of 32 bytes = 43 chars (no padding)
        invite!.Token.Should().NotBeNullOrEmpty();
        // 32 bytes base64url = at least 40 chars
        invite.Token.Length.Should().BeGreaterThanOrEqualTo(40);

        // ExpiresAt should be ~7 days from now
        invite.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(6));
        invite.ExpiresAt.Should().BeBefore(DateTimeOffset.UtcNow.AddDays(8));
    }

    [Fact]
    public async Task CreateInvite_StoresInviteUrl_WithFrontendUrlBase()
    {
        var userId = Guid.NewGuid();
        var email = $"invite-url-{userId}@test.com";
        var client = CreateClient(userId, email);
        await SeedUser(client, userId, email);
        var ws = await CreateWorkspace(client, $"Invite URL Test {userId}");

        var adminClient = CreateClient(userId, email, ws.Id);
        var resp = await adminClient.PostAsJsonAsync($"/workspaces/{ws.Id}/invites",
            new { email = "invited2@test.com", role = "member" });
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var invite = await resp.Content.ReadFromJsonAsync<InviteResponse>();
        invite.Should().NotBeNull();
        invite!.InviteUrl.Should().Contain("/invite/");
        invite.InviteUrl.Should().Contain(invite.Token);
    }

    [Fact]
    public async Task AcceptInvite_CreatesMembership_WithRoleFromInvitation()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"Accept Invite Test {adminId}");

        var adminClientWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var inviteResp = await adminClientWithTenant.PostAsJsonAsync($"/workspaces/{ws.Id}/invites",
            new { email = "newmember@test.com", role = "member" });
        inviteResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteResponse>();

        // New user accepts invite
        var newUserId = Guid.NewGuid();
        var newEmail = $"newmember-{newUserId}@test.com";
        var newClient = CreateClient(newUserId, newEmail);
        await SeedUser(newClient, newUserId, newEmail);

        var acceptResp = await newClient.PostAsJsonAsync($"/invites/{invite!.Token}/accept", new { });
        acceptResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await acceptResp.Content.ReadFromJsonAsync<AcceptResponse>();
        result.Should().NotBeNull();
        result!.WorkspaceId.Should().Be(ws.Id);

        // Verify membership was created by listing members as admin
        var membersResp = await adminClientWithTenant.GetAsync($"/workspaces/{ws.Id}/members");
        membersResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await membersResp.Content.ReadFromJsonAsync<MemberDto[]>();
        members.Should().Contain(m => m.UserId == newUserId && m.Role == "member");
    }

    [Fact]
    public async Task AcceptInvite_ExpiredToken_Returns410Gone()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-exp-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"Expired Invite Test {adminId}");

        var adminClientWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var inviteResp = await adminClientWithTenant.PostAsJsonAsync($"/workspaces/{ws.Id}/invites",
            new { email = "expired@test.com", role = "member" });
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteResponse>();

        // Manually expire the invite in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Nodefy.Api.Data.AppDbContext>();
        var inv = await db.Invitations.IgnoreQueryFilters().FirstAsync(i => i.Token == invite!.Token);
        inv.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var newUserId = Guid.NewGuid();
        var newEmail = $"expired-user-{newUserId}@test.com";
        var newClient = CreateClient(newUserId, newEmail);
        await SeedUser(newClient, newUserId, newEmail);

        var resp = await newClient.PostAsJsonAsync($"/invites/{invite!.Token}/accept", new { });
        ((int)resp.StatusCode).Should().Be(410);
    }

    [Fact]
    public async Task AcceptInvite_UnknownToken_Returns404()
    {
        var userId = Guid.NewGuid();
        var email = $"user-404-{userId}@test.com";
        var client = CreateClient(userId, email);
        await SeedUser(client, userId, email);

        var resp = await client.PostAsJsonAsync("/invites/nonexistent-token-xyz/accept", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record InviteResponse(string InviteUrl, string Token, DateTimeOffset ExpiresAt);
    private record AcceptResponse(Guid WorkspaceId);
    private record MemberDto(Guid UserId, string? Name, string Email, string? AvatarUrl, string Role, DateTimeOffset JoinedAt);
}
