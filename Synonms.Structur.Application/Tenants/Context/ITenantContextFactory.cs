namespace Synonms.Structur.Application.Tenants.Context;

public interface ITenantContextFactory<TTenant>
    where TTenant : StructurTenant
{
    Task<TenantContext<TTenant>> CreateAsync(Guid? selectedTenantId, CancellationToken cancellationToken);
}