using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisers;

public class BirthDateBsonSerialiser : IBsonSerializer<BirthDate?>
{
    public Type ValueType => typeof(BirthDate);
    
    public BirthDate? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType is BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        
        long millisecondsSinceEpoch = context.Reader.ReadDateTime();
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(millisecondsSinceEpoch);
        return BirthDate.Convert(DateOnly.FromDateTime(dateTimeOffset.UtcDateTime));
    }
    
    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, BirthDate? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            DateTimeOffset dateTimeOffset = new(value.Value.ToDateTime(TimeOnly.MinValue));
            long millisecondsSinceEpoch = dateTimeOffset.ToUnixTimeMilliseconds();
            context.Writer.WriteDateTime(millisecondsSinceEpoch);
        }
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else if (value is BirthDate valueObject)
        {
            Serialize(context, args, valueObject);
        }
        else
        {
            throw new NotSupportedException($"{nameof(BirthDateBsonSerialiser)} does not support serialisation of {value.GetType().Name}.");
        }
    }
}