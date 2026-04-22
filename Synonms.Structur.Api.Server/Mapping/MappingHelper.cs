using System.Collections;
using System.Reflection;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Api.Server.Mapping;

public static class MappingHelper
{
    public static object? ConvertValue(Type fromType, Type toType, object? fromValue)
    {
        if (fromValue is null) return null;
        
        if (fromType.IsSimpleValueObject())
        {
            object? rawValue = fromType.GetSimpleValueObjectValue(fromValue);
            
            // ValueObject<string> to Enum e.g. AddressType -> AddressTypeEnumeration
            if (fromType.GetSimpleValueObjectValueType() == typeof(string) && toType.IsEnum)
            {
                string? valueAsString = rawValue as string;
                object? convertedEnumValue = string.IsNullOrWhiteSpace(valueAsString) ? null : Enum.Parse(toType, valueAsString);
                return convertedEnumValue;
            }

            return rawValue;
        }

        return fromValue;
    }
    
    /// <summary>
    /// Map TAggregateRoot.TComplexValueObject to TResource.TComplexValueObjectResource : A complex value object where we present an embedded resource.
    /// </summary>
    /// <param name="sourceValueObjectType">The type of the property on the source Aggregate</param>
    /// <param name="destinationValueObjectResourceType">The type of the property on the destination Resource</param>
    /// <param name="valueObjectValue">The value of the property from the source Aggregate</param>
    /// <returns>Populated TComplexValueObjectResource, or null if source is null or types are incompatible</returns>
    public static object? MapComplexValueObject(Type? sourceValueObjectType, Type destinationValueObjectResourceType, object? valueObjectValue)
    {
        if (sourceValueObjectType is null) return null;
        if (valueObjectValue is null) return null;

        if (!destinationValueObjectResourceType.IsComplexValueObjectResource())
        {
            return null;
        }

        object? valueObjectResource = Activator.CreateInstance(destinationValueObjectResourceType);
        
        IEnumerable<PropertyInfo> valueObjectResourceProperties = destinationValueObjectResourceType.GetPublicInstanceProperties([]);

        foreach (PropertyInfo valueObjectResourcePropertyInfo in valueObjectResourceProperties)
        {
            PropertyInfo? valueObjectPropertyInfo = sourceValueObjectType.GetProperty(valueObjectResourcePropertyInfo.Name, BindingFlags.Instance | BindingFlags.Public);
            
            if (valueObjectPropertyInfo is null)
            {
                continue;
            }
            
            object? valueObjectPropertyValue = valueObjectPropertyInfo.GetValue(valueObjectValue);
            object? convertedValue = MappingHelper.ConvertValue(valueObjectPropertyInfo.PropertyType, valueObjectResourcePropertyInfo.PropertyType, valueObjectPropertyValue);
            
            valueObjectResourcePropertyInfo.SetValue(valueObjectResource, convertedValue);
        }

        return valueObjectResource;
    }
    
    /// <summary>
    /// Map TAggregateRoot.IEnumerable&lt;TComplexValueObject&gt; to TResource.IEnumerable&lt;TComplexValueObjectResource&gt; : A complex value object collection where we present an embedded array of resources.
    /// </summary>
    /// <param name="sourceValueObjectCollectionType">The type of the property on the source Aggregate</param>
    /// <param name="destinationValueObjectResourceCollectionType">The type of the property on the destination Resource</param>
    /// <param name="valueObjectValue">The value of the property from the source Aggregate</param>
    /// <returns>IList of populated TComplexValueObjectResource, or empty list if source is null or types are incompatible</returns>
    public static object? MapComplexValueObjectCollection(Type? sourceValueObjectCollectionType, Type destinationValueObjectResourceCollectionType, object? valueObjectValue)
    {
        Type? sourceValueObjectType = sourceValueObjectCollectionType?.GetArrayOrEnumerableElementType();

        if (sourceValueObjectType is null || !sourceValueObjectType.IsComplexValueObject())
        {
            return null;
        }

        Type? destinationValueObjectResourceType = destinationValueObjectResourceCollectionType.GetArrayOrEnumerableElementType();

        if (destinationValueObjectResourceType is null || !destinationValueObjectResourceType.IsComplexValueObjectResource())
        {
            return null;
        }

        Type destinationListType = typeof(List<>).MakeGenericType(destinationValueObjectResourceType);

        IList valueObjectResources = (IList)Activator.CreateInstance(destinationListType)!;

        if (valueObjectValue is null)
        {
            return valueObjectResources;
        }

        if (valueObjectValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? valueObjectResource = MapComplexValueObject(sourceValueObjectType, destinationValueObjectResourceType, item);

                if (valueObjectResource is not null)
                {
                    valueObjectResources?.Add(valueObjectResource);
                }
            }
        }

        return valueObjectResources;
    }
    
    public static object? MapEmbeddedResource(IResourceMapperFactory resourceMapperFactory, Type? sourceAggregateRootType, Type destinationResourceType, object? aggregateRootValue)
    {
        // TAggregateRoot.TAggregateRoot to TResource.TResource : A related aggregate where we present an embedded resource.

        if (sourceAggregateRootType is null) return null;
        
        IResourceMapper? resourceMapper = resourceMapperFactory.Create(sourceAggregateRootType, destinationResourceType);

        object? destinationResourceValue = aggregateRootValue is null ? null : resourceMapper?.Map(aggregateRootValue);
        
        return destinationResourceValue;
    }

    public static object? MapEmbeddedChildResource(IChildResourceMapperFactory childResourceMapperFactory, Type? sourceAggregateMemberType, Type destinationChildResourceType, object? aggregateMemberValue)
    {
        // TAggregateRoot.TAggregateMember to TResource.TChildResource : A member where we present a nested child resource.

        if (sourceAggregateMemberType is null) return null;
        
        IChildResourceMapper? childResourceMapper = childResourceMapperFactory.Create(sourceAggregateMemberType, destinationChildResourceType);
        
        object? destinationChildResourceValue = aggregateMemberValue is null ? null : childResourceMapper?.Map(aggregateMemberValue);

        return destinationChildResourceValue;
    }

    public static object? MapEmbeddedResourceCollection(IResourceMapperFactory resourceMapperFactory, Type? sourceAggregateRootCollectionType, Type destinationResourceCollectionType, object? aggregateRootCollectionValue)
    {
        // TAggregateRoot.IEnumerable<TAggregateRoot> to TResource.IEnumerable<TResource> : A related resource collection where we present an embedded array.
        
        if (sourceAggregateRootCollectionType is null) return null;
        
        Type? sourceAggregateRootType = sourceAggregateRootCollectionType.GetArrayOrEnumerableElementType();

        if (sourceAggregateRootType is null || !sourceAggregateRootType.IsAggregateRoot()) return null;
        
        Type? destinationResourceType = destinationResourceCollectionType.GetArrayOrEnumerableElementType();

        if (destinationResourceType is null || !destinationResourceType.IsResource()) return null;

        Type destinationListType = typeof(List<>).MakeGenericType(destinationResourceType);

        IList destinationList = (IList)Activator.CreateInstance(destinationListType)!;

        if (aggregateRootCollectionValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? embeddedResource = MapEmbeddedResource(resourceMapperFactory, sourceAggregateRootType, destinationResourceType, item);

                if (embeddedResource is not null)
                {
                    destinationList?.Add(embeddedResource);
                }
            }
        }

        return destinationList;
    }

    public static object? MapEmbeddedChildResourceCollection(IChildResourceMapperFactory childResourceMapperFactory, Type? sourceAggregateMemberCollectionType, Type destinationChildResourceCollectionType, object? aggregateMemberCollectionValue)
    {
        // TAggregateRoot.IEnumerable<TAggregateMember> to TResource.IEnumerable<TChildResource> : A member collection where we present a nested child resource array.
                    
        if (sourceAggregateMemberCollectionType is null) return null;
        
        Type? sourceAggregateMemberType = sourceAggregateMemberCollectionType.GetArrayOrEnumerableElementType();

        if (sourceAggregateMemberType is null || !sourceAggregateMemberType.IsAggregateMember()) return null;
        
        Type? destinationChildResourceType = destinationChildResourceCollectionType.GetArrayOrEnumerableElementType();

        if (destinationChildResourceType is null || !destinationChildResourceType.IsChildResource()) return null;

        Type destinationListType = typeof(List<>).MakeGenericType(destinationChildResourceType);

        IList destinationList = (IList)Activator.CreateInstance(destinationListType)!;

        if (aggregateMemberCollectionValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? embeddedChildResource = MapEmbeddedChildResource(childResourceMapperFactory, sourceAggregateMemberType, destinationChildResourceType, item);
                        
                if (embeddedChildResource is not null)
                {
                    destinationList?.Add(embeddedChildResource);
                }
            }
        }

        return destinationList;
    }
    
    public static LookupResource MapLookup(Lookup lookup) =>
        new()
        {
            Id = lookup.Id.Value,
            LookupCode = lookup.LookupCode,
            LookupName = lookup.LookupName,
        };
    
    public static LookupResource? MapOptionalLookup(Lookup? lookup) =>
        lookup is null
            ? null
            : new LookupResource
            {
                Id = lookup.Id.Value,
                LookupCode = lookup.LookupCode,
                LookupName = lookup.LookupName,
            };

    public static object? MapRelatedResource<TDestination>(IRouteGenerator routeGenerator, ResourceLinks linksToAddTo, PropertyInfo resourcePropertyInfo, Type? entityIdType, object? aggregateRootValue)
    {
        // TAggregateRoot.EntityId<TEntity> to TResource.EntityId<TEntity>/Guid : A related resource where we pass the Id and potentially present a link.
        // or
        // TAggregateMember.EntityId<TEntity> to TChildResource.EntityId<TEntity>/Guid : A related resource where we pass the Id and potentially present a link.

        // Check if there is a related resource property, i.e. if this property is 'WidgetId' then look for 'Widget'
        string relatedResourcePropertyName = resourcePropertyInfo.Name.Replace("Id", string.Empty);

        if (typeof(TDestination).GetProperties(BindingFlags.Instance | BindingFlags.Public).Any(_ => _.Name.Equals(relatedResourcePropertyName)))
        {
            // Resource is embedded - don't add link
            return aggregateRootValue;
        }
        
        if (entityIdType is null) return aggregateRootValue;

        Type relatedEntityType = entityIdType.GetGenericArguments().Single();
        
        Guid guid = Guid.Parse(aggregateRootValue?.ToString() ?? Guid.Empty.ToString());
        
        Uri relationUri = routeGenerator.Item(relatedEntityType, guid);
        Link relationLink = Link.RelationLink(relationUri);

        linksToAddTo.Add(relatedResourcePropertyName.ToCamelCase(), relationLink);
        
        return guid;
    }

    public static object? MapRelatedResourceCollection(IRouteGenerator routeGenerator, ResourceLinks linksToAddTo, PropertyInfo resourcePropertyInfo, string sourceEntityTypeName, Guid sourceEntityId, object? sourceValue)
    {
        // TAggregateRoot.IEnumerable<EntityId<TAggregateRoot>> to TResource.IEnumerable<EntityId<TAggregateRoot>/Guid>: A related resource collection where we present a link.
        // or
        // TAggregateRoot.IEnumerable<EntityId<TAggregateRoot>> to TResource.IEnumerable<EntityId<TAggregateRoot>/Guid>: A related resource collection where we present a link.
        // We only need the Id from the Aggregate for this (to build the url), not a related property value.

        Type? entityIdType = resourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (entityIdType is null || !entityIdType.IsEntityId()) return null;

        Type relatedEntityType = entityIdType.GetGenericArguments().Single();
        string parentIdPropertyName = sourceEntityTypeName.ToCamelCase() + "Id";  
        
        // TODO: This likely won't work for AggregateMember and may not for Roots either - think of something else
        QueryParameters queryParameters = new()
        {
            [parentIdPropertyName] = sourceEntityId
        };
        Uri relationUri = routeGenerator.Collection(relatedEntityType, queryParameters);
        Link relationLink = Link.RelationLink(relationUri);

        linksToAddTo.Add(resourcePropertyInfo.Name.ToCamelCase(), relationLink);
        
        List<Guid> guids = [];
        
        if (sourceValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                if (Guid.TryParse(item.ToString(), out Guid guid))
                {
                    guids.Add(guid);
                }
            }
        }
        
        return guids;
    }
    
    public static object? MapVanillaScalar(Type? sourcePropertyType, object? sourceValue)
    {
        // Either a TAggregateRoot.SimpleValueObject -> TResource.VanillaScalar : A simple (single value) DDD value object property which we cast to a regular resource property
        // Or a EntityId<TEntity> -> TResource.Guid : Entity Id to Guid
        // Or a fallback to a vanilla -> vanilla

        if (sourcePropertyType is null) return null;
        
        if (sourcePropertyType.IsSimpleValueObject())
        {
            object? rawValue = sourcePropertyType.GetSimpleValueObjectValue(sourceValue);

            return rawValue;
        }

        if (sourcePropertyType.IsEntityId())
        {
            object? rawValue = sourceValue as Guid?;

            return rawValue;
        }

        return sourceValue;
    }
    
    public static object? MapVanillaCollection(Type? sourcePropertyCollectionType, Type destinationPropertyCollectionType, object? sourceValue)
    {
        // Either TAggregateRoot.IEnumerable<SimpleValueObject> -> TResource.IEnumerable<VanillaScalar> : A DDD ValueObject collection where we present a nested array of vanilla values.
        // Or fallback to IEnumerable<vanilla> -> IEnumerable<vanilla>
                    
        Type? sourceVanillaType = sourcePropertyCollectionType?.GetArrayOrEnumerableElementType();

        if (sourceVanillaType is null) return null;

        Type? destinationVanillaType = destinationPropertyCollectionType.GetArrayOrEnumerableElementType();

        if (destinationVanillaType is null) return null;

        Type destinationListType = typeof(List<>).MakeGenericType(destinationVanillaType);

        IList destinationList = (IList)Activator.CreateInstance(destinationListType)!;

        if (sourceValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                if (sourceVanillaType.IsSimpleValueObject())
                {
                    object? vanillaValue = MapVanillaScalar(sourceVanillaType, item);

                    if (vanillaValue is not null)
                    {
                        destinationList?.Add(vanillaValue);
                    }

                    continue;
                }
                
                destinationList?.Add(item);
            }
        }

        return destinationList;
    }
}