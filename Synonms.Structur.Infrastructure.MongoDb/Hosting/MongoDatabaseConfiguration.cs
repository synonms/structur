namespace Synonms.Structur.Infrastructure.MongoDb.Hosting;

public class MongoDatabaseConfiguration
{
    public MongoDatabaseConfiguration(string databaseName, IDictionary<Type, string> collectionNamesByAggregateType)
    {
        DatabaseName = databaseName;
        CollectionNamesByAggregateType = collectionNamesByAggregateType;
    }
    
    public string DatabaseName { get; }
    
    public IDictionary<Type, string> CollectionNamesByAggregateType { get; }
}