using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.System;

namespace Synonms.Structur.Api.Core.Schema.Resources;

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
        Type nonNullableType = propertyType.StripNullable();

        if (nonNullableType.IsArrayOrEnumerable())
        {
            Type? enumerableElementType = nonNullableType.GetArrayOrEnumerableElementType();

            if (enumerableElementType is null)
            {
                return ResourcePropertyType.Unknown;
            }

            if (enumerableElementType.IsEntityId() || enumerableElementType ==  typeof(Guid))
            {
                return ResourcePropertyType.RelatedResourceCollection;
            }

            if (enumerableElementType.IsResource())
            {
                return ResourcePropertyType.EmbeddedResourceCollection;
            }

            if (enumerableElementType.IsChildResource())
            {
                return ResourcePropertyType.EmbeddedChildResourceCollection;
            }

            if (enumerableElementType.IsComplexValueObjectResource())
            {
                return ResourcePropertyType.ComplexValueObjectResourceCollection;
            }

            return ResourcePropertyType.VanillaCollection;
        }
        
        if (nonNullableType.IsEntityId() || nonNullableType ==  typeof(Guid))
        {
            return ResourcePropertyType.RelatedResource;
        }

        if (nonNullableType.IsResource())
        {
            return ResourcePropertyType.EmbeddedResource;
        }

        if (nonNullableType.IsChildResource())
        {
            return ResourcePropertyType.EmbeddedChildResource;
        }

        if (nonNullableType.IsComplexValueObjectResource())
        {
            return ResourcePropertyType.ComplexValueObjectResource;
        }

        if (nonNullableType.IsLookupResource())
        {
            return ResourcePropertyType.EmbeddedLookupResource;
        }

        if (nonNullableType.IsEnum)
        {
            return ResourcePropertyType.Enumeration;
        }
        
        return ResourcePropertyType.VanillaScalar;
    }
        
    public static bool IsResource(this Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.BaseType == typeof(Resource);

    public static bool IsChildResource(this Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.BaseType == typeof(ChildResource);

    public static bool IsComplexValueObjectResource(this Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.BaseType == typeof(ComplexValueObjectResource);
    
    public static bool IsLookupResource(this Type type) =>
        type == typeof(LookupResource);

    public static bool IsForRelatedEntityCollectionLink(this Type type) =>
        type.IsArrayOrEnumerable()
        && (type.GetArrayOrEnumerableElementType()?.IsEntityId() ?? false);
}