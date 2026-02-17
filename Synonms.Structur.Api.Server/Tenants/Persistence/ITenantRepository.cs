using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Tenants.Persistence;

public interface ITenantRepository<TTenant>
    where TTenant : StructurTenant
{
    Task<Maybe<TTenant>> FindSelectedTenantAsync(Guid id, CancellationToken cancellationToken);
    
    Task<IEnumerable<TTenant>> ReadAvailableTenantsAsync(CancellationToken cancellationToken);
}