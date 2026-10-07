namespace Synonms.Structur.Infrastructure.Persistence;

public sealed class AggregatePersistenceConfiguration
{
    public AggregatePersistenceConfiguration(string collectionName, Type recordType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(recordType);

        CollectionName = collectionName;
        RecordType = recordType;
    }

    public string CollectionName { get; }

    public Type RecordType { get; }
}
