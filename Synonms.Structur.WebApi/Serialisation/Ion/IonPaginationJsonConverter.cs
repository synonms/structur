using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Schema;

namespace Synonms.Structur.WebApi.Serialisation.Ion;

public class IonPaginationJsonConverter : JsonConverter<Pagination>
{
    public override Pagination? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        int offset = jsonDocument.RootElement.GetProperty(IonPropertyNames.Pagination.Offset).GetInt32();
        int limit = jsonDocument.RootElement.GetProperty(IonPropertyNames.Pagination.Limit).GetInt32();
        int size = jsonDocument.RootElement.GetProperty(IonPropertyNames.Pagination.Size).GetInt32();

        Link? firstPageLink = GetPageLink(IanaLinkRelationConstants.Pagination.First, jsonDocument.RootElement);

        if (firstPageLink is null)
        {
            throw new JsonException($"Failed to deserialise [{IanaLinkRelationConstants.Pagination.First}] link from document.");
        }

        Link? lastPageLink = GetPageLink(IanaLinkRelationConstants.Pagination.Last, jsonDocument.RootElement);

        if (lastPageLink is null)
        {
            throw new JsonException($"Failed to deserialise [{IanaLinkRelationConstants.Pagination.Last}] link from document.");
        }

        Link? previousPageLink = GetPageLink(IanaLinkRelationConstants.Pagination.Previous, jsonDocument.RootElement);
        Link? nextPageLink = GetPageLink(IanaLinkRelationConstants.Pagination.Next, jsonDocument.RootElement);

        return new Pagination(offset, limit, size, firstPageLink, lastPageLink)
        {
            Previous = previousPageLink,
            Next = nextPageLink
        };
    }

    public override void Write(Utf8JsonWriter writer, Pagination value, JsonSerializerOptions options)
    {
        writer.WriteNumber(PropertyNames.Pagination.Offset, value.Offset);
        writer.WriteNumber(PropertyNames.Pagination.Limit, value.Limit);
        writer.WriteNumber(PropertyNames.Pagination.Size, value.Size);

        writer.WritePropertyName(IanaLinkRelationConstants.Pagination.First);
        JsonSerializer.Serialize(writer, (object)value.First, options);

        writer.WritePropertyName(IanaLinkRelationConstants.Pagination.Last);
        JsonSerializer.Serialize(writer, (object)value.Last, options);

        if (value.Previous is not null)
        {
            writer.WritePropertyName(IanaLinkRelationConstants.Pagination.Previous);
            JsonSerializer.Serialize(writer, (object)value.Previous, options);
        }

        if (value.Next is not null)
        {
            writer.WritePropertyName(IanaLinkRelationConstants.Pagination.Next);
            JsonSerializer.Serialize(writer, (object)value.Next, options);
        }
    }
    
    private Link? GetPageLink(string relation, JsonElement jsonElement)
    {
        string? href = jsonElement.TryGetProperty(relation, out JsonElement linkElement) ? linkElement.GetProperty(IonPropertyNames.Links.Uri).GetString() : null;

        return string.IsNullOrWhiteSpace(href) ? null : Link.PageLink(new Uri(href, UriKind.RelativeOrAbsolute));
    }
}