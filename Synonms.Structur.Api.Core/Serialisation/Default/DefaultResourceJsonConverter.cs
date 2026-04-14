using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.System;
using Synonms.Structur.Core.Versioning;

namespace Synonms.Structur.Api.Core.Serialisation.Default;

public class DefaultResourceJsonConverter<TResource> : JsonConverter<TResource>
    where TResource : Resource, new()
{
    private readonly Version _version;

    public DefaultResourceJsonConverter(Version version)
    {
        _version = version;
    }
    
    public override TResource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        Guid? id = null;
        if (jsonDocument.RootElement.TryGetProperty("id", out JsonElement idElement))
        {
            id = idElement.TryGetGuid(out Guid guid) ? guid : null;
        }
        
        string selfLinkJson = jsonDocument.RootElement.TryGetProperty(IanaLinkRelationConstants.Self, out JsonElement selfElement) ? selfElement.ToString() : string.Empty;
        Link selfLink = string.IsNullOrWhiteSpace(selfLinkJson) 
            ? Link.EmptyLink() 
            : JsonSerializer.Deserialize<Link>(selfLinkJson, options) ?? Link.EmptyLink();

        TResource resource = id is not null && id != Guid.Empty 
            ? new TResource
            {
                Id = id.Value,
                SelfLink = selfLink
            }
            : new TResource
            {
                SelfLink = selfLink
            };

        foreach (JsonProperty jsonProperty in jsonDocument.RootElement.EnumerateObject())
        {
            PropertyInfo? propertyInfo = typeof(TResource).GetProperty(jsonProperty.Name.ToPascalCase(), BindingFlags.Instance | BindingFlags.Public);

            if (propertyInfo is null || propertyInfo.CanWrite is false)
            {
                continue;
            }

            if (propertyInfo.PropertyType.IsForRelatedEntityCollectionLink())
            {
                continue;
            }

            object? value = jsonProperty.Value.Deserialize(propertyInfo.PropertyType, options);

            if (value is not null && value.GetType().IsAssignableTo(propertyInfo.PropertyType))
            {
                propertyInfo.SetValue(resource, value);
            }
        }
        
        return resource;
    }

    public override void Write(Utf8JsonWriter writer, TResource value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        VersionHistory objectVersionHistory = typeof(TResource).GetVersionHistory();

        writer.WriteString("id", value.Id);

        foreach (PropertyInfo propertyInfo in typeof(TResource).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (propertyInfo.Name.Equals("id", StringComparison.OrdinalIgnoreCase)
                || propertyInfo.Name.Equals("SelfLink", StringComparison.OrdinalIgnoreCase)
                || propertyInfo.Name.Equals("Links", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (propertyInfo.PropertyType.IsForRelatedEntityCollectionLink())
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