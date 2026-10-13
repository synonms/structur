using System.Collections;
using System.Reflection;
using System.Text.Json;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.System;

namespace Synonms.Structur.Api.Core.Serialisation;

public static class ResourceJsonDataReader
{
    public static object? Deserialize(string json, Type contractType, JsonSerializerOptions options, out ResourceMetadata metadata)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        Guid id = ReadId(document.RootElement) ?? Guid.NewGuid();

        using MemoryStream sanitizedJson = new();
        using (Utf8JsonWriter writer = new(sanitizedJson))
        {
            WriteWithoutTransportMetadata(writer, document.RootElement);
        }

        sanitizedJson.Position = 0;
        object? data = JsonSerializer.Deserialize(sanitizedJson, contractType, options);
        metadata = new ResourceMetadata(id, Link.EmptyLink());
        if (data is not null)
        {
            PopulateEmbeddedMetadata(metadata, document.RootElement, data, options);
        }

        return data;
    }

    private static Guid? ReadId(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        JsonProperty[] idProperties = element.EnumerateObject()
            .Where(property => property.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (idProperties.Length > 1)
        {
            throw new JsonException("A resource envelope may contain only one id.");
        }

        if (idProperties.Length == 0)
        {
            return null;
        }

        if (!idProperties[0].Value.TryGetGuid(out Guid id))
        {
            throw new JsonException("The resource envelope id must be a valid GUID.");
        }

        return id;
    }

    private static void WriteWithoutTransportMetadata(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in value.EnumerateObject())
                {
                    if (IsTransportMetadata(property.Name) || IsLinkObject(property.Value))
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteWithoutTransportMetadata(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in value.EnumerateArray())
                {
                    WriteWithoutTransportMetadata(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }

    private static bool IsTransportMetadata(string name) =>
        name.Equals("id", StringComparison.OrdinalIgnoreCase)
        || name.Equals("links", StringComparison.OrdinalIgnoreCase)
        || name.Equals("self", StringComparison.OrdinalIgnoreCase)
        || name.Equals("selfLink", StringComparison.OrdinalIgnoreCase)
        || name.Equals("supportedVersions", StringComparison.OrdinalIgnoreCase);

    private static bool IsLinkObject(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object
        && (value.TryGetProperty("uri", out _) || value.TryGetProperty("href", out _))
        && value.TryGetProperty("relation", out _);

    private static void PopulateEmbeddedMetadata(ResourceMetadata parent, JsonElement json, object data, JsonSerializerOptions options)
    {
        if (json.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty jsonProperty in json.EnumerateObject())
        {
            if (IsTransportMetadata(jsonProperty.Name))
            {
                continue;
            }

            PropertyInfo? property = data.GetType().GetProperty(
                jsonProperty.Name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            object? child = property?.GetValue(data);
            if (child is null || property is null)
            {
                continue;
            }

            if (child is IEnumerable children && child is not string && jsonProperty.Value.ValueKind == JsonValueKind.Array)
            {
                IEnumerator childEnumerator = children.GetEnumerator();
                int index = 0;
                foreach (JsonElement childJson in jsonProperty.Value.EnumerateArray())
                {
                    if (!childEnumerator.MoveNext())
                    {
                        break;
                    }

                    object? childItem = childEnumerator.Current;
                    Type? elementType = property.PropertyType.GetArrayOrEnumerableElementType();
                    if (childItem is not null && elementType?.IsResource() == true)
                    {
                        ResourceMetadata childMetadata = CreateNestedMetadata(childJson, childItem, options);
                        parent.AddEmbeddedMetadata(childItem, childMetadata, $"{jsonProperty.Name}[{index}]");
                    }

                    index++;
                }

                continue;
            }

            if (property.PropertyType.IsResource())
            {
                ResourceMetadata childMetadata = CreateNestedMetadata(jsonProperty.Value, child, options);
                parent.AddEmbeddedMetadata(child, childMetadata, jsonProperty.Name);
            }
        }
    }

    private static ResourceMetadata CreateNestedMetadata(JsonElement json, object data, JsonSerializerOptions options)
    {
        Guid id = ReadId(json) ?? Guid.NewGuid();
        ResourceMetadata metadata = new(id, Link.EmptyLink(), includesSelfLink: false);
        PopulateEmbeddedMetadata(metadata, json, data, options);
        return metadata;
    }
}
