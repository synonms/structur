using Synonms.Structur.Application.Tenants.Faults;
using Synonms.Structur.Application.Tenants.Persistence;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Tenants.Context;

public class TenantContext : ITenantContext
{
    protected Guid? TenantId;

    public Guid? GetTenantId() => TenantId;
    
    public bool HasTenantId() => TenantId is not null;
}

public class TenantContext<TTenant> : TenantContext, ITenantContext<TTenant> where TTenant : StructurTenant
{
    private readonly ITenantRepository<TTenant> _repository;
    private TTenant? _tenant;

    public TenantContext(ITenantRepository<TTenant> repository)
    {
        _repository = repository;
    }
    
    public Result<TTenant> GetTenant() => 
        TenantId is null 
            ? new TenantIdResolutionFault() 
            : _tenant is null 
                ? new TenantResolutionFault(TenantId.Value) 
                : _tenant;

    public bool HasTenant() => _tenant is not null;

    public async Task SelectTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        TenantId = tenantId;

        if (tenantId is null)
        {
            _tenant = null;
            return;
        }
        
        await _repository.FindSelectedTenantAsync(tenantId.Value, cancellationToken)
            .MatchAsync(
                tenant => _tenant = tenant, 
                () => _tenant = null);
    }
}