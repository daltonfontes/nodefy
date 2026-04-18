using Microsoft.EntityFrameworkCore;
using Nodefy.Api.Auth;
using Nodefy.Api.Data;
using Nodefy.Api.Endpoints;
using Nodefy.Api.Hubs;
using Nodefy.Api.Middleware;
using Nodefy.Api.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserAccessor>();

// CRITICAL: Scoped lifetime — never Singleton (Pitfall 3)
builder.Services.AddScoped<ITenantService, TenantService>();

builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=db;Database=nodefy;Username=nodefy_app;Password=changeme_local_dev";
    opts.UseNpgsql(conn);
    opts.AddInterceptors(new TenantDbConnectionInterceptor(sp.GetRequiredService<ITenantService>()));
});

// Skip JWT auth registration in Testing environment — TestAuthHandler is added by ApiFactory
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddNodefyJwtAuth(builder.Configuration);
    builder.Services.AddAuthorization();
}
else
{
    builder.Services.AddAuthorization();
}

builder.Services.AddSignalR();

builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000")
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

app.MapGet("/health", () => Results.Ok("OK"));

app.MapSsoSyncEndpoints();
app.MapWorkspaceEndpoints();
app.MapMemberEndpoints();
app.MapInviteEndpoints();
app.MapPipelineEndpoints();
app.MapStageEndpoints();
app.MapActivityLogEndpoints();
app.MapHub<BoardHub>("/hubs/board");

app.Run();

public partial class Program { }   // for WebApplicationFactory<Program>
