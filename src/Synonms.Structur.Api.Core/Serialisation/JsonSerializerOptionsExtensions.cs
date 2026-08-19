using System.Text.Json;

namespace Synonms.Structur.Api.Core.Serialisation;

public static class JsonSerializerOptionsExtensions
{
    public static JsonSerializerOptions ConfigureForStructurFramework(this JsonSerializerOptions jsonSerialiserOptions)
    {
        jsonSerialiserOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonSerialiserOptions.Converters.Add(new DateOnlyJsonConverter());
        jsonSerialiserOptions.Converters.Add(new OptionalDateOnlyJsonConverter());
        jsonSerialiserOptions.Converters.Add(new TimeOnlyJsonConverter());
        jsonSerialiserOptions.Converters.Add(new OptionalTimeOnlyJsonConverter());

        return jsonSerialiserOptions;
    }
}