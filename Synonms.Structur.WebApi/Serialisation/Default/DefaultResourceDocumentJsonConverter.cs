using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Resources;

namespace Synonms.Structur.WebApi.Serialisation.Default;

public class DefaultResourceDocumentJsonConverter<TResource> : JsonConverter<ResourceDocument<TResource>>
    where TResource : Resource
{
    public override ResourceDocument<TResource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        JsonElement valueElement = jsonDocument.RootElement.GetProperty(DefaultPropertyNames.Value);

        TResource? clientResource = JsonSerializer.Deserialize<TResource>(valueElement.ToString(), options);

        if (clientResource is null)
        {
            throw new JsonException($"Unable to deserialise client resource type {nameof(TResource)}.");
        }

        string selfLinkJson = jsonDocument.RootElement.TryGetProperty(IanaLinkRelationConstants.Self, out JsonElement selfElement) ? selfElement.ToString() : string.Empty;
            
        Link selfLink = JsonSerializer.Deserialize<Link>(selfLinkJson, options) ?? Link.EmptyLink();

        ResourceDocument<TResource> resourceDocument = new(selfLink, clientResource);

        return resourceDocument;
    }

    public override void Write(Utf8JsonWriter writer, ResourceDocument<TResource> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(DefaultPropertyNames.Value);
        JsonSerializer.Serialize(writer, value.Resource, options);

        if (value.Links.TryGetValue(IanaLinkRelationConstants.Self, out Link? selfLink))
        {
            writer.WritePropertyName(IanaLinkRelationConstants.Self);
            JsonSerializer.Serialize(writer, (object)selfLink, options);
        }

        writer.WriteEndObject();
    }
}