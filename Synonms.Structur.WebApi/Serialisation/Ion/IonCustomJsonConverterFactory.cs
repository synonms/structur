using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.WebApi.Serialisation.Ion;

public class IonCustomJsonConverterFactory : JsonConverterFactory
{
    private readonly Dictionary<Type, Type> _supportedGenericConverterTypes = new ()
    {
        { typeof(ResourceDocument<>), typeof(IonResourceDocumentJsonConverter<>) },
        { typeof(ResourceCollectionDocument<>), typeof(IonResourceCollectionDocumentJsonConverter<>) }
    };

    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsEntityId())
        {
            return true;
        }

        if (typeToConvert.IsResource())
        {
            return true;
        }

        if (typeToConvert.IsChildResource())
        {
            return true;
        }

        if (typeToConvert.IsGenericType is false)
        {
            return false;
        }

        Type genericType = typeToConvert.GetGenericTypeDefinition();

        return _supportedGenericConverterTypes.ContainsKey(genericType);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (CanConvert(typeToConvert) is false)
        {
            return null;
        }

        if (typeToConvert.IsEntityId())
        {
            return CreateEntityIdConverter(typeToConvert);
        }

        if (typeToConvert.IsResource())
        {
            return CreateResourceConverter(typeToConvert);
        }

        if (typeToConvert.IsChildResource())
        {
            return CreateChildResourceConverter(typeToConvert);
        }

        Type genericType = typeToConvert.GetGenericTypeDefinition();
        Type resourceType = typeToConvert.GetGenericArguments().Last();

        if (_supportedGenericConverterTypes.TryGetValue(genericType, out Type? genericConverterType))
        {
            Type serverConverterType = genericConverterType.MakeGenericType(resourceType);

            return (JsonConverter?)Activator.CreateInstance(serverConverterType);
        }

        return null;
    }

    private static JsonConverter? CreateResourceConverter(Type resourceType)
    {
        Type converterType = typeof(IonResourceJsonConverter<>).MakeGenericType(resourceType);
                
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    private static JsonConverter? CreateChildResourceConverter(Type childResourceType)
    {
        Type converterType = typeof(IonChildResourceJsonConverter<>).MakeGenericType(childResourceType);
                
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    private static JsonConverter? CreateEntityIdConverter(Type entityIdType)
    {
        Type? entityType = entityIdType.GetGenericArguments().First();

        if (entityType is null)
        {
            throw new InvalidOperationException($"Type '{entityIdType}' is considered an EntityId<> but the TEntity generic type parameter cannot be determined.");
        }
            
        Type converterType = typeof(IonEntityIdJsonConverter<>).MakeGenericType(entityType);
                
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}