using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

/// <summary>Persists the domain-owned data shape and reconstructs through trusted Rehydrate.</summary>
public sealed class ComplexValueObjectBsonSerialiser<TSelf, TData> : SerializerBase<TSelf?>, IBsonDocumentSerializer
    where TSelf : class, IComplexValueObject<TSelf, TData>
    where TData : class
{
    private readonly IBsonSerializer<TData> _dataSerializer = BsonSerializer.LookupSerializer<TData>();

    public override TSelf? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        try
        {
            TData data = _dataSerializer.Deserialize(context, new BsonDeserializationArgs { NominalType = typeof(TData) });
            return TSelf.Rehydrate(data);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException or BsonSerializationException)
        {
            // DTO serializers can include raw values in their exceptions. Do not leak them or their
            // inner exceptions; report the affected contract without sensitive persisted content.
            throw new BsonSerializationException($"Cannot rehydrate complex value object {typeof(TSelf).Name}: required data is missing/null or the persisted member shape is malformed.");
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TSelf? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }
        _dataSerializer.Serialize(context, new BsonSerializationArgs { NominalType = typeof(TData) }, value.ToData());
    }

    public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
    {
        if (_dataSerializer is IBsonDocumentSerializer documentSerializer
            && documentSerializer.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo dataInfo))
        {
            Type? memberType = typeof(TSelf).GetProperty(memberName)?.PropertyType;
            serializationInfo = memberType is null
                ? dataInfo
                : new BsonSerializationInfo(dataInfo.ElementName, BsonSerializer.LookupSerializer(memberType), memberType);
            return true;
        }
        serializationInfo = null!;
        return false;
    }
}
