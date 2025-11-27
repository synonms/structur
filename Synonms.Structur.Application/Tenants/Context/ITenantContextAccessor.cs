namespace Synonms.Structur.Application.Tenants.Context;

public interface ITenantContextAccessor
{
    TenantContext? BaseTenantContext { get; set; }
}

public interface ITenantContextAccessor<TTenant> : ITenantContextAccessor
    where TTenant : StructurTenant
{
    TenantContext<TTenant>? TenantContext { get; set; } 
}