using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Schema.Errors;

namespace Synonms.Structur.WebApi.Serialisation.Ion;

public class IonErrorJsonConverter : JsonConverter<Error>
{
    public override Error? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        Guid id = jsonDocument.RootElement.GetProperty("id").GetGuid();
        string code = jsonDocument.RootElement.GetProperty("code").GetString() ?? string.Empty;
        string title = jsonDocument.RootElement.GetProperty("title").GetString() ?? string.Empty;
        string detail = jsonDocument.RootElement.GetProperty("detail").GetString() ?? string.Empty;

        string? pointer = null;
        string? parameter = null;

        if (jsonDocument.RootElement.TryGetProperty("source", out JsonElement sourceElement))
        {
            pointer = sourceElement.GetProperty("pointer").GetString();
            parameter = sourceElement.GetProperty("parameter").GetString();
        }

        Error error = new(id, code, title, detail, new ErrorSource(pointer, parameter));

        return error;
    }

    public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
            
        writer.WriteString("id", value.Id);
        writer.WriteString("code", value.Code);
        writer.WriteString("title", value.Title);
        writer.WriteString("detail", value.Detail);

        writer.WritePropertyName("source");
            
        writer.WriteStartObject();
            
        writer.WriteString("pointer", value.Source.Pointer);
        writer.WriteString("parameter", value.Source.Parameter);
            
        writer.WriteEndObject();
            
        writer.WriteEndObject();
    }
}