using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Data.Entities;

namespace Nodefy.Api.Endpoints;

public static class SsoSyncEndpoints
{
    public record SsoSyncRequest(string Provider, string ProviderAccountId, string Email, string? Name, string? AvatarUrl);
    public record UserDto(Guid Id, string Email, string? Name, string? AvatarUrl);

    public static IEndpointRouteBuilder MapSsoSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/sso/sync", async (SsoSyncRequest req, AppDbContext db, CurrentUserAccessor user) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest(new { error = "email is required (GitHub null-email pitfall — frontend must use /user/emails fallback)" });
            // Upsert by (provider, providerAccountId)
            var existing = await db.Users.FirstOrDefaultAsync(u =>
                u.Provider == req.Provider && u.ProviderAccountId == req.ProviderAccountId);
            if (existing is null)
            {
                existing = new User
                {
                    Id = Guid.NewGuid(),
                    Email = req.Email,
                    Name = req.Name,
                    AvatarUrl = req.AvatarUrl,
                    Provider = req.Provider,
                    ProviderAccountId = req.ProviderAccountId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                db.Users.Add(existing);
            }
            else
            {
                existing.Email = req.Email;
                existing.Name = req.Name ?? existing.Name;
                existing.AvatarUrl = req.AvatarUrl ?? existing.AvatarUrl;
            }
            await db.SaveChangesAsync();
            return Results.Ok(new UserDto(existing.Id, existing.Email, existing.Name, existing.AvatarUrl));
        }).RequireAuthorization();
        return app;
    }
}
