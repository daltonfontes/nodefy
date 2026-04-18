using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Data;

/// <summary>
/// Design-time factory used by `dotnet ef` tooling only.
/// Not used at runtime — the DI-registered AppDbContext is used instead.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // Use a placeholder connection string for design-time (migration generation only).
        // The real connection string is injected via DI at runtime.
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=nodefy;Username=nodefy_app;Password=changeme_local_dev");

        return new AppDbContext(optionsBuilder.Options, new NullTenantService());
    }

    /// <summary>
    /// No-op tenant service for design-time context creation.
    /// </summary>
    private sealed class NullTenantService : ITenantService
    {
        public Guid TenantId => Guid.Empty;
        public void SetTenant(Guid tenantId) { }
    }
}
