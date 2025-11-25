using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Synonms.Structur.Application.Lookups;
using Synonms.Structur.Application.Products.Persistence;
using Synonms.Structur.Application.Tenants.Persistence;
using Synonms.Structur.Application.Users.Persistence;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.Transactions;

namespace Synonms.Structur.Infrastructure.MongoDb;

public class MongoDbBuilder(IHostApplicationBuilder hostApplicationBuilder)
{
    public MongoDbBuilder WithTransactionSupport()
    {
        hostApplicationBuilder.Services.Replace(new ServiceDescriptor(typeof(IDomainTransaction), typeof(MongoDomainTransaction)));

        return this;
    }
    
    public MongoDbBuilder WithPipelineRepositories()
    {
        hostApplicationBuilder.Services.AddScoped(typeof(ITenantRepository<>), typeof(MongoDbTenantRepository<>));
        hostApplicationBuilder.Services.AddScoped(typeof(IProductRepository<>), typeof(MongoDbProductRepository<>));
        hostApplicationBuilder.Services.AddScoped(typeof(IUserRepository<>), typeof(MongoDbUserRepository<>));

        return this;
    }
    
    public MongoDbBuilder WithLookups()
    {
        hostApplicationBuilder.Services.AddScoped(typeof(ILookupRepository<>), typeof(MongoDbLookupRepository<>));
        hostApplicationBuilder.Services.AddScoped(typeof(ILookupOptionsProvider), typeof(MongoDbLookupOptionsProvider));

        return this;
    }

}