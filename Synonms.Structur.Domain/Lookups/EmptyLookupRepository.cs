using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Domain.Lookups;

public class EmptyLookupRepository<TLookup> : ILookupRepository<TLookup>
    where TLookup : Lookup
{
    public Task<TLookup?> FindAsync(EntityId<Lookup> id, CancellationToken cancellationToken = default) =>
        Task.FromResult<TLookup?>(null);

    public Task<IEnumerable<TLookup>> ReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Enumerable.Empty<TLookup>());
}