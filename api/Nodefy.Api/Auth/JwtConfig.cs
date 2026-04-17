using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Nodefy.Api.Auth;

public static class JwtConfig
{
    public static IServiceCollection AddNodefyJwtAuth(this IServiceCollection services, IConfiguration cfg)
    {
        var secret = cfg["AUTH_JWT_SECRET"] ?? cfg["AUTH_SECRET"]
            ?? throw new InvalidOperationException("AUTH_JWT_SECRET (or AUTH_SECRET) is required");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = cfg["AUTH_JWT_ISSUER"] ?? "nodefy-frontend",
                    ValidateAudience = true,
                    ValidAudience = cfg["AUTH_JWT_AUDIENCE"] ?? "nodefy-api",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                };
            });
        services.AddAuthorizationBuilder()
            .AddPolicy("admin", p => p.RequireAuthenticatedUser());  // role check happens per-endpoint
        return services;
    }
}
