using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

internal sealed class ValueObjectBsonSerializationProvider : IBsonSerializationProvider
{
    public IBsonSerializer? GetSerializer(Type type)
    {
        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            return null;
        }

        Type? valueObjectContract = type.GetInterfaces()
            .FirstOrDefault(contract => contract.IsGenericType
                                        && contract.GetGenericTypeDefinition() == typeof(IValueObject<,>)
                                        && contract.GetGenericArguments()[0] == type);

        if (valueObjectContract is null)
        {
            return null;
        }

        Type[] genericArguments = valueObjectContract.GetGenericArguments();
        Type serializerType = typeof(ValueObjectBsonSerialiser<,>).MakeGenericType(genericArguments);
        return (IBsonSerializer)Activator.CreateInstance(serializerType)!;
    }
}
