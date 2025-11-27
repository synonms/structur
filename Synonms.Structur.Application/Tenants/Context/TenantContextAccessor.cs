namespace Synonms.Structur.Application.Tenants.Context;

public class TenantContextAccessor<TTenant> : ITenantContextAccessor<TTenant>
    where TTenant : StructurTenant
{
    public TenantContext? BaseTenantContext { get; set; }

    public TenantContext<TTenant>? TenantContext { get; set; }
}