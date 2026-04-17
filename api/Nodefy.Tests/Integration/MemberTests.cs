using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class MemberTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;

    public MemberTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

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

    private async Task<Guid> AddMember(HttpClient adminClient, WorkspaceDto ws, string memberEmail)
    {
        var memberId = Guid.NewGuid();
        var memberClient = CreateClient(memberId, memberEmail);
        await SeedUser(memberClient, memberId, memberEmail);

        // Admin creates invite
        var adminWithTenant = CreateClient(
            Guid.Parse(adminClient.DefaultRequestHeaders.GetValues("X-Test-User-Id").First()),
            adminClient.DefaultRequestHeaders.GetValues("X-Test-Email").First(),
            ws.Id);
        var inviteResp = await adminWithTenant.PostAsJsonAsync($"/workspaces/{ws.Id}/invites",
            new { email = memberEmail, role = "member" });
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteResponse>();

        // Member accepts invite
        await memberClient.PostAsJsonAsync($"/invites/{invite!.Token}/accept", new { });

        return memberId;
    }

    [Fact]
    public async Task GetMembers_AsAdmin_ReturnsList()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-list-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"Members List Test {adminId}");

        var adminWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var resp = await adminWithTenant.GetAsync($"/workspaces/{ws.Id}/members");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await resp.Content.ReadFromJsonAsync<MemberDto[]>();
        members.Should().NotBeNull();
        members!.Should().HaveCountGreaterThanOrEqualTo(1);
        members.Should().Contain(m => m.UserId == adminId && m.Role == "admin");
    }

    [Fact]
    public async Task GetMembers_AsNonAdmin_Returns403()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-403-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"NonAdmin Test {adminId}");

        // Add a non-admin member
        var memberId = Guid.NewGuid();
        var memberEmail = $"member-403-{memberId}@test.com";
        var memberClient = CreateClient(memberId, memberEmail);
        await SeedUser(memberClient, memberId, memberEmail);

        var adminWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var inviteResp = await adminWithTenant.PostAsJsonAsync($"/workspaces/{ws.Id}/invites",
            new { email = memberEmail, role = "member" });
        var invite = await inviteResp.Content.ReadFromJsonAsync<InviteResponse>();
        await memberClient.PostAsJsonAsync($"/invites/{invite!.Token}/accept", new { });

        // Non-admin tries to list members
        var memberWithTenant = CreateClient(memberId, memberEmail, ws.Id);
        var resp = await memberWithTenant.GetAsync($"/workspaces/{ws.Id}/members");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatchRole_PromotesMemberToAdmin()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-promote-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"Promote Test {adminId}");

        var memberId = Guid.NewGuid();
        var memberEmail = $"member-promote-{memberId}@test.com";
        await AddMember(adminClient, ws, memberEmail);

        var adminWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var resp = await adminWithTenant.PatchAsJsonAsync($"/workspaces/{ws.Id}/members/{memberId}", new { role = "admin" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify promotion
        var membersResp = await adminWithTenant.GetAsync($"/workspaces/{ws.Id}/members");
        var members = await membersResp.Content.ReadFromJsonAsync<MemberDto[]>();
        members.Should().Contain(m => m.UserId == memberId && m.Role == "admin");
    }

    [Fact]
    public async Task PatchRole_RefusesToDemoteLastAdmin_With409()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-lastadmin-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"Last Admin Test {adminId}");

        // Admin tries to demote themselves (they are the only admin)
        var adminWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var resp = await adminWithTenant.PatchAsJsonAsync($"/workspaces/{ws.Id}/members/{adminId}", new { role = "member" });
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteMember_RemovesMembership()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-delete-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"Delete Member Test {adminId}");

        var memberId = Guid.NewGuid();
        var memberEmail = $"member-delete-{memberId}@test.com";
        await AddMember(adminClient, ws, memberEmail);

        var adminWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var resp = await adminWithTenant.DeleteAsync($"/workspaces/{ws.Id}/members/{memberId}");
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify removal
        var membersResp = await adminWithTenant.GetAsync($"/workspaces/{ws.Id}/members");
        var members = await membersResp.Content.ReadFromJsonAsync<MemberDto[]>();
        members.Should().NotContain(m => m.UserId == memberId);
    }

    [Fact]
    public async Task DeleteMember_RefusesToRemoveSelf_With409()
    {
        var adminId = Guid.NewGuid();
        var adminEmail = $"admin-self-{adminId}@test.com";
        var adminClient = CreateClient(adminId, adminEmail);
        await SeedUser(adminClient, adminId, adminEmail);
        var ws = await CreateWorkspace(adminClient, $"Self Remove Test {adminId}");

        // Admin tries to remove themselves
        var adminWithTenant = CreateClient(adminId, adminEmail, ws.Id);
        var resp = await adminWithTenant.DeleteAsync($"/workspaces/{ws.Id}/members/{adminId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private record WorkspaceDto(Guid Id, string Name, string Slug, string Currency, bool CurrencyLocked, string? Role);
    private record InviteResponse(string InviteUrl, string Token, DateTimeOffset ExpiresAt);
    private record MemberDto(Guid UserId, string? Name, string Email, string? AvatarUrl, string Role, DateTimeOffset JoinedAt);
}
