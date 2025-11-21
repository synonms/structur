using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Schema;

namespace Synonms.Structur.WebApi.Serialisation.Default;

public class DefaultPaginationJsonConverter : JsonConverter<Pagination>
{
    public override Pagination? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        int offset = jsonDocument.RootElement.GetProperty(DefaultPropertyNames.Pagination.Offset).GetInt32();
        int limit = jsonDocument.RootElement.GetProperty(DefaultPropertyNames.Pagination.Limit).GetInt32();
        int size = jsonDocument.RootElement.GetProperty(DefaultPropertyNames.Pagination.Size).GetInt32();

        return new Pagination(offset, limit, size, Link.EmptyLink(), Link.EmptyLink());
    }

    public override void Write(Utf8JsonWriter writer, Pagination value, JsonSerializerOptions options)
    {
        writer.WriteNumber(PropertyNames.Pagination.Offset, value.Offset);
        writer.WriteNumber(PropertyNames.Pagination.Limit, value.Limit);
        writer.WriteNumber(PropertyNames.Pagination.Size, value.Size);
    }
}