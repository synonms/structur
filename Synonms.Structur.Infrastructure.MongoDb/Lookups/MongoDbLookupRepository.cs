using MongoDB.Driver;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;

namespace Synonms.Structur.Infrastructure.MongoDb.Lookups;

public class MongoDbLookupRepository<TLookup> : ILookupRepository<TLookup>
    where TLookup : Lookup
{
    private readonly IMongoCollection<TLookup> _mongoCollection;

    public MongoDbLookupRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<TLookup>(MongoDbConstants.Database.Collections.Lookups);
    }
    
    public async Task<TLookup?> FindAsync(EntityId<Lookup> id, CancellationToken cancellationToken = default)
    {
        TLookup? lookup = await _mongoCollection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return lookup;
    }

    public Task<IEnumerable<TLookup>> ReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_mongoCollection.AsQueryable().AsEnumerable());
}