using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

/// <summary>
/// Persists a scalar value object as its underlying BSON value and reconstructs it using Rehydrate.
/// </summary>
/// <remarks>
/// All scalar types use their registered MongoDB serializer.
/// </remarks>
public class ValueObjectBsonSerialiser<TSelf, TValue> : IBsonSerializer<TSelf?>
    where TSelf : class, IScalarValueObject<TSelf, TValue>
{
    private readonly IBsonSerializer<TValue> _valueSerialiser;

    public ValueObjectBsonSerialiser()
    {
        _valueSerialiser = BsonSerializer.LookupSerializer<TValue>();
    }

    public Type ValueType => typeof(TSelf);

    public TSelf? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType is BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }

        BsonDeserializationArgs valueArgs = new() { NominalType = typeof(TValue) };
        TValue value = _valueSerialiser.Deserialize(context, valueArgs);
        return TSelf.Rehydrate(value);
    }

    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TSelf? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }

        BsonSerializationArgs valueArgs = new() { NominalType = typeof(TValue) };
        _valueSerialiser.Serialize(context, valueArgs, value.Value);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else if (value is TSelf valueObject)
        {
            Serialize(context, args, valueObject);
        }
        else
        {
            throw new NotSupportedException($"{GetType().Name} does not support serialisation of {value.GetType().Name}.");
        }
    }
}
