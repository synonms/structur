using MongoDB.Driver;
using Synonms.Structur.Application.Products;
using Synonms.Structur.Application.Products.Persistence;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;

namespace Synonms.Structur.Infrastructure.MongoDb.Products;

public class MongoDbProductRepository<TProduct> : IProductRepository<TProduct>
    where TProduct : StructurProduct
{
    private readonly IMongoCollection<TProduct> _mongoCollection;

    public MongoDbProductRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<TProduct>(MongoDbConstants.Database.Collections.Tenants);
    }

    public Task<IEnumerable<TProduct>> ReadAvailableProductsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_mongoCollection.AsQueryable().AsEnumerable());

    public async Task<Maybe<TProduct>> FindSelectedProductAsync(Guid id, CancellationToken cancellationToken)
    {
        TProduct? product = await _mongoCollection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return product;
    }
}