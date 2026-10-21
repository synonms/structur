namespace Synonms.Structur.Infrastructure.Persistence;

public sealed class AggregatePersistenceConfiguration
{
    [Obsolete("A record Type is not a restoration mapping. Use the constructor accepting AggregateRecordMapping. For explicit legacy deployments use MongoDatabaseConfiguration.UseLegacyDirectAggregatePersistence.")]
    public AggregatePersistenceConfiguration(string collectionName, Type recordType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(recordType);

        CollectionName = collectionName;
        RecordType = recordType;
    }

    public AggregatePersistenceConfiguration(string collectionName, AggregateRecordMapping mapping)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(mapping);
        CollectionName = collectionName;
        RecordType = mapping.RecordType;
        Mapping = mapping;
    }

    public string CollectionName { get; }

    public Type RecordType { get; }

    public AggregateRecordMapping? Mapping { get; }
}
