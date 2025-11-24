using System.Reflection;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Infrastructure.MongoDb;

public static class StructurDomainClassMaps
{
    public static void TryRegisterClassMapsForDomainEventsFrom(params Assembly[] assembliesContainingDomainEvents)
    {
        BsonClassMap.TryRegisterClassMap<DomainEvent>(classMap => {
            classMap.AutoMap();
            classMap.SetIsRootClass(true);
        });

        foreach (Assembly assembly in assembliesContainingDomainEvents)
        {
            RegisterFrom(assembly);
        }
    }

    private static void RegisterFrom(Assembly assembly)
    {
        List<Type> domainEventTypes = assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, BaseType.IsGenericType: true }
                           && type.BaseType.GetGenericTypeDefinition() == typeof(DomainEvent<>))
            .ToList();

        foreach (Type domainEventType in domainEventTypes)
        {
            MethodInfo? tryRegisterClassMapMethodInfo = typeof(BsonClassMap)
                .GetMethod(nameof(BsonClassMap.TryRegisterClassMap), BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Standard, [], null)?
                .MakeGenericMethod(domainEventType);

            if (tryRegisterClassMapMethodInfo is not null)
            {
                Console.WriteLine("Registering BSON ClassMap for type " + domainEventType.Name);
                tryRegisterClassMapMethodInfo.Invoke(null, null);
            }
        }
    }
}