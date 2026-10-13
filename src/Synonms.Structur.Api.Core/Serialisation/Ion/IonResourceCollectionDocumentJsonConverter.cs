using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Serialisation;

namespace Synonms.Structur.Api.Core.Serialisation.Ion;

public class IonResourceCollectionDocumentJsonConverter<TResource> : JsonConverter<ResourceCollectionDocument<TResource>>
{
    private readonly Version _version;

    public IonResourceCollectionDocumentJsonConverter(Version version) => _version = version;
    public override ResourceCollectionDocument<TResource> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        JsonElement valueArray = jsonDocument.RootElement.GetProperty(IonPropertyNames.Value);

        if (valueArray.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Expected '{IonPropertyNames.Value}' property to be an array.");
        }

        List<ResourceEnvelope<TResource>> clientResources = new();

        foreach (JsonElement valueElement in valueArray.EnumerateArray())
        {
            TResource? clientResource = (TResource?)ResourceJsonDataReader.Deserialize(
                valueElement.GetRawText(),
                typeof(TResource),
                options,
                out ResourceMetadata metadata);

            if (clientResource is null)
            {
                throw new JsonException($"Unable to deserialise client resource type {nameof(TResource)}.");
            }

            metadata.SelfLink = valueElement.TryGetProperty(IanaLinkRelationConstants.Self, out JsonElement itemSelfElement)
                ? JsonSerializer.Deserialize<Link>(itemSelfElement.GetRawText(), options) ?? Link.EmptyLink()
                : Link.EmptyLink();
            valueElement.ForEachIonLinkProperty((linkName, link) => metadata.Links.Add(linkName, link), options);
            clientResources.Add(new ResourceEnvelope<TResource>(clientResource, metadata));
        }

        JsonElement selfElement = jsonDocument.RootElement.GetProperty(IanaLinkRelationConstants.Self);

        Link? selfLink = JsonSerializer.Deserialize<Link>(selfElement.ToString(), options);

        if (selfLink is null)
        {
            throw new JsonException($"Unable to extract [{IanaLinkRelationConstants.Self}] link from document.");
        }

        Pagination? pagination = JsonSerializer.Deserialize<Pagination>(jsonDocument.RootElement.ToString(), options);

        if (pagination is null)
        {
            throw new JsonException($"Unable to extract pagination from document.");
        }

        ResourceCollectionDocument<TResource> resourceDocument = new(selfLink, clientResources, pagination);

        jsonDocument.RootElement.ForEachIonLinkProperty((linkName, link) => resourceDocument.WithLink(linkName, link), options);

        return resourceDocument;
    }

    public override void Write(Utf8JsonWriter writer, ResourceCollectionDocument<TResource> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(IonPropertyNames.Value);
        writer.WriteStartArray();
        foreach (ResourceEnvelope<TResource> envelope in value.Envelopes)
        {
            ResourceJsonWriter.Write(writer, envelope, options, _version, true);
        }
        writer.WriteEndArray();

        foreach ((string key, Link link) in value.Links)
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, (object)link, options);
        }

        JsonSerializer.Serialize(writer, value.Pagination, options);

        writer.WriteEndObject();
    }
}
