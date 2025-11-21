using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synonms.Structur.WebApi.Serialisation;

public class OptionalTimeOnlyJsonConverter : JsonConverter<TimeOnly?>
{
    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? token = reader.GetString();

        if (TimeOnly.TryParse(token, out TimeOnly value))
        {
            return value;
        }
        
        return null;
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString("O"));
        }
    }
}