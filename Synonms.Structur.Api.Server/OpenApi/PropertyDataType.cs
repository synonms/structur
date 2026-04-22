using Microsoft.OpenApi;

namespace Synonms.Structur.Api.Server.OpenApi;

public class PropertyDataType
{
    public PropertyDataType(JsonSchemaType type, string format = "")
    {
        Type = type;
        Format = format;
    }

    public JsonSchemaType Type { get; }

    public string Format { get; }
}