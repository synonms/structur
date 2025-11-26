using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisers;

public class FriendlyIdBsonSerialiser : IBsonSerializer<FriendlyId?>
{
    public Type ValueType => typeof(FriendlyId);

    public FriendlyId? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType is BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        
        return FriendlyId.Convert(context.Reader.ReadString());
    }
    
    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FriendlyId? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            context.Writer.WriteString(value);
        }
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else if (value is FriendlyId valueObject)
        {
            Serialize(context, args, valueObject);
        }
        else
        {
            throw new NotSupportedException($"{nameof(FriendlyIdBsonSerialiser)} does not support serialisation of {value.GetType().Name}.");
        }
    }
}