using System.Collections.ObjectModel;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Serialisation;
using Synonms.Structur.Infrastructure.Persistence;

namespace Synonms.Structur.Infrastructure.MongoDb.Hosting;

public class MongoDatabaseConfiguration
{
    private readonly IReadOnlyDictionary<Type, RawRecordBsonCodec> _codecs;
    public MongoDatabaseConfiguration(string databaseName, IDictionary<Type, string> collectionNamesByAggregateType)
        : this(databaseName, collectionNamesByAggregateType, [])
    {
    }

    /// <summary>Resolves every collection against an explicit manual mapping; no direct fallback.</summary>
    public MongoDatabaseConfiguration(string databaseName, IDictionary<Type, string> collectionNamesByAggregateType,
        IEnumerable<AggregateRecordMapping> mappings)
        : this(databaseName, Resolve(collectionNamesByAggregateType, mappings))
    {
    }

    public MongoDatabaseConfiguration(string databaseName, IDictionary<Type, AggregatePersistenceConfiguration> persistenceConfigurationsByAggregateType)
        : this(databaseName, persistenceConfigurationsByAggregateType, false)
    {
    }

    private MongoDatabaseConfiguration(string databaseName,
        IDictionary<Type, AggregatePersistenceConfiguration> configurations, bool legacy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        DatabaseName = databaseName;
        IsLegacyDirectAggregatePersistence = legacy;
        Dictionary<Type, AggregatePersistenceConfiguration> snapshot = new(configurations);
        foreach ((Type aggregateType, AggregatePersistenceConfiguration configuration) in snapshot.OrderBy(pair => pair.Key.FullName, StringComparer.Ordinal))
        {
            if (!IsAggregate(aggregateType))
            {
                throw new InvalidOperationException($"Persistence type {aggregateType.Name} is not a concrete aggregate root.");
            }

            if (!legacy && (configuration.Mapping is null || configuration.Mapping.AggregateType != aggregateType))
            {
                throw new InvalidOperationException($"Persistence mapping {aggregateType.Name}/{configuration.RecordType.Name}: missing matching typed capture/restore mapping. Register AggregateRecordMapping<TAggregate,TRecord>.");
            }
        }

        PersistenceConfigurationsByAggregateType = new ReadOnlyDictionary<Type, AggregatePersistenceConfiguration>(snapshot);
        CollectionNamesByAggregateType = new ReadOnlyDictionary<Type, string>(snapshot.ToDictionary(pair => pair.Key, pair => pair.Value.CollectionName));
        _codecs = snapshot.Where(pair => pair.Value.Mapping is not null)
            .ToDictionary(pair => pair.Key, pair => new RawRecordBsonCodec(pair.Value.Mapping!.Schema));
    }

    /// <summary>Explicit compatibility route. Never chosen because a normal mapping is missing.</summary>
    [Obsolete("Direct aggregate persistence is legacy only. Register plain records with AggregateRecordMapping<TAggregate,TRecord> and a trusted IAggregateRecordMapper<TAggregate,TRecord>.")]
    public static MongoDatabaseConfiguration UseLegacyDirectAggregatePersistence(string databaseName, IDictionary<Type, string> collectionNamesByAggregateType)
    {
#pragma warning disable CS0618
        return new MongoDatabaseConfiguration(databaseName,
            collectionNamesByAggregateType.ToDictionary(pair => pair.Key, pair => new AggregatePersistenceConfiguration(pair.Value, pair.Key)), true);
#pragma warning restore CS0618
    }

    public string DatabaseName { get; }
    public bool IsLegacyDirectAggregatePersistence { get; }
    public IDictionary<Type, string> CollectionNamesByAggregateType { get; }
    public IDictionary<Type, AggregatePersistenceConfiguration> PersistenceConfigurationsByAggregateType { get; }

    public AggregatePersistenceConfiguration GetPersistenceConfiguration(Type aggregateType) =>
        PersistenceConfigurationsByAggregateType.TryGetValue(aggregateType, out AggregatePersistenceConfiguration? configuration)
            ? configuration
            : throw new InvalidOperationException($"Persistence mapping for aggregate {aggregateType.Name} is not configured.");

    internal RawRecordBsonCodec GetRecordCodec(Type aggregateType) => _codecs[aggregateType];

    public string GetCollectionName(Type aggregateType) => GetPersistenceConfiguration(aggregateType).CollectionName;

    public string GetCollectionName<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        GetCollectionName(typeof(TAggregateRoot));

    public Type GetRecordType(Type aggregateType) => GetPersistenceConfiguration(aggregateType).RecordType;

    public Type GetRecordType<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        GetRecordType(typeof(TAggregateRoot));

    private static Dictionary<Type, AggregatePersistenceConfiguration> Resolve(IDictionary<Type, string> collections,
        IEnumerable<AggregateRecordMapping> mappings)
    {
        Dictionary<Type, AggregateRecordMapping> byAggregate = new();
        foreach (AggregateRecordMapping mapping in mappings.OrderBy(mapping => mapping.AggregateType.FullName, StringComparer.Ordinal)
                     .ThenBy(mapping => mapping.RecordType.FullName, StringComparer.Ordinal))
        {
            if (!byAggregate.TryAdd(mapping.AggregateType, mapping))
            {
                throw new InvalidOperationException($"Persistence mapping for aggregate {mapping.AggregateType.Name}: conflicting mapping registrations.");
            }
        }

        Dictionary<Type, AggregatePersistenceConfiguration> result = new();
        foreach ((Type aggregate, string collection) in collections.OrderBy(pair => pair.Key.FullName, StringComparer.Ordinal))
        {
            if (!byAggregate.TryGetValue(aggregate, out AggregateRecordMapping? mapping))
            {
                throw new InvalidOperationException($"Persistence mapping for aggregate {aggregate.Name}: missing typed capture/restore mapping. Register AggregateRecordMapping<TAggregate,TRecord>; direct aggregate persistence is not a fallback.");
            }

            result.Add(aggregate, new AggregatePersistenceConfiguration(collection, mapping));
        }

        return result;
    }

    private static bool IsAggregate(Type type)
    {
        if (type.IsAbstract || type.ContainsGenericParameters)
        {
            return false;
        }

        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>) &&
                current.GetGenericArguments()[0] == type)
            {
                return true;
            }
        }

        return false;
    }
}
