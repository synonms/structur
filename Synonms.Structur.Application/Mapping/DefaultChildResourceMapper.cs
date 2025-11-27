using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Application.Pipeline;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Application.Mapping;

public class DefaultChildResourceMapper<TAggregateMember, TChildResource> : IChildResourceMapper<TAggregateMember, TChildResource>
    where TAggregateMember : AggregateMember<TAggregateMember>
    where TChildResource : ChildResource, new()
{
    private readonly ILogger<DefaultChildResourceMapper<TAggregateMember, TChildResource>> _logger;
    private readonly IResourceMapperFactory _resourceMapperFactory;
    private readonly IChildResourceMapperFactory _childResourceMapperFactory;

    private class ChildResourcePropertyDetails
    {
        public ChildResourcePropertyDetails(TChildResource childResource, PropertyInfo childResourcePropertyInfo, TAggregateMember aggregateMember)
        {
            ChildResource = childResource;
            ChildResourcePropertyInfo = childResourcePropertyInfo;
            AggregateMember = aggregateMember;
        }

        public TChildResource ChildResource { get; }
        public PropertyInfo ChildResourcePropertyInfo { get; }
        public TAggregateMember AggregateMember { get; }
        public PropertyInfo? AggregateMemberPropertyInfo { get; init; }
        public object? AggregateMemberPropertyValue { get; init; }
    }

    public DefaultChildResourceMapper(ILogger<DefaultChildResourceMapper<TAggregateMember, TChildResource>> logger, IResourceMapperFactory resourceMapperFactory, IChildResourceMapperFactory childResourceMapperFactory)
    {
        _logger = logger;
        _resourceMapperFactory = resourceMapperFactory;
        _childResourceMapperFactory = childResourceMapperFactory;
    }
    
    public object? Map(object value)
    {
        if (value is TAggregateMember aggregateMember)
        {
            return Map(aggregateMember);
        }
        
        return null;
    }

    public TChildResource? Map(TAggregateMember aggregateMember)
    {
        TChildResource childResource = new()
        {
            Id = aggregateMember.Id.Value,
        };

        string[] propertiesToExclude = new[] { nameof(ChildResource.Id) };
        IEnumerable<PropertyInfo> childResourceProperties = typeof(TChildResource).GetPublicInstanceProperties(propertiesToExclude);

        foreach (PropertyInfo childResourcePropertyInfo in childResourceProperties)
        {
            PropertyInfo? aggregateMemberPropertyInfo = typeof(TAggregateMember).GetProperty(childResourcePropertyInfo.Name, BindingFlags.Instance | BindingFlags.Public);
            object? aggregateMemberPropertyValue = aggregateMemberPropertyInfo?.GetValue(aggregateMember);

            ChildResourcePropertyDetails propertyDetails = new(childResource, childResourcePropertyInfo, aggregateMember)
            {
                AggregateMemberPropertyInfo = aggregateMemberPropertyInfo,
                AggregateMemberPropertyValue = aggregateMemberPropertyValue
            };

            switch (childResourcePropertyInfo.GetResourcePropertyType())
            {
                case ResourcePropertyType.EmbeddedResource:
                    MapEmbeddedResource(propertyDetails);
                    break;
                case ResourcePropertyType.EmbeddedChildResource:
                    MapEmbeddedChildResource(propertyDetails);
                    break;
                case ResourcePropertyType.EmbeddedResourceCollection:
                    MapEmbeddedResourceCollection(propertyDetails);
                    break;
                case ResourcePropertyType.EmbeddedChildResourceCollection:
                    MapEmbeddedChildResourceCollection(propertyDetails);
                    break;
                case ResourcePropertyType.EmbeddedLookupResource:
                    MapEmbeddedLookupResource(propertyDetails);
                    break;
                case ResourcePropertyType.RelatedResource:
                    MapRelatedResource(propertyDetails);
                    break;
                case ResourcePropertyType.RelatedResourceCollection:
                    MapRelatedResourceCollection(propertyDetails);
                    break;
                case ResourcePropertyType.ValueObjectResource:
                    MapValueObjectResource(propertyDetails);
                    break;
                case ResourcePropertyType.ValueObjectResourceCollection:
                    MapValueObjectResourceCollection(propertyDetails);
                    break;
                case ResourcePropertyType.VanillaCollection:
                    MapVanillaCollection(propertyDetails);
                    break;
                case ResourcePropertyType.VanillaScalar:
                    MapVanillaScalar(propertyDetails);
                    break;
                case ResourcePropertyType.Unknown:
                default:
                    _logger.LogWarning("Unable to determine property type for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
                    continue;
            }
        }

        return childResource;
    }
    
    private object? CreateChildResourceFromAggregateMember(Type aggregateMemberType, Type childResourceType, object? aggregateMemberValue)
    {
        if (aggregateMemberValue is null)
        {
            return null;
        }
        
        if (childResourceType.IsChildResource() is false)
        {
            return null;
        }

        IChildResourceMapper? childResourceMapper = _childResourceMapperFactory.Create(aggregateMemberType, childResourceType);

        return childResourceMapper?.Map(aggregateMemberValue);
    }
    
    private object? CreateResourceFromAggregateRoot(Type aggregateRootType, Type resourceType, object? aggregateRootValue)
    {
        if (aggregateRootValue is null)
        {
            return null;
        }

        if (resourceType.IsResource() is false)
        {
            return null;
        }

        IResourceMapper? resourceMapper = _resourceMapperFactory.Create(aggregateRootType, resourceType);

        return resourceMapper?.Map(aggregateRootValue);
    }

    private object? CreateValueObjectResourceFromValueObject(Type valueObjectType, Type valueObjectResourceType, object? valueObjectValue)
    {
        if (valueObjectValue is null)
        {
            return null;
        }

        if (valueObjectResourceType.IsValueObjectResource() is false)
        {
            return null;
        }

        object? valueObjectResource = Activator.CreateInstance(valueObjectResourceType);
        
        IEnumerable<PropertyInfo> valueObjectResourceProperties = valueObjectResourceType.GetPublicInstanceProperties([]);

        foreach (PropertyInfo valueObjectResourcePropertyInfo in valueObjectResourceProperties)
        {
            PropertyInfo? valueObjectPropertyInfo = valueObjectType.GetProperty(valueObjectResourcePropertyInfo.Name, BindingFlags.Instance | BindingFlags.Public);
            
            if (valueObjectPropertyInfo is null)
            {
                continue;
            }
            
            object? valueObjectPropertyValue = valueObjectPropertyInfo.GetValue(valueObjectValue);
            
            valueObjectResourcePropertyInfo.SetValue(valueObjectResource, valueObjectPropertyValue);
        }

        return valueObjectResource;
    }
    
    private void MapEmbeddedChildResource(ChildResourcePropertyDetails propertyDetails)
    {
        // TChildResource.TChildResource <-> TAggregateMember.TAggregateMember: A member where we present a nested child resource.
                
        if (propertyDetails.AggregateMemberPropertyInfo is null || propertyDetails.AggregateMemberPropertyInfo.PropertyType.IsAggregateMember() is false)
        {
            _logger.LogWarning("Unable to determine AggregateMember type from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type aggregateMemberType = propertyDetails.AggregateMemberPropertyInfo.PropertyType;
        Type childResourceType = propertyDetails.ChildResourcePropertyInfo.PropertyType;
        object? value = propertyDetails.AggregateMemberPropertyValue;

        object? childResource = CreateChildResourceFromAggregateMember(aggregateMemberType, childResourceType, value);

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, childResource);
    }

    private void MapEmbeddedChildResourceCollection(ChildResourcePropertyDetails propertyDetails)
    {
        // TChildResource.IEnumerable<TChildResource> <-> TAggregateMember.IEnumerable<TAggregateMember>: A member collection where we present a nested child resource array.
                    
        Type? childResourcePropertyEnumerableElementType = propertyDetails.ChildResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (childResourcePropertyEnumerableElementType is null || childResourcePropertyEnumerableElementType.IsChildResource() is false)
        {
            _logger.LogWarning("Unable to determine enumerable ChildResource type for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type? aggregateMemberPropertyEnumerableElementType = propertyDetails.AggregateMemberPropertyInfo?.PropertyType.GetArrayOrEnumerableElementType();

        if (aggregateMemberPropertyEnumerableElementType is null || aggregateMemberPropertyEnumerableElementType.IsAggregateMember() is false)
        {
            _logger.LogWarning("Unable to determine enumerable AggregateMember type from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type childResourceCollectionType = typeof(List<>).MakeGenericType(childResourcePropertyEnumerableElementType);

        IList childResources = (IList)Activator.CreateInstance(childResourceCollectionType)!;

        if (propertyDetails.AggregateMemberPropertyValue is null)
        {
            propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, childResources);
            return;
        }
        
        if (propertyDetails.AggregateMemberPropertyValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? childResource = CreateChildResourceFromAggregateMember(aggregateMemberPropertyEnumerableElementType, childResourcePropertyEnumerableElementType, item);
                        
                if (childResource is not null)
                {
                    childResources?.Add(childResource);
                }
            }
        }

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, childResources);
    }
    
    private void MapEmbeddedLookupResource(ChildResourcePropertyDetails propertyDetails)
    {
        // TChildResource.LookupResource <-> TAggregateMember.TLookup: A member where we present a nested lookup resource.
                
        if (propertyDetails.AggregateMemberPropertyInfo is null)
        {
            _logger.LogWarning("Unable to get corresponding property info from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }
        
        if (propertyDetails.AggregateMemberPropertyInfo.PropertyType.IsLookup() is false)
        {
            _logger.LogWarning("Property type from the aggregate member '{aggregateMemberType}' property '{aggregateMemberPropertyType}' is expected to be a Lookup for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, propertyDetails.AggregateMemberPropertyInfo.Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Lookup? lookup = propertyDetails.AggregateMemberPropertyValue as Lookup;

        LookupResource? lookupResource = LookupResourceMapper.MapOptional(lookup);

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, lookupResource);
    }

    private void MapEmbeddedResource(ChildResourcePropertyDetails propertyDetails)
    {
        // TChildResource.TResource <-> TAggregateMember.TAggregateRoot: A related aggregate where we present an embedded resource.

        if (propertyDetails.AggregateMemberPropertyInfo is null || propertyDetails.AggregateMemberPropertyInfo.PropertyType.IsAggregateRoot() is false)
        {
            _logger.LogWarning("Unable to determine AggregateRoot type from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type embeddedAggregateRootType = propertyDetails.AggregateMemberPropertyInfo.PropertyType;
        Type embeddedResourceType = propertyDetails.ChildResourcePropertyInfo.PropertyType;
        object? value = propertyDetails.AggregateMemberPropertyValue;

        object? embeddedResource = CreateResourceFromAggregateRoot(embeddedAggregateRootType, embeddedResourceType, value);

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, embeddedResource);
    }

    private void MapEmbeddedResourceCollection(ChildResourcePropertyDetails propertyDetails)
    {
        // TChildResource.IEnumerable<TResource> <-> TAggregateMember.IEnumerable<TAggregateRoot>: A related resource collection where we present an embedded array.

        Type? childResourcePropertyEnumerableElementType = propertyDetails.ChildResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (childResourcePropertyEnumerableElementType is null || childResourcePropertyEnumerableElementType.IsResource() is false)
        {
            _logger.LogWarning("Unable to determine enumerable Resource type for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type? aggregateMemberPropertyEnumerableElementType = propertyDetails.AggregateMemberPropertyInfo?.PropertyType.GetArrayOrEnumerableElementType();

        if (aggregateMemberPropertyEnumerableElementType is null || aggregateMemberPropertyEnumerableElementType.IsAggregateRoot() is false)
        {
            _logger.LogWarning("Unable to determine enumerable AggregateRoot type from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type embeddedResourceCollectionType = typeof(List<>).MakeGenericType(childResourcePropertyEnumerableElementType);

        IList embeddedResources = (IList)Activator.CreateInstance(embeddedResourceCollectionType)!;

        if (propertyDetails.AggregateMemberPropertyValue is null)
        {
            propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, embeddedResources);
            return;
        }

        if (embeddedResources is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? embeddedResource = CreateResourceFromAggregateRoot(aggregateMemberPropertyEnumerableElementType, childResourcePropertyEnumerableElementType, item);

                if (embeddedResource is not null)
                {
                    embeddedResources?.Add(embeddedResource);
                }
            }
        }

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, embeddedResources);
    }
    
    private void MapValueObjectResource(ChildResourcePropertyDetails propertyDetails)
    {
        // TResource.TValueObjectResource <-> TAggregateRoot.TValueObject: A complex value object (multiple properties) where we present an embedded resource.

        if (propertyDetails.AggregateMemberPropertyInfo is null || propertyDetails.AggregateMemberPropertyInfo.PropertyType.IsValueObject() is false)
        {
            _logger.LogWarning("Unable to determine ValueObject type from the aggregate '{aggregateMemberType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type embeddedValueObjectType = propertyDetails.AggregateMemberPropertyInfo.PropertyType;
        Type embeddedValueObjectResourceType = propertyDetails.ChildResourcePropertyInfo.PropertyType;
        object? value = propertyDetails.AggregateMemberPropertyValue;

        object? embeddedResource = CreateValueObjectResourceFromValueObject(embeddedValueObjectType, embeddedValueObjectResourceType, value);

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, embeddedResource);
    }
    
    private void MapValueObjectResourceCollection(ChildResourcePropertyDetails propertyDetails)
    {
        // TResource.IEnumerable<TValueObjectResource> <-> TAggregateRoot.IEnumerable<TValueObject>: A complex value object collection where we present an embedded array.

        Type? valueObjectResourcePropertyEnumerableElementType = propertyDetails.ChildResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (valueObjectResourcePropertyEnumerableElementType is null || valueObjectResourcePropertyEnumerableElementType.IsValueObjectResource() is false)
        {
            _logger.LogWarning("Unable to determine enumerable ValueObjectResource type for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type? valueObjectPropertyEnumerableElementType = propertyDetails.AggregateMemberPropertyInfo?.PropertyType.GetArrayOrEnumerableElementType();

        if (valueObjectPropertyEnumerableElementType is null || valueObjectPropertyEnumerableElementType.IsValueObject() is false)
        {
            _logger.LogWarning("Unable to determine enumerable ValueObject type from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type valueObjectResourceCollectionType = typeof(List<>).MakeGenericType(valueObjectResourcePropertyEnumerableElementType);

        IList valueObjectResources = (IList)Activator.CreateInstance(valueObjectResourceCollectionType)!;

        if (propertyDetails.AggregateMemberPropertyValue is null)
        {
            propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, valueObjectResources);
            return;
        }

        if (propertyDetails.AggregateMemberPropertyValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? valueObjectResource = CreateValueObjectResourceFromValueObject(valueObjectPropertyEnumerableElementType, valueObjectResourcePropertyEnumerableElementType, item);

                if (valueObjectResource is not null)
                {
                    valueObjectResources?.Add(valueObjectResource);
                }
            }
        }

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, valueObjectResources);
    }
    
    private void MapRelatedResource(ChildResourcePropertyDetails propertyDetails)
    {
        // TChildResource.EntityId<TEntity> <-> TAggregateMember.EntityId<TEntity>: A related resource where we pass the Id and potentially present a link.

        if (propertyDetails.AggregateMemberPropertyInfo is null)
        {
            _logger.LogWarning("Unable to get corresponding property info from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, propertyDetails.AggregateMemberPropertyValue);

        // Check if there is a related resource property, i.e. if this property is 'WidgetId' then look for 'Widget'
        string relatedResourcePropertyName = propertyDetails.ChildResourcePropertyInfo.Name.Replace("Id", string.Empty);

        if (typeof(TChildResource).GetProperties(BindingFlags.Instance | BindingFlags.Public).Any(_ => _.Name.Equals(relatedResourcePropertyName)))
        {
            // Resource is embedded - don't add link
            return;
        }
                    
        Type relatedEntityIdType = propertyDetails.AggregateMemberPropertyInfo.PropertyType;
        Type relatedEntityType = relatedEntityIdType.GetGenericArguments().Single();
                
        // TODO: Support links on child resources??
        // Uri relationUri = _routeGenerator.Item(relatedEntityType, Guid.Parse(propertyDetails.AggregateMemberPropertyValue?.ToString() ?? Guid.Empty.ToString()));
        // Link relationLink = Link.RelationLink(relationUri);
        //
        // propertyDetails.ChildResource.Links.Add(relatedResourcePropertyName.ToCamelCase(), relationLink);
    }

    private void MapRelatedResourceCollection(ChildResourcePropertyDetails propertyDetails)
    {
        // TChildResource.IEnumerable<EntityId<TAggregateMember>>: A related resource collection where we present a link.
        // We only need the Id from the Aggregate for this (to build the url), not a related property value.

        Type? childResourcePropertyEnumerableElementType = propertyDetails.ChildResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (childResourcePropertyEnumerableElementType is null || childResourcePropertyEnumerableElementType.IsEntityId() is false)
        {
            _logger.LogWarning("Unable to determine enumerable EntityId type for resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type relatedEntityType = childResourcePropertyEnumerableElementType.GetGenericArguments().Single();
        EntityId<TAggregateMember> parentId = propertyDetails.AggregateMember.Id;
        string parentIdPropertyName = typeof(TAggregateMember).Name.ToCamelCase() + "Id";
        QueryParameters queryParameters = new()
        {
            [parentIdPropertyName] = parentId.Value
        };
        // TODO: Support links on child resources??
        // Uri relationUri = _routeGenerator.Collection(relatedEntityType, queryParameters);
        // Link relationLink = Link.RelationLink(relationUri);
        //
        // propertyDetails.ChildResource.Links.Add(propertyDetails.ChildResourcePropertyInfo.Name.ToCamelCase(), relationLink);
    }
    
    private void MapVanillaCollection(ChildResourcePropertyDetails propertyDetails)
    {
        // Either TChildResource.IEnumerable<VanillaScalar> <-> TAggregateMember.IEnumerable<ValueObject>: A DDD ValueObject collection where we present a nested array of vanilla values.
        // Or fallback to IEnumerable<vanilla> <-> IEnumerable<vanilla>
                    
        Type? childResourcePropertyEnumerableElementType = propertyDetails.ChildResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (childResourcePropertyEnumerableElementType is null)
        {
            _logger.LogWarning("Unable to determine enumerable element type for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type? aggregateMemberPropertyEnumerableElementType = propertyDetails.AggregateMemberPropertyInfo?.PropertyType.GetArrayOrEnumerableElementType();

        if (aggregateMemberPropertyEnumerableElementType is null)
        {
            _logger.LogWarning("Unable to determine enumerable element type from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
            return;
        }

        Type destinationCollectionType = typeof(List<>).MakeGenericType(childResourcePropertyEnumerableElementType);

        IList destinationList = (IList)Activator.CreateInstance(destinationCollectionType)!;

        if (propertyDetails.AggregateMemberPropertyValue is null)
        {
            propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, destinationList);
            return;
        }

        if (propertyDetails.AggregateMemberPropertyValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                if (aggregateMemberPropertyEnumerableElementType.IsValueObject())
                {
                    PropertyInfo? valueObjectValuePropertyInfo = aggregateMemberPropertyEnumerableElementType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

                    if (valueObjectValuePropertyInfo is null)
                    {
                        _logger.LogWarning("Unable to get value property of ValueObject from the aggregate member '{aggregateMemberType}' for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TAggregateMember).Name, typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
                        return;
                    }

                    object? rawValue = propertyDetails.AggregateMemberPropertyValue is null ? null : valueObjectValuePropertyInfo.GetValue(item);

                    destinationList?.Add(rawValue);

                    continue;
                }
                
                destinationList?.Add(item);
            }
        }

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, destinationList);
    }

    private void MapVanillaScalar(ChildResourcePropertyDetails propertyDetails)
    {
        // Either a TChildResource.VanillaScalar <-> TAggregateMember.ValueObject: A DDD value object property which we cast to a regular resource property
        // Or a fallback to a vanilla -> vanilla

        if (propertyDetails.AggregateMemberPropertyInfo?.PropertyType.IsValueObject() ?? false)
        {
            PropertyInfo? valueObjectValuePropertyInfo = propertyDetails.AggregateMemberPropertyInfo?.PropertyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

            if (valueObjectValuePropertyInfo is null)
            {
                _logger.LogWarning("Unable to get value property of ValueObject for child resource '{childResourceType}' property '{childResourcePropertyName}'.", typeof(TChildResource).Name, propertyDetails.ChildResourcePropertyInfo.Name);
                return;
            }

            object? rawValue = propertyDetails.AggregateMemberPropertyValue is null ? null : valueObjectValuePropertyInfo.GetValue(propertyDetails.AggregateMemberPropertyValue);

            propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, rawValue);

            return;
        }

        propertyDetails.ChildResourcePropertyInfo.SetValue(propertyDetails.ChildResource, propertyDetails.AggregateMemberPropertyValue);
    }
}