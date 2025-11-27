using System.Reflection;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Application.Schema.Resources;

public static class PropertyInfoExtensions
{
    public static ResourcePropertyType GetResourcePropertyType(this PropertyInfo propertyInfo)
    {
        if (propertyInfo.PropertyType.IsArrayOrEnumerable())
        {
            Type? resourcePropertyEnumerableElementType = propertyInfo.PropertyType.GetArrayOrEnumerableElementType();

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
        
        if (propertyInfo.PropertyType.IsEntityId())
        {
            return ResourcePropertyType.RelatedResource;
        }

        if (propertyInfo.PropertyType.IsResource())
        {
            return ResourcePropertyType.EmbeddedResource;
        }

        if (propertyInfo.PropertyType.IsChildResource())
        {
            return ResourcePropertyType.EmbeddedChildResource;
        }

        if (propertyInfo.PropertyType.IsValueObjectResource())
        {
            return ResourcePropertyType.ValueObjectResource;
        }

        if (propertyInfo.PropertyType.IsLookupResource())
        {
            return ResourcePropertyType.EmbeddedLookupResource;
        }

        return ResourcePropertyType.VanillaScalar;
    }
}