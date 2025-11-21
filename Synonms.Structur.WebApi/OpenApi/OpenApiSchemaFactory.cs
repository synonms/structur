using System.Reflection;
using Microsoft.OpenApi.Models;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.WebApi.OpenApi;

public static class OpenApiSchemaFactory
{
    private static readonly Dictionary<Type, PropertyDataType> PrimitiveTypesAndFormats = new ()
    {
        { typeof(bool), new PropertyDataType(OpenApiDataTypes.Boolean) },
        { typeof(byte), new PropertyDataType(OpenApiDataTypes.Integer) },
        { typeof(sbyte), new PropertyDataType(OpenApiDataTypes.Integer) },
        { typeof(short), new PropertyDataType(OpenApiDataTypes.Integer) },
        { typeof(ushort), new PropertyDataType(OpenApiDataTypes.Integer) },
        { typeof(int), new PropertyDataType(OpenApiDataTypes.Integer, OpenApiIntegerFormats.Int32) },
        { typeof(uint), new PropertyDataType(OpenApiDataTypes.Integer) },
        { typeof(long), new PropertyDataType(OpenApiDataTypes.Integer, OpenApiIntegerFormats.Int64) },
        { typeof(ulong), new PropertyDataType(OpenApiDataTypes.Integer) },
        { typeof(float), new PropertyDataType(OpenApiDataTypes.Number, OpenApiNumberFormats.Float) },
        { typeof(double), new PropertyDataType(OpenApiDataTypes.Number, OpenApiNumberFormats.Double) },
        { typeof(decimal), new PropertyDataType(OpenApiDataTypes.Number) },
        { typeof(byte[]), new PropertyDataType(OpenApiDataTypes.String, OpenApiStringFormats.Byte) },
        { typeof(string), new PropertyDataType(OpenApiDataTypes.String) },
        { typeof(char), new PropertyDataType(OpenApiDataTypes.String) },
        { typeof(DateTime), new PropertyDataType(OpenApiDataTypes.String, OpenApiStringFormats.DateTime) },
        { typeof(DateTimeOffset), new PropertyDataType(OpenApiDataTypes.String) },
        { typeof(DateOnly), new PropertyDataType(OpenApiDataTypes.String, OpenApiStringFormats.Date) },
        { typeof(TimeOnly), new PropertyDataType(OpenApiDataTypes.String, OpenApiStringFormats.Time) },
        { typeof(Guid), new PropertyDataType(OpenApiDataTypes.String, OpenApiStringFormats.Uuid) },
        { typeof(Uri), new PropertyDataType(OpenApiDataTypes.String, OpenApiStringFormats.Uri) }
    };

    public static Dictionary<string, OpenApiSchema> GenerateResourceProperties(StructurResourceAttribute resourceAttribute)
    {
        Dictionary<string, OpenApiSchema> properties = new()
        {
            { "id", new OpenApiSchema() { Type = "string", Format = "uuid" } },
            { "createdAt", new OpenApiSchema() { Type = "string", Format = "date-time" } },
            { "updatedAt", new OpenApiSchema() { Type = "string", Format = "date-time" } }
        };

        foreach (PropertyInfo propertyInfo in resourceAttribute.ResourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (propertyInfo.Name.Equals("id", StringComparison.OrdinalIgnoreCase)
                || propertyInfo.Name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase)
                || propertyInfo.Name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase)
                || propertyInfo.Name.Equals("SelfLink", StringComparison.OrdinalIgnoreCase)
                || propertyInfo.Name.Equals("Links", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (propertyInfo.PropertyType.IsArrayOrEnumerable())
            {
                Type? resourcePropertyEnumerableElementType = propertyInfo.PropertyType.GetArrayOrEnumerableElementType();

                if (resourcePropertyEnumerableElementType is null)
                {
                    // TODO: Warn
                    continue;
                }

                if (resourcePropertyEnumerableElementType.IsEntityId())
                {
                    // TResource.IEnumerable<EntityId<TAggregateRoot>> = A related resource collection where we present a link.
                    continue;
                }
            }
            
            if (propertyInfo.PropertyType.IsForRelatedEntityCollectionLink())
            {
                continue;
            }

            if (PrimitiveTypesAndFormats.TryGetValue(propertyInfo.PropertyType, out PropertyDataType? propertyDataType))
            {
                properties.Add(propertyInfo.Name.ToCamelCase(), new OpenApiSchema() { Type = propertyDataType.Type, Format = propertyDataType.Format });
            }
            else
            {
                // TODO: EntityId<>
                // TODO: IEnumerable<EntityId<>>
                // TODO: Resource
                // TODO: IEnumerable<Resource>
                // TODO: ChildResource
                // TODO: IEnumerable<ChildResource>
            }
        }

        return properties;
    }
}