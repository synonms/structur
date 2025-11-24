using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Synonms.Structur.Application.Tenants.Persistence;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Transactions;

namespace Synonms.Structur.Infrastructure.MongoDb;

public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddStructurMongoDb(this IHostApplicationBuilder builder, MongoDatabaseConfiguration mongoDatabaseConfiguration, string connectionStringName, params Assembly[] domainAssemblies)
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
        builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        builder.Services.AddScoped<IDomainTransaction, MongoDomainTransaction>();
        
        builder.Services.AddScoped(typeof(IWriteAggregateRepository<>), typeof(MongoDbWriteAggregateRepository<>));
        builder.Services.AddScoped(typeof(IReadAggregateRepository<>), typeof(MongoDbReadAggregateRepository<>));
        builder.Services.AddScoped(typeof(IDomainEventRepository<>), typeof(MongoDbDomainEventRepository<>));
        builder.Services.AddScoped(typeof(ITenantRepository<>), typeof(MongoDbTenantRepository<>));

        builder.Services.AddTransient(_ => TimeProvider.System);

        builder.Services.AddHealthChecks().AddMongoDb();

        return builder;
    }
}