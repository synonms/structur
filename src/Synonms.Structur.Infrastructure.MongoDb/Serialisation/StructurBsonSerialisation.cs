using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Core.Entities;

namespace Synonms.Structur.Infrastructure.MongoDb.Serialisation;

public static class StructurBsonSerialisation
{
    private static readonly Lock RegistrationLock = new();
    private static bool _entityIdSerializerDefinitionRegistered;
    private static bool _valueObjectProviderRegistered;

    /// <summary>
    /// Registers explicit serializers and automatic serialization for scalar value objects.
    /// </summary>
    /// <remarks>
    /// Call before MongoDB first looks up serializers for domain types. Explicit registrations take
    /// precedence over the value-object fallback, and repeated calls preserve existing registrations.
    /// Value-object assemblies do not need to be supplied unless they contain explicit serializers.
    /// </remarks>
    public static void TryRegisterSerialisersFrom(params Assembly[] assembliesContainingSerialisers)
    {
        RegisterEntityIdSerializerDefinition();

        RegisterFrom(InfrastructureMongoDbProject.Assembly);

        foreach (Assembly assembly in assembliesContainingSerialisers)
        {
            RegisterFrom(assembly);
        }

        RegisterValueObjectProvider();
    }

    private static void RegisterFrom(Assembly assembly)
    {
        List<Type> serialiserTypes = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false }
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
                catch (BsonSerializationException) when (
                    BsonSerializer.LookupSerializer(serialisedType).ValueType == serialisedType)
                {
                    // MongoDB throws for a different existing instance; keep the first registration.
                }
            }
        }
    }

    private static void RegisterValueObjectProvider()
    {
        lock (RegistrationLock)
        {
            if (_valueObjectProviderRegistered)
            {
                return;
            }

            BsonSerializer.RegisterSerializationProvider(new ValueObjectBsonSerializationProvider());
            _valueObjectProviderRegistered = true;
        }
    }

    private static void RegisterEntityIdSerializerDefinition()
    {
        lock (RegistrationLock)
        {
            if (_entityIdSerializerDefinitionRegistered)
            {
                return;
            }

            BsonSerializer.RegisterGenericSerializerDefinition(typeof(EntityId<>), typeof(EntityIdBsonSerialiser<>));
            _entityIdSerializerDefinitionRegistered = true;
        }
    }
}