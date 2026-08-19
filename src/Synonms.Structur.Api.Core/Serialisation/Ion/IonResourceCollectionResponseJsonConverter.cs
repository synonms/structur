using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Api.Core.Schema.Client;
using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Api.Core.Schema.Resources;

namespace Synonms.Structur.Api.Core.Serialisation.Ion;

public class IonResourceCollectionResponseJsonConverter<TResource> : JsonConverter<ResourceCollectionResponse<TResource>>
    where TResource : Resource
{
    public override ResourceCollectionResponse<TResource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        if (jsonDocument.RootElement.TryGetProperty(IonPropertyNames.Errors, out JsonElement _))
        {
            ErrorCollectionDocument? errorCollectionDocument = JsonSerializer.Deserialize<ErrorCollectionDocument>(jsonDocument.RootElement.ToString(), options);

            if (errorCollectionDocument is null)
            {
                throw new JsonException($"Failed to deserialise {nameof(errorCollectionDocument)}.");
            }
            
            return new ResourceCollectionResponse<TResource>(errorCollectionDocument);
        }

        ResourceCollectionDocument<TResource>? resourceCollectionDocument = JsonSerializer.Deserialize<ResourceCollectionDocument<TResource>>(jsonDocument.RootElement.ToString(), options);

        if (resourceCollectionDocument is null)
        {
            throw new JsonException($"Failed to deserialise {nameof(ResourceCollectionDocument<TResource>)}.");
        }
            
        return new ResourceCollectionResponse<TResource>(resourceCollectionDocument);
    }

    public override void Write(Utf8JsonWriter writer, ResourceCollectionResponse<TResource> value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}