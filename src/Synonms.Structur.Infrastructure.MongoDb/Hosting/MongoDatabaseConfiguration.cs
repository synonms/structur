using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.Persistence;

namespace Synonms.Structur.Infrastructure.MongoDb.Hosting;

public class MongoDatabaseConfiguration
{
    public MongoDatabaseConfiguration(string databaseName, IDictionary<Type, string> collectionNamesByAggregateType)
        : this(databaseName, collectionNamesByAggregateType.ToDictionary(
            x => x.Key,
            x => new AggregatePersistenceConfiguration(x.Value, x.Key)))
    {
    }

    public MongoDatabaseConfiguration(string databaseName, IDictionary<Type, AggregatePersistenceConfiguration> persistenceConfigurationsByAggregateType)
    {
        DatabaseName = databaseName;
        PersistenceConfigurationsByAggregateType = new Dictionary<Type, AggregatePersistenceConfiguration>(persistenceConfigurationsByAggregateType);
        CollectionNamesByAggregateType = PersistenceConfigurationsByAggregateType.ToDictionary(x => x.Key, x => x.Value.CollectionName);
    }
    
    public string DatabaseName { get; }
    
    public IDictionary<Type, string> CollectionNamesByAggregateType { get; }

    public IDictionary<Type, AggregatePersistenceConfiguration> PersistenceConfigurationsByAggregateType { get; }

    public string GetCollectionName(Type aggregateType)
    {
        if (PersistenceConfigurationsByAggregateType.TryGetValue(aggregateType, out AggregatePersistenceConfiguration? persistenceConfiguration) is false)
        {
            throw new InvalidOperationException($"Mongo collection name for type {aggregateType.Name} is not configured.");
        }

        return persistenceConfiguration.CollectionName;
    }

    public string GetCollectionName<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        GetCollectionName(typeof(TAggregateRoot));

    public Type GetRecordType(Type aggregateType)
    {
        if (PersistenceConfigurationsByAggregateType.TryGetValue(aggregateType, out AggregatePersistenceConfiguration? persistenceConfiguration) is false)
        {
            throw new InvalidOperationException($"Mongo record type for aggregate {aggregateType.Name} is not configured.");
        }

        return persistenceConfiguration.RecordType;
    }

    public Type GetRecordType<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        GetRecordType(typeof(TAggregateRoot));
}