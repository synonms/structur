using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Api.Core.Schema.Client;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;

namespace Synonms.Structur.Api.Core.Serialisation.Default;

public class DefaultCustomJsonConverterFactory : JsonConverterFactory
{
    private readonly Version _version;

    private readonly Dictionary<Type, Type> _supportedGenericConverterTypes = new ()
    {
        { typeof(ResourceDocument<>), typeof(DefaultResourceDocumentJsonConverter<>) },
        { typeof(ResourceCollectionDocument<>), typeof(DefaultResourceCollectionDocumentJsonConverter<>) },
        { typeof(ResourceResponse<>), typeof(DefaultResourceResponseJsonConverter<>) },
        { typeof(ResourceCollectionResponse<>), typeof(DefaultResourceCollectionResponseJsonConverter<>) }
    };

    public DefaultCustomJsonConverterFactory(Version version)
    {
        _version = version;
    }
    
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

    private JsonConverter? CreateResourceConverter(Type resourceType)
    {
        Type converterType = typeof(DefaultResourceJsonConverter<>).MakeGenericType(resourceType);
        ConstructorInfo? constructorWithVersion = converterType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(Version)]);
        object? instance = constructorWithVersion?.Invoke([_version]);        
        
        return (JsonConverter?)instance;
    }

    private JsonConverter? CreateChildResourceConverter(Type childResourceType)
    {
        Type converterType = typeof(DefaultChildResourceJsonConverter<>).MakeGenericType(childResourceType);
        ConstructorInfo? constructorWithVersion = converterType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(Version)]);
        object? instance = constructorWithVersion?.Invoke([_version]);        
        
        return (JsonConverter?)instance;
    }

    private static JsonConverter? CreateEntityIdConverter(Type entityIdType)
    {
        Type? entityType = entityIdType.GetGenericArguments().First();

        if (entityType is null)
        {
            throw new InvalidOperationException($"Type '{entityIdType}' is considered an EntityId<> but the TEntity generic type parameter cannot be determined.");
        }
            
        Type converterType = typeof(DefaultEntityIdJsonConverter<>).MakeGenericType(entityType);
                
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}