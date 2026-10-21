using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Infrastructure.MongoDb.Serialisation;
using Synonms.Structur.Infrastructure.Persistence;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

/// <summary>Normal typed capture/restore adapter. Only the explicitly selected legacy route serializes aggregates.</summary>
public sealed class MongoDbAggregateRecordMapper<TAggregate>
    where TAggregate : AggregateRoot<TAggregate>
{
    private readonly AggregateRecordMapping? _mapping;
    private readonly RawRecordBsonCodec? _codec;
    private readonly bool _legacy;

    public MongoDbAggregateRecordMapper(MongoDatabaseConfiguration configuration)
    {
        AggregatePersistenceConfiguration persistence = configuration.GetPersistenceConfiguration(typeof(TAggregate));
        _legacy = configuration.IsLegacyDirectAggregatePersistence;
        _mapping = persistence.Mapping;
        _codec = _mapping is null ? null : configuration.GetRecordCodec(typeof(TAggregate));
        if (!_legacy && _mapping is null)
        {
            throw new InvalidOperationException($"Persistence mapping for aggregate {typeof(TAggregate).Name}: missing typed capture/restore mapping.");
        }
    }

    public string StorageName(string propertyName) =>
        _legacy ? propertyName : _mapping!.Schema.Root.Properties.Single(property => property.Name == propertyName).StorageName;

    internal RawRecordSchema? Schema => _mapping?.Schema;

    internal bool IsLegacy => _legacy;

    public BsonDocument Capture(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        if (_legacy)
        {
            return aggregate.ToBsonDocument();
        }

        object record;
        try
        {
            record = _mapping!.Capture(aggregate);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            // A handwritten mapper's exception (including its inner exception) may contain values.
            throw _mapping!.Schema.Failure(_mapping.RecordType.Name, "typed capture failed");
        }

        return _codec!.Encode(record);
    }

    public TAggregate Restore(BsonDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_legacy)
        {
            return BsonSerializer.Deserialize<TAggregate>(document);
        }

        object record = _codec!.Decode(document);
        try
        {
            return (TAggregate)_mapping!.Restore(record);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException)
        {
            throw _mapping!.Schema.Failure(_mapping.RecordType.Name, "trusted typed restoration failed");
        }
    }
}
