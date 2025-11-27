using System.Reflection;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

public static class StructurBsonSerialisation
{
    public static void TryRegisterSerialisersFrom(params Assembly[] assembliesContainingSerialisers)
    {
        BsonSerializer.RegisterGenericSerializerDefinition(typeof(EntityId<>), typeof(EntityIdBsonSerialiser<>));

        RegisterFrom(InfrastructureMongoDbProject.Assembly);

        foreach (Assembly assembly in assembliesContainingSerialisers)
        {
            RegisterFrom(assembly);
        }
    }

    private static void RegisterFrom(Assembly assembly)
    {
        List<Type> serialiserTypes = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && type != typeof(EntityIdBsonSerialiser<>)
                           && type.GetInterfaces()
                               .Where(i => i.IsGenericType)
                               .Any(i => i.GetGenericTypeDefinition() == typeof(IBsonSerializer<>)))
            .ToList();

        foreach (Type serialiserType in serialiserTypes)
        {
            Type serialisedType = serialiserType.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBsonSerializer<>)).GetGenericArguments()[0];

            if (Activator.CreateInstance(serialiserType) is IBsonSerializer bsonSerialiser)
            {
                try
                {
                    BsonSerializer.TryRegisterSerializer(serialisedType, bsonSerialiser);
                }
                catch
                {   // If the serialiser is already registered, we can ignore the exception.
                    // This is useful for testing scenarios where multiple assemblies may try to register the same serialiser.
                }
            }
        }
    }
}