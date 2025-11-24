using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Domain.Lookups;

public interface ILookupRepository<TLookup> 
    where TLookup : Lookup
{
    Task<TLookup?> FindAsync(EntityId<Lookup> id);
    
    Task<IEnumerable<TLookup>> ReadAsync();
}