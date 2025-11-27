using MongoDB.Driver;
using Synonms.Structur.Application.Tenants;
using Synonms.Structur.Application.Tenants.Persistence;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;

namespace Synonms.Structur.Infrastructure.MongoDb.Tenants;

public class MongoDbTenantRepository<TTenant> : ITenantRepository<TTenant>
    where TTenant : StructurTenant
{
    private readonly IMongoCollection<TTenant> _mongoCollection;

    public MongoDbTenantRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<TTenant>(MongoDbConstants.Database.Collections.Tenants);
    }

    public Task<IEnumerable<TTenant>> ReadAvailableTenantsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_mongoCollection.AsQueryable().AsEnumerable());

    public async Task<Maybe<TTenant>> FindSelectedTenantAsync(Guid id, CancellationToken cancellationToken)
    {
        TTenant? tenant = await _mongoCollection
            .Find(tenant => tenant.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return tenant;
    }
}
