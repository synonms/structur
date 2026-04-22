using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

public class UkBankAccountNumberBsonSerialiser : IBsonSerializer<UkBankAccountNumber?>
{
    public Type ValueType => typeof(UkBankAccountNumber);

    public UkBankAccountNumber? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType is BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        
        return UkBankAccountNumber.Convert(context.Reader.ReadString());
    }
    
    object? IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, UkBankAccountNumber? value)
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
        else if (value is UkBankAccountNumber valueObject)
        {
            Serialize(context, args, valueObject);
        }
        else
        {
            throw new NotSupportedException($"{nameof(UkBankAccountNumberBsonSerialiser)} does not support serialisation of {value.GetType().Name}.");
        }
    }
}