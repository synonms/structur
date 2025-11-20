using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Lookups;

namespace Synonms.Structur.Application.Persistence;

public interface ILookupRepository<TLookup> 
    where TLookup : Lookup
{
    Task<TLookup?> FindAsync(EntityId<Lookup> id);
    
    Task<IEnumerable<TLookup>> ReadAsync();
}