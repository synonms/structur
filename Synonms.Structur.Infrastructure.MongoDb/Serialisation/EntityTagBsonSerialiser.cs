using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

public class EntityTagBsonSerialiser : IBsonSerializer<EntityTag?>
{
    public Type ValueType => typeof(EntityTag);

    public EntityTag? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType is BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        
        return context.Reader.ReadGuid();
    }

    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EntityTag? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            context.Writer.WriteGuid(value);
        }
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else if (value is EntityTag entityTag)
        {
            Serialize(context, args, entityTag);
        }
        else
        {
            throw new NotSupportedException($"{nameof(EntityTagBsonSerialiser)} does not support serialisation of {value.GetType().Name}.");
        }
    }
}