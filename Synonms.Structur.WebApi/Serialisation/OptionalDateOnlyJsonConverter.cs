using System.Text.Json;
using System.Text.Json.Serialization;

namespace Synonms.Structur.WebApi.Serialisation;

public class OptionalDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? token = reader.GetString();

        if (DateOnly.TryParse(token, out DateOnly value))
        {
            return value;
        }
        
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
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