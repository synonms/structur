using System.Linq.Expressions;
using MongoDB.Driver;
using Synonms.Structur.Application.Tenants.Context;
using Synonms.Structur.Domain.Entities;
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

    public override Expression<Func<TAggregateRoot, bool>> GlobalFilter => x => x.TenantId == (_tenantContext.GetTenantId() ?? Guid.Empty);
}