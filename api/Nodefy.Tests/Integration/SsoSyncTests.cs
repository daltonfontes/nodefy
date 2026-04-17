using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Nodefy.Tests.Fixtures;
using Xunit;

namespace Nodefy.Tests.Integration;

public class SsoSyncTests : IClassFixture<PostgresFixture>
{
    private readonly ApiFactory _factory;

    public SsoSyncTests(PostgresFixture fixture) => _factory = new ApiFactory(fixture.ConnectionString);

    private HttpClient CreateClient(Guid userId, string email)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        return client;
    }

    [Fact]
    public async Task PostSsoSync_CreatesUserOnFirstCall()
    {
        var userId = Guid.NewGuid();
        var email = $"new-user-{userId}@test.com";
        var client = CreateClient(userId, email);

        var resp = await client.PostAsJsonAsync("/sso/sync", new
        {
            provider = "github",
            providerAccountId = userId.ToString(),
            email,
            name = "Test User",
            avatarUrl = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await resp.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostSsoSync_IsIdempotent_DoesNotCreateDuplicate()
    {
        var userId = Guid.NewGuid();
        var email = $"idempotent-{userId}@test.com";
        var client = CreateClient(userId, email);

        var payload = new
        {
            provider = "google",
            providerAccountId = userId.ToString(),
            email,
            name = "Idempotent User",
            avatarUrl = (string?)null
        };

        // First call
        var resp1 = await client.PostAsJsonAsync("/sso/sync", payload);
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        var user1 = await resp1.Content.ReadFromJsonAsync<UserDto>();

        // Second call — same provider + providerAccountId
        var resp2 = await client.PostAsJsonAsync("/sso/sync", payload);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var user2 = await resp2.Content.ReadFromJsonAsync<UserDto>();

        // Both calls return the same user ID
        user1!.Id.Should().Be(user2!.Id);
    }

    [Fact]
    public async Task PostSsoSync_AcceptsNonNullEmail_FromGitHubFallbackPayload()
    {
        // AUTH-01 GitHub null-email pitfall: frontend must use /user/emails fallback and send a non-null email
        var userId = Guid.NewGuid();
        var email = $"github-verified-{userId}@users.noreply.github.com";
        var client = CreateClient(userId, email);

        // Simulating what the frontend sends after the /user/emails fallback
        var resp = await client.PostAsJsonAsync("/sso/sync", new
        {
            provider = "github",
            providerAccountId = $"gh-{userId}",
            email,   // non-null verified email from /user/emails endpoint
            name = "GitHub User",
            avatarUrl = "https://avatars.githubusercontent.com/u/12345"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await resp.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
    }

    private record UserDto(Guid Id, string Email, string? Name, string? AvatarUrl);
}
