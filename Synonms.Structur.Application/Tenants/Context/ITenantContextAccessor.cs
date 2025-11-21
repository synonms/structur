namespace Synonms.Structur.Application.Tenants.Context;

public interface ITenantContextAccessor<TTenant>
    where TTenant : StructurTenant
{
    TenantContext<TTenant>? TenantContext { get; set; } 
}