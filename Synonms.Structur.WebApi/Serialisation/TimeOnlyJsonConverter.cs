using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synonms.Structur.WebApi.Serialisation;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? token = reader.GetString();

        if (TimeOnly.TryParse(token, out TimeOnly value))
        {
            return value;
        }
        
        throw new JsonException($"Unable to parse TimeOnly value [{token ?? string.Empty}]");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O"));
    }
}