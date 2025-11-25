using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Tenants.Persistence;

public class NoStructurTenantRepository : ITenantRepository<NoStructurTenant>
{
    public Task<Maybe<NoStructurTenant>> FindSelectedTenantAsync(Guid id, CancellationToken cancellationToken) =>
        Maybe<NoStructurTenant>.NoneAsync;

    public Task<IEnumerable<NoStructurTenant>> ReadAvailableTenantsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Enumerable.Empty<NoStructurTenant>());
}