using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisers;

public class EntityIdBsonSerialiser<TEntity> : IBsonSerializer<EntityId<TEntity>?>
    where TEntity : Entity<TEntity>
{
    public Type ValueType => typeof(EntityId<>);

    public EntityId<TEntity>? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType is BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }

        Guid guid = context.Reader.ReadGuid();
        
        return (EntityId<TEntity>)guid;
    }
    
    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, EntityId<TEntity>? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else
        {
            context.Writer.WriteGuid(value.Value);
        }
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
        }
        else if (value is EntityId<TEntity> entityId)
        {
            Serialize(context, args, entityId);
        }
        else
        {
            throw new NotSupportedException($"{nameof(MonikerBsonSerialiser)} does not support serialisation of {value.GetType().Name}.");
        }
    }
}