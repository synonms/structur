using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
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
        { typeof(bool), new PropertyDataType(JsonSchemaType.Boolean) },
        { typeof(byte), new PropertyDataType(JsonSchemaType.Integer) },
        { typeof(sbyte), new PropertyDataType(JsonSchemaType.Integer) },
        { typeof(short), new PropertyDataType(JsonSchemaType.Integer) },
        { typeof(ushort), new PropertyDataType(JsonSchemaType.Integer) },
        { typeof(int), new PropertyDataType(JsonSchemaType.Integer, OpenApiIntegerFormats.Int32) },
        { typeof(uint), new PropertyDataType(JsonSchemaType.Integer) },
        { typeof(long), new PropertyDataType(JsonSchemaType.Integer, OpenApiIntegerFormats.Int64) },
        { typeof(ulong), new PropertyDataType(JsonSchemaType.Integer) },
        { typeof(float), new PropertyDataType(JsonSchemaType.Number, OpenApiNumberFormats.Float) },
        { typeof(double), new PropertyDataType(JsonSchemaType.Number, OpenApiNumberFormats.Double) },
        { typeof(decimal), new PropertyDataType(JsonSchemaType.Number) },
        { typeof(byte[]), new PropertyDataType(JsonSchemaType.String, OpenApiStringFormats.Byte) },
        { typeof(string), new PropertyDataType(JsonSchemaType.String) },
        { typeof(char), new PropertyDataType(JsonSchemaType.String) },
        { typeof(DateTime), new PropertyDataType(JsonSchemaType.String, OpenApiStringFormats.DateTime) },
        { typeof(DateTimeOffset), new PropertyDataType(JsonSchemaType.String) },
        { typeof(DateOnly), new PropertyDataType(JsonSchemaType.String, OpenApiStringFormats.Date) },
        { typeof(TimeOnly), new PropertyDataType(JsonSchemaType.String, OpenApiStringFormats.Time) },
        { typeof(Guid), new PropertyDataType(JsonSchemaType.String, OpenApiStringFormats.Uuid) },
        { typeof(Uri), new PropertyDataType(JsonSchemaType.String, OpenApiStringFormats.Uri) }
    };

    public static OpenApiSchema GetOrCreateSchemaReferenceForResource(ILogger logger, OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute, string componentSchemaName, Dictionary<string, IOpenApiSchema>? additionalProperties = null) =>
        openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () =>
        {
            Dictionary<string, IOpenApiSchema> properties = additionalProperties ?? new Dictionary<string, IOpenApiSchema>();

            properties.Add("id", new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" });
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
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = true,
                Properties = properties,
                Required = requiredProperties.ToHashSet()
            };

            return componentSchema;
        });
    
    private static OpenApiSchema GetOrCreateSchemaReferenceForResource(ILogger logger, OpenApiDocument openApiDocument, string componentSchemaName, Type objectType) =>
        openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () =>
        {
            Dictionary<string, IOpenApiSchema> properties = new();
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
                Type = JsonSchemaType.Object,
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
            Type = JsonSchemaType.Array, 
            Items = GenerateSchemaForProperty(logger, openApiDocument, propertyName, elementType)
        };
    }
    
    private static OpenApiSchema CreateSchemaForEnum(Type enumPropertyType)
    {
        bool isNullable = enumPropertyType.IsNullable();
        Type nonNullableType = isNullable ? (enumPropertyType.GetNullableType() ?? enumPropertyType) : enumPropertyType;
        
        List<JsonNode> enumNames = Enum.GetNames(nonNullableType).Select(JsonNode (x) => JsonValue.Create(x)).ToList();
        
        return new OpenApiSchema
        {
            Type = JsonSchemaType.String, 
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
                Format = propertyDataType.Format
            };
        }
        
        logger.LogWarning("Unable to map scalar property '{PropertyName}' of type '{PropertyType}'.", propertyName, nonNullableType.Name);

        return new OpenApiSchema();
    }
}