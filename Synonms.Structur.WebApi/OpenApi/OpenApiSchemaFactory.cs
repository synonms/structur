using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System;
using Synonms.Structur.Core.System.Reflection;

namespace Synonms.Structur.WebApi.OpenApi;

public static class OpenApiSchemaFactory
{
    private static readonly Dictionary<Type, PropertyDataType> PrimitiveTypesAndFormats = new()
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

    public static OpenApiSchema GetOrCreateSchemaReferenceForResource(ILogger logger, OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute, string componentSchemaName, Dictionary<string, OpenApiSchema>? additionalProperties = null) =>
        openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () =>
        {
            Dictionary<string, OpenApiSchema> properties = additionalProperties ?? new Dictionary<string, OpenApiSchema>();

            properties.Add("id", new OpenApiSchema { Type = "string", Format = "uuid" });
                /*            { "createdAt", new OpenApiSchema { Type = "string", Format = "date-time" } },
                            { "updatedAt", new OpenApiSchema { Type = "string", Format = "date-time" } }*/
                
            List<string> requiredProperties = [];

            string[] propertiesToExclude = [nameof(Resource.Id), /*nameof(Resource.CreatedAt), nameof(Resource.UpdatedAt),*/ nameof(Resource.SelfLink), nameof(Resource.Links)];

            foreach (PropertyInfo resourcePropertyInfo in resourceAttribute.ResourceType.GetPublicInstanceProperties(propertiesToExclude))
            {
                OpenApiSchema schema = GenerateSchemaForProperty(logger, openApiDocument, resourcePropertyInfo.PropertyType);

                properties.Add(resourcePropertyInfo.Name.ToCamelCase(), schema);

                if (resourcePropertyInfo.IsNullable() is false)
                {
                    requiredProperties.Add(resourcePropertyInfo.Name.ToCamelCase());
                }
            }

            OpenApiSchema componentSchema = new()
            {
                Type = "object",
                AdditionalPropertiesAllowed = true,
                Properties = properties,
                Required = requiredProperties.ToHashSet()
            };

            return componentSchema;
        });
    
    private static OpenApiSchema GetOrCreateSchemaReferenceForResource(ILogger logger, OpenApiDocument openApiDocument, string componentSchemaName, Type objectType) =>
        openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () =>
        {
            Dictionary<string, OpenApiSchema> properties = new();
            List<string> requiredProperties = [];

            string[] propertiesToExclude =
                [nameof(Resource.Id), /*nameof(Resource.IsDeleted), nameof(Resource.CreatedAt), nameof(Resource.UpdatedAt),*/ nameof(Resource.SelfLink), nameof(Resource.Links)];

            foreach (PropertyInfo propertyInfo in objectType.GetPublicInstanceProperties(propertiesToExclude))
            {
                OpenApiSchema schema = GenerateSchemaForProperty(logger, openApiDocument, propertyInfo.PropertyType);

                properties.Add(propertyInfo.Name.ToCamelCase(), schema);

                if (propertyInfo.IsNullable() is false)
                {
                    requiredProperties.Add(propertyInfo.Name.ToCamelCase());
                }
            }

            return new OpenApiSchema
            {
                Type = OpenApiDataTypes.Object,
                Required = requiredProperties.ToHashSet(),
                Properties = properties
            };
        });
    
    private static OpenApiSchema GenerateSchemaForProperty(ILogger logger, OpenApiDocument openApiDocument, Type propertyType) =>
        propertyType.GetResourcePropertyType() switch
        {
            ResourcePropertyType.EmbeddedResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.EmbeddedChildResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.EmbeddedResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyType),
            ResourcePropertyType.EmbeddedChildResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyType),
            ResourcePropertyType.EmbeddedLookupResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.RelatedResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.RelatedResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyType),
            ResourcePropertyType.ValueObjectResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.ValueObjectResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyType),
            ResourcePropertyType.VanillaCollection => CreateSchemaForArray(logger, openApiDocument, propertyType),
            ResourcePropertyType.VanillaScalar => CreateSchemaForScalar(logger, propertyType),
            _ => new OpenApiSchema()
        };
    
    private static OpenApiSchema CreateSchemaForArray(ILogger logger, OpenApiDocument openApiDocument, Type arrayType)
    {
        Type? elementType = arrayType.GetArrayOrEnumerableElementType();

        if (elementType is null)
        {
            logger.LogWarning("Unable to determine element type for array type '{ArrayType}'.", arrayType.Name);
            
            return new OpenApiSchema();
        }

        return new OpenApiSchema
        {
            Type = OpenApiDataTypes.Array, 
            Items = GenerateSchemaForProperty(logger, openApiDocument, elementType)
        };
    }
    
    private static OpenApiSchema CreateSchemaForScalar(ILogger logger, Type scalarPropertyType)
    {
        if (PrimitiveTypesAndFormats.TryGetValue(scalarPropertyType, out PropertyDataType? propertyDataType))
        {
            return new OpenApiSchema
            {
                Type = propertyDataType.Type, 
                Format = propertyDataType.Format
            };
        }
        
        logger.LogWarning("Unable to map scalar property of type '{PropertyType}'.", scalarPropertyType.Name);

        return new OpenApiSchema();
    }
}