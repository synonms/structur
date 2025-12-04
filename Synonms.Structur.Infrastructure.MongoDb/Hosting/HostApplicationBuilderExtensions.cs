using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Synonms.Structur.Application.Tenants;
using Synonms.Structur.Application.Tenants.Persistence;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Transactions;
using Synonms.Structur.Infrastructure.MongoDb.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Events;
using Synonms.Structur.Infrastructure.MongoDb.Serialisation;
using Synonms.Structur.Infrastructure.MongoDb.Tenants;

namespace Synonms.Structur.Infrastructure.MongoDb.Hosting;

public static class HostApplicationBuilderExtensions
{
    public static MongoDbBuilder AddStructurMongoDb(this IHostApplicationBuilder builder, MongoDatabaseConfiguration mongoDatabaseConfiguration, string connectionStringName, params Assembly[] domainAssemblies) =>
        AddStructurMongoDb<NoStructurTenant>(builder, mongoDatabaseConfiguration, connectionStringName, domainAssemblies);

    public static MongoDbBuilder AddStructurMongoDb<TTenant>(this IHostApplicationBuilder builder, MongoDatabaseConfiguration mongoDatabaseConfiguration, string connectionStringName, params Assembly[] domainAssemblies)
        where TTenant : StructurTenant
    {
        ConventionPack conventionPack = [new IgnoreExtraElementsConvention(true)];
        ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        StructurBsonSerialisation.TryRegisterSerialisersFrom(domainAssemblies);
        StructurDomainClassMaps.TryRegisterClassMapsForDomainEventsFrom(domainAssemblies);

        string? connectionString = builder.Configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new NullReferenceException($"The MongoDB connection string '{connectionStringName}' is not set.");    
        }

        builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(MongoClientSettings.FromConnectionString(connectionString)));
        builder.Services.AddSingleton(_ => mongoDatabaseConfiguration);
        builder.Services.AddScoped<IDomainTransaction, NoOpDomainTransaction>();
        
        builder.Services.AddScoped(typeof(IWriteAggregateRepository<>), typeof(MongoDbWriteAggregateRepository<>));
        Console.WriteLine($"Tenant type: {typeof(TTenant).Name}");
        if (typeof(TTenant) == typeof(NoStructurTenant))
        {
            Console.WriteLine("Adding Read Repository type: MongoDbReadAggregateRepository");
            builder.Services.AddScoped(typeof(IReadAggregateRepository<>), typeof(MongoDbReadAggregateRepository<>));
        }
        else
        {
            Console.WriteLine("Adding Read Repository type: MongoDbMultiTenantReadAggregateRepository");
            builder.Services.AddScoped(typeof(IReadAggregateRepository<>), typeof(MongoDbMultiTenantReadAggregateRepository<>));
        }
        builder.Services.AddScoped(typeof(IDomainEventRepository<>), typeof(MongoDbDomainEventRepository<>));
        builder.Services.AddScoped(typeof(ITenantRepository<>), typeof(MongoDbTenantRepository<>));


        builder.Services.AddTransient(_ => TimeProvider.System);

        builder.Services.AddHealthChecks().AddMongoDb();

        return new MongoDbBuilder(builder);
    }
}