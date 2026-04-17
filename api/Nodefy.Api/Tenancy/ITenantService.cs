namespace Nodefy.Api.Tenancy;

public interface ITenantService
{
    Guid TenantId { get; }
    void SetTenant(Guid tenantId);
}
