using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nodefy.Api.Tenancy;

namespace Nodefy.Api.Data;

public class TenantDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantService _tenantService;
    public TenantDbConnectionInterceptor(ITenantService tenantService) { _tenantService = tenantService; }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (_tenantService.TenantId == Guid.Empty) return;
        await using var cmd = connection.CreateCommand();
        // Parameterised SET is not supported for SET app.* — use literal after Guid validation.
        // TenantId is a parsed Guid, not a free-form string, so injection is impossible here.
        cmd.CommandText = $"SET app.current_tenant = '{_tenantService.TenantId}'";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (_tenantService.TenantId == Guid.Empty) return;
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET app.current_tenant = '{_tenantService.TenantId}'";
        cmd.ExecuteNonQuery();
    }
}
