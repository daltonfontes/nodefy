namespace Nodefy.Api.Tenancy;

public class TenantService : ITenantService
{
    public Guid TenantId { get; private set; } = Guid.Empty;
    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
