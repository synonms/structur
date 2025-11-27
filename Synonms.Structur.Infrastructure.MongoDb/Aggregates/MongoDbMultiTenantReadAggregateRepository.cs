using System.Linq.Expressions;
using MongoDB.Driver;
using Synonms.Structur.Application.Tenants.Context;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

public class MongoDbMultiTenantReadAggregateRepository<TAggregateRoot> : MongoDbReadAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    
    public MongoDbMultiTenantReadAggregateRepository(ITenantContextAccessor tenantContextAccessor, IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
     : base(mongoClient, mongoDatabaseConfiguration)
    {
        _tenantContextAccessor = tenantContextAccessor;
    }

    public override Expression<Func<TAggregateRoot, bool>> GlobalFilter => x => x.TenantId == _tenantContextAccessor.BaseTenantContext!.BaseSelectedTenant!.Id;
}