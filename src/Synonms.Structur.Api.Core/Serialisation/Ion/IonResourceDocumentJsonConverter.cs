using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Serialisation;

namespace Synonms.Structur.Api.Core.Serialisation.Ion;

public class IonResourceDocumentJsonConverter<TResource> : JsonConverter<ResourceDocument<TResource>>
{
    private readonly Version _version;

    public IonResourceDocumentJsonConverter(Version version) => _version = version;

    public override ResourceDocument<TResource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        JsonElement valueElement = jsonDocument.RootElement.GetProperty(IonPropertyNames.Value);

        TResource? clientResource = (TResource?)ResourceJsonDataReader.Deserialize(
            valueElement.GetRawText(),
            typeof(TResource),
            options,
            out ResourceMetadata metadata);

        if (clientResource is null)
        {
            throw new JsonException($"Unable to deserialise client resource type {nameof(TResource)}.");
        }

        JsonElement selfElement = jsonDocument.RootElement.GetProperty(IanaLinkRelationConstants.Self);

        Link? selfLink = JsonSerializer.Deserialize<Link>(selfElement.ToString(), options);

        if (selfLink is null)
        {
            throw new JsonException($"Unable to extract [{IanaLinkRelationConstants.Self}] link from document.");
        }

        metadata.SelfLink = valueElement.TryGetProperty(IanaLinkRelationConstants.Self, out JsonElement itemSelfElement)
            ? JsonSerializer.Deserialize<Link>(itemSelfElement.GetRawText(), options) ?? selfLink
            : selfLink;
        valueElement.ForEachIonLinkProperty((linkName, link) => metadata.Links.Add(linkName, link), options);
        ResourceDocument<TResource> resourceDocument = new(new ResourceEnvelope<TResource>(clientResource, metadata));

        jsonDocument.RootElement.ForEachIonLinkProperty((linkName, link) => resourceDocument.WithLink(linkName, link), options);

        return resourceDocument;
    }

    public override void Write(Utf8JsonWriter writer, ResourceDocument<TResource> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(IonPropertyNames.Value);
        ResourceJsonWriter.Write(writer, new ResourceEnvelope<TResource>(value.Resource, value.Metadata), options, _version, true);

        foreach ((string key, Link link) in value.Links.Where(pair => pair.Key != IanaLinkRelationConstants.Self))
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, (object)link, options);
        }

        writer.WriteEndObject();
    }
}
