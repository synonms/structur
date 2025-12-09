using System.Reflection;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.System;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Application.Schema.Resources;

public static class TypeExtensions
{
    private static readonly Dictionary<Type, string> TypeToDataTypeMappings = new ()
    {
        [typeof(string)] = DataTypeConstants.String,
        [typeof(Guid)] = DataTypeConstants.String,
        [typeof(bool)] = DataTypeConstants.Boolean,
        [typeof(bool?)] = DataTypeConstants.Boolean,
        [typeof(DateOnly)] = DataTypeConstants.DateOnly,
        [typeof(DateOnly?)] = DataTypeConstants.DateOnly,
        [typeof(TimeOnly)] = DataTypeConstants.TimeOnly, 
        [typeof(TimeOnly?)] = DataTypeConstants.TimeOnly,
        [typeof(DateTime)] = DataTypeConstants.DateTime,
        [typeof(DateTime?)] = DataTypeConstants.DateTime,
        [typeof(TimeSpan)] = DataTypeConstants.Duration, 
        [typeof(TimeSpan?)] = DataTypeConstants.Duration,
        [typeof(decimal)] = DataTypeConstants.Decimal,
        [typeof(decimal?)] = DataTypeConstants.Decimal, 
        [typeof(double)] = DataTypeConstants.Number,
        [typeof(double?)] = DataTypeConstants.Number,
        [typeof(float)] = DataTypeConstants.Number,
        [typeof(float?)] = DataTypeConstants.Number,
        [typeof(int)] = DataTypeConstants.Integer,
        [typeof(int?)] = DataTypeConstants.Integer,
        [typeof(uint)] = DataTypeConstants.Integer,
        [typeof(uint?)] = DataTypeConstants.Integer,
        [typeof(long)] = DataTypeConstants.Integer,
        [typeof(long?)] = DataTypeConstants.Integer,
        [typeof(ulong)] = DataTypeConstants.Integer,
        [typeof(ulong?)] = DataTypeConstants.Integer,
        [typeof(short)] = DataTypeConstants.Integer,
        [typeof(short?)] = DataTypeConstants.Integer,
        [typeof(ushort)] = DataTypeConstants.Integer,
        [typeof(ushort?)] = DataTypeConstants.Integer
    };
    
    public static string GetResourceDataType(this Type type)
    {
        if (TypeToDataTypeMappings.TryGetValue(type, out string? propertyType))
        {
            return propertyType;
        }

        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            return DataTypeConstants.Array;
        }

        if (type.IsEntityId())
        {
            return DataTypeConstants.String;
        }
        
        return DataTypeConstants.Object;
    }

    public static ResourcePropertyType GetResourcePropertyType(this Type propertyType)
    {
        if (propertyType.IsArrayOrEnumerable())
        {
            Type? resourcePropertyEnumerableElementType = propertyType.GetArrayOrEnumerableElementType();

            if (resourcePropertyEnumerableElementType is null)
            {
                return ResourcePropertyType.Unknown;
            }

            if (resourcePropertyEnumerableElementType.IsEntityId())
            {
                return ResourcePropertyType.RelatedResourceCollection;
            }

            if (resourcePropertyEnumerableElementType.IsResource())
            {
                return ResourcePropertyType.EmbeddedResourceCollection;
            }

            if (resourcePropertyEnumerableElementType.IsChildResource())
            {
                return ResourcePropertyType.EmbeddedChildResourceCollection;
            }

            if (resourcePropertyEnumerableElementType.IsValueObjectResource())
            {
                return ResourcePropertyType.ValueObjectResourceCollection;
            }

            return ResourcePropertyType.VanillaCollection;
        }
        
        if (propertyType.IsEntityId())
        {
            return ResourcePropertyType.RelatedResource;
        }

        if (propertyType.IsResource())
        {
            return ResourcePropertyType.EmbeddedResource;
        }

        if (propertyType.IsChildResource())
        {
            return ResourcePropertyType.EmbeddedChildResource;
        }

        if (propertyType.IsValueObjectResource())
        {
            return ResourcePropertyType.ValueObjectResource;
        }

        if (propertyType.IsLookupResource())
        {
            return ResourcePropertyType.EmbeddedLookupResource;
        }

        return ResourcePropertyType.VanillaScalar;
    }
    
    public static IEnumerable<PropertyInfo> GetPublicInstanceProperties(this Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

    public static IEnumerable<PropertyInfo> GetPublicInstanceProperties(this Type type, string[] excludePropertyNames) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(propertyInfo => excludePropertyNames.Contains(propertyInfo.Name) is false);
    
    public static bool IsForRelatedEntityCollectionLink(this Type type) =>
        type.IsArrayOrEnumerable()
        && (type.GetArrayOrEnumerableElementType()?.IsEntityId() ?? false);

    public static bool IsForEmbeddedResource(this Type type) =>
        type.IsResource();

    public static bool IsForEmbeddedResourceCollection(this Type type) =>
        type.IsArrayOrEnumerable()
        && (type.GetArrayOrEnumerableElementType()?.IsResource() ?? false);

    public static bool IsForEmbeddedChildResourceCollection(this Type type) =>
        type.IsArrayOrEnumerable()
        && (type.GetArrayOrEnumerableElementType()?.IsChildResource() ?? false);
    
    public static bool IsResource(this Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.BaseType == typeof(Resource);

    public static bool IsChildResource(this Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.BaseType == typeof(ChildResource);

    public static bool IsValueObjectResource(this Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.BaseType == typeof(ValueObjectResource);
    
    public static bool IsLookupResource(this Type type) =>
        type == typeof(LookupResource);
}