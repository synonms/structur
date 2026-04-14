using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.System;
using Synonms.Structur.Core.Versioning;

namespace Synonms.Structur.Api.Core.Serialisation.Ion;

public class IonChildResourceJsonConverter<TChildResource> : JsonConverter<TChildResource>
    where TChildResource : ChildResource, new()
{
    private readonly Version _version;

    public IonChildResourceJsonConverter(Version version)
    {
        _version = version;
    }
    
    public override TChildResource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        TChildResource childResource = new();

        foreach (JsonProperty jsonProperty in jsonDocument.RootElement.EnumerateObject())
        {
            PropertyInfo? propertyInfo = typeof(TChildResource).GetProperty(jsonProperty.Name.ToPascalCase(), BindingFlags.Instance | BindingFlags.Public);

            if (propertyInfo is null || propertyInfo.CanWrite is false)
            {
                continue;
            }

            object? value = jsonProperty.Value.Deserialize(propertyInfo.PropertyType, options);

            if (value is not null && value.GetType().IsAssignableTo(propertyInfo.PropertyType))
            {
                propertyInfo.SetValue(childResource, value);
            }
        }
            
        return childResource;
    }

    public override void Write(Utf8JsonWriter writer, TChildResource value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        VersionHistory objectVersionHistory = typeof(TChildResource).GetVersionHistory();
        
        writer.WriteString("id", value.Id);

        foreach (PropertyInfo propertyInfo in typeof(TChildResource).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (propertyInfo.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
         
            VersionHistory propertyVersionHistory = propertyInfo.GetVersionHistory();
            VersionHistory applicableVersionHistory = VersionHistory.Merge(propertyVersionHistory, objectVersionHistory);

            if (applicableVersionHistory.IsApplicableAtVersion(_version))
            {
                writer.WritePropertyName(propertyInfo.Name.ToCamelCase());
                JsonSerializer.Serialize(writer, propertyInfo.GetValue(value), options);
            }
        }
            
        writer.WriteEndObject();
    }
}