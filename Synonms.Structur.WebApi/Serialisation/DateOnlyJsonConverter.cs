using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synonms.Structur.WebApi.Serialisation;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? token = reader.GetString();

        if (DateOnly.TryParse(token, out DateOnly value))
        {
            return value;
        }
        
        throw new JsonException($"Unable to parse DateOnly value [{token ?? string.Empty}]");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O"));
    }
}