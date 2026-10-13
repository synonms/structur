using System.Reflection;
using System.Text.Json;
using System.Collections;
using System.Collections.Generic;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.System;
using Synonms.Structur.Core.Versioning;

namespace Synonms.Structur.Api.Core.Serialisation;

/// <summary>Writes a plain data contract with framework metadata supplied by its envelope.</summary>
public static class ResourceJsonWriter
{
    public static void Write<TResource>(
        Utf8JsonWriter writer,
        ResourceEnvelope<TResource> envelope,
        JsonSerializerOptions options,
        Version version,
        bool ion)
    {
        WriteObject(writer, envelope.Data!, typeof(TResource), envelope.Metadata, options, version, ion);
    }

    private static void WriteObject(
        Utf8JsonWriter writer,
        object data,
        Type dataType,
        ResourceMetadata metadata,
        JsonSerializerOptions options,
        Version version,
        bool ion)
    {
        writer.WriteStartObject();
        if (metadata.IncludesId)
        {
            writer.WriteString("id", metadata.Id);
        }

        VersionHistory objectVersionHistory = dataType.GetVersionHistory();
        foreach (PropertyInfo property in dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (IsFrameworkMetadataName(property.Name)
                || property.GetIndexParameters().Length != 0
                || property.PropertyType.IsForRelatedEntityCollectionLink())
            {
                continue;
            }

            VersionHistory propertyVersionHistory = property.GetVersionHistory();
            if (!VersionHistory.Merge(propertyVersionHistory, objectVersionHistory).IsApplicableAtVersion(version))
            {
                continue;
            }

            writer.WritePropertyName(property.Name.ToCamelCase());
            WritePropertyValue(writer, property.GetValue(data), property.PropertyType, metadata, options, version, ion);
        }

        if (ion)
        {
            if (metadata.IncludesSelfLink)
            {
                writer.WritePropertyName(IanaLinkRelationConstants.Self);
                JsonSerializer.Serialize(writer, (object)metadata.SelfLink, options);
            }
            foreach ((string name, Link link) in metadata.Links)
            {
                writer.WritePropertyName(name.ToCamelCase());
                JsonSerializer.Serialize(writer, link, options);
            }
        }

        writer.WriteEndObject();
    }

    private static void WritePropertyValue(
        Utf8JsonWriter writer,
        object? value,
        Type valueType,
        ResourceMetadata parentMetadata,
        JsonSerializerOptions options,
        Version version,
        bool ion)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value is IDictionary)
        {
            JsonSerializer.Serialize(writer, value, valueType, options);
            return;
        }

        if (parentMetadata.TryGetEmbeddedMetadata(value, out ResourceMetadata? embeddedMetadata))
        {
            WriteObject(writer, value, valueType, embeddedMetadata, options, version, ion);
            return;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            Type? elementType = valueType.GetArrayOrEnumerableElementType();
            writer.WriteStartArray();
            foreach (object? item in enumerable)
            {
                if (item is not null && parentMetadata.TryGetEmbeddedMetadata(item, out embeddedMetadata))
                {
                    WriteObject(writer, item, elementType ?? item.GetType(), embeddedMetadata, options, version, ion);
                }
                else
                {
                    if (item is null)
                    {
                        writer.WriteNullValue();
                    }
                    else if (IsPlainContract(elementType ?? item.GetType()))
                    {
                        WritePlainObject(writer, item, elementType ?? item.GetType(), options, version, ion);
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, item, elementType ?? item.GetType(), options);
                    }
                }
            }
            writer.WriteEndArray();
            return;
        }

        if (IsPlainContract(valueType))
        {
            WritePlainObject(writer, value, valueType, options, version, ion);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, valueType, options);
        }
    }

    private static bool IsPlainContract(Type type) =>
        type.IsClass
        && type != typeof(string)
        && type != typeof(Link)
        && type != typeof(Uri)
        && type != typeof(Version)
        && type != typeof(LookupResource)
        && !type.IsComplexValueObjectResource()
        && !typeof(Delegate).IsAssignableFrom(type)
        && !type.IsGenericTypeDefinition;

    private static void WritePlainObject(
        Utf8JsonWriter writer,
        object value,
        Type valueType,
        JsonSerializerOptions options,
        Version version,
        bool ion)
    {
        writer.WriteStartObject();
        VersionHistory objectVersionHistory = valueType.GetVersionHistory();

        foreach (PropertyInfo property in valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (IsFrameworkMetadataName(property.Name)
                || property.GetMethod?.IsPublic != true
                || property.GetIndexParameters().Length != 0
                || property.PropertyType.IsForRelatedEntityCollectionLink())
            {
                continue;
            }

            VersionHistory history = VersionHistory.Merge(property.GetVersionHistory(), objectVersionHistory);
            if (!history.IsApplicableAtVersion(version))
            {
                continue;
            }

            writer.WritePropertyName(property.Name.ToCamelCase());
            WritePropertyValue(writer, property.GetValue(value), property.PropertyType, new ResourceMetadata(Guid.Empty, Link.EmptyLink()), options, version, ion);
        }

        writer.WriteEndObject();
    }

    internal static bool IsFrameworkMetadataName(string name) =>
        name.Equals("Id", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Links", StringComparison.OrdinalIgnoreCase)
        || name.Equals("SelfLink", StringComparison.OrdinalIgnoreCase)
        || name.Equals("SupportedVersions", StringComparison.OrdinalIgnoreCase);
}
