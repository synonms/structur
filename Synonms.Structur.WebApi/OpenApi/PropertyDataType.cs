namespace Synonms.Structur.WebApi.OpenApi;

public class PropertyDataType
{
    public PropertyDataType(string type, string format = "")
    {
        Type = type;
        Format = format;
    }

    public string Type { get; }

    public string Format { get; }
}