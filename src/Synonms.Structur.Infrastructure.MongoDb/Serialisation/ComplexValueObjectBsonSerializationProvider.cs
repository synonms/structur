using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

internal sealed class ComplexValueObjectBsonSerializationProvider : IBsonSerializationProvider
{
    public IBsonSerializer? GetSerializer(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            return null;
        }
        Type? contract = type.GetComplexValueObjectContract();
        return contract is null ? null : (IBsonSerializer)Activator.CreateInstance(
            typeof(ComplexValueObjectBsonSerialiser<,>).MakeGenericType(contract.GetGenericArguments()))!;
    }
}
