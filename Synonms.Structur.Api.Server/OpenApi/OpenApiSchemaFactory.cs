using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System;
using Synonms.Structur.Core.System.Reflection;

namespace Synonms.Structur.Api.Server.OpenApi;

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
                OpenApiSchema schema = GenerateSchemaForProperty(logger, openApiDocument, resourcePropertyInfo.Name, resourcePropertyInfo.PropertyType);

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
                OpenApiSchema schema = GenerateSchemaForProperty(logger, openApiDocument, propertyInfo.Name, propertyInfo.PropertyType);

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
    
    private static OpenApiSchema GenerateSchemaForProperty(ILogger logger, OpenApiDocument openApiDocument, string propertyName, Type propertyType) =>
        propertyType.GetResourcePropertyType() switch
        {
            ResourcePropertyType.EmbeddedResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.EmbeddedChildResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.EmbeddedResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyName, propertyType),
            ResourcePropertyType.EmbeddedChildResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyName, propertyType),
            ResourcePropertyType.EmbeddedLookupResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.RelatedResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.RelatedResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyName, propertyType),
            ResourcePropertyType.ComplexValueObjectResource => GetOrCreateSchemaReferenceForResource(logger, openApiDocument, propertyType.Name, propertyType),
            ResourcePropertyType.ComplexValueObjectResourceCollection => CreateSchemaForArray(logger, openApiDocument, propertyName, propertyType),
            ResourcePropertyType.Enumeration => CreateSchemaForEnum(propertyType),
            ResourcePropertyType.VanillaCollection => CreateSchemaForArray(logger, openApiDocument, propertyName, propertyType),
            ResourcePropertyType.VanillaScalar => CreateSchemaForScalar(logger, propertyName, propertyType),
            _ => new OpenApiSchema()
        };
    
    private static OpenApiSchema CreateSchemaForArray(ILogger logger, OpenApiDocument openApiDocument, string propertyName, Type arrayType)
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
            Items = GenerateSchemaForProperty(logger, openApiDocument, propertyName, elementType)
        };
    }
    
    private static OpenApiSchema CreateSchemaForEnum(Type enumPropertyType)
    {
        bool isNullable = enumPropertyType.IsNullable();
        Type nonNullableType = isNullable ? (enumPropertyType.GetNullableType() ?? enumPropertyType) : enumPropertyType;
        
        List<IOpenApiAny> enumNames = Enum.GetNames(nonNullableType).Select(x => (IOpenApiAny)new OpenApiString(x)).ToList();
        
        return new OpenApiSchema
        {
            Type = OpenApiDataTypes.String, 
            Enum = enumNames
        };
    }
    
    private static OpenApiSchema CreateSchemaForScalar(ILogger logger, string propertyName, Type scalarPropertyType)
    {
        bool isNullable = scalarPropertyType.IsNullable();
        Type nonNullableType = isNullable ? (scalarPropertyType.GetNullableType() ?? scalarPropertyType) : scalarPropertyType;
        
        if (PrimitiveTypesAndFormats.TryGetValue(nonNullableType, out PropertyDataType? propertyDataType))
        {
            return new OpenApiSchema
            {
                Type = propertyDataType.Type, 
                Format = propertyDataType.Format,
                Nullable = isNullable
            };
        }
        
        logger.LogWarning("Unable to map scalar property '{PropertyName}' of type '{PropertyType}'.", propertyName, nonNullableType.Name);

        return new OpenApiSchema();
    }
}