using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

public static class MongoDbRecordMapper
{
    public static TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return (TDestination)Map(source, typeof(TDestination));
    }

    public static object Map(object source, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinationType);

        BsonDocument sourceDocument = source.ToBsonDocument(source.GetType());
        return BsonSerializer.Deserialize(sourceDocument, destinationType);
    }

    public static TAggregateRoot MapToAggregateRoot<TAggregateRoot>(BsonDocument document, Type recordType)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(recordType);

        object record = BsonSerializer.Deserialize(document, recordType);
        return Map<TAggregateRoot>(record);
    }

    public static BsonDocument MapToRecordDocument<TAggregateRoot>(TAggregateRoot aggregateRoot, Type recordType)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        ArgumentNullException.ThrowIfNull(aggregateRoot);
        ArgumentNullException.ThrowIfNull(recordType);

        object record = Map(aggregateRoot, recordType);
        return record.ToBsonDocument(recordType);
    }
}
