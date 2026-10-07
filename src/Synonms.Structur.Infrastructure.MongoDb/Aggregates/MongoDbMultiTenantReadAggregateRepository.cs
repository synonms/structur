using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Synonms.Structur.Api.Server.Tenants.Context;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

public class MongoDbMultiTenantReadAggregateRepository<TAggregateRoot> : MongoDbReadAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly ITenantContext _tenantContext;
    
    public MongoDbMultiTenantReadAggregateRepository(ITenantContext tenantContext, IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
     : base(mongoClient, mongoDatabaseConfiguration)
    {
        _tenantContext = tenantContext;
    }

    public override FilterDefinition<BsonDocument> GlobalFilter =>
        Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq(nameof(AggregateRoot<TAggregateRoot>.DeletedAction), BsonNull.Value),
            Builders<BsonDocument>.Filter.Eq(nameof(AggregateRoot<TAggregateRoot>.TenantId), _tenantContext.GetTenantId() ?? Guid.Empty));
}