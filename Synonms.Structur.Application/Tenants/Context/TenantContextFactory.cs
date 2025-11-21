using Synonms.Structur.Application.Tenants.Persistence;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Tenants.Context;

public class TenantContextFactory<TTenant> : ITenantContextFactory<TTenant>
    where TTenant : StructurTenant
{
    private readonly ITenantRepository<TTenant> _repository;

    public TenantContextFactory(ITenantRepository<TTenant> repository)
    {
        _repository = repository;
    }
        
    public async Task<TenantContext<TTenant>> CreateAsync(Guid? selectedTenantId, CancellationToken cancellationToken)
    {
        TTenant? selectedTenant = null;
        
        if (selectedTenantId is not null)
        {
            await _repository.FindSelectedTenantAsync(selectedTenantId.Value, cancellationToken)
                .IfSomeAsync(tenant => selectedTenant = tenant);
        }

        IEnumerable<TTenant> availableTenants = await _repository.ReadAvailableTenantsAsync(cancellationToken);

        return TenantContext<TTenant>.Create(availableTenants, selectedTenant);
    }
}