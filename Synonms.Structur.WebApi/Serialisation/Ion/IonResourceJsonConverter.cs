using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.System;

namespace Synonms.Structur.WebApi.Serialisation.Ion;

public class IonResourceJsonConverter<TResource> : JsonConverter<TResource>
    where TResource : Resource, new()
{
    public override TResource Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        Guid id = jsonDocument.RootElement.GetProperty("id").GetGuid();
        Link selfLink = Link.EmptyLink();

        if (jsonDocument.RootElement.TryGetProperty(IanaLinkRelationConstants.Self, out JsonElement selfElement))
        {
            Link? link = JsonSerializer.Deserialize<Link>(selfElement.ToString(), options);

            if (link is not null)
            {
                selfLink = link;
            }
        }

        TResource resource = new()
        {
            Id = id,
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
            
        jsonDocument.RootElement.ForEachIonLinkProperty((linkName, link) => resource.Links.Add(linkName, link), options);
        
        return resource;
    }

    public override void Write(Utf8JsonWriter writer, TResource value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

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

            writer.WritePropertyName(propertyInfo.Name.ToCamelCase());
            JsonSerializer.Serialize(writer, propertyInfo.GetValue(value), options);
        }

        writer.WritePropertyName(IanaLinkRelationConstants.Self);
        JsonSerializer.Serialize(writer, (object)value.SelfLink, options);

        foreach ((string linkName, Link link) in value.Links)
        {
            writer.WritePropertyName(linkName.ToCamelCase());
            JsonSerializer.Serialize(writer, link, options);
        }
            
        writer.WriteEndObject();
    }
}