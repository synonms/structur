using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Pipeline;
using Synonms.Structur.Application.Routing;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Application.Mapping;

public class DefaultResourceMapper<TAggregateRoot, TResource> : IResourceMapper<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new() 
{
    private readonly ILogger<DefaultResourceMapper<TAggregateRoot, TResource>> _logger;
    private readonly IResourceMapperFactory _resourceMapperFactory;
    private readonly IChildResourceMapperFactory _childResourceMapperFactory;
    private readonly IRouteGenerator _routeGenerator;

    private class ResourcePropertyDetails
    {
        public ResourcePropertyDetails(TResource resource, PropertyInfo resourcePropertyInfo, TAggregateRoot aggregateRoot)
        {
            Resource = resource;
            ResourcePropertyInfo = resourcePropertyInfo;
            AggregateRoot = aggregateRoot;
        }

        public TResource Resource { get; }
        public PropertyInfo ResourcePropertyInfo { get; }
        public TAggregateRoot AggregateRoot { get; }
        public PropertyInfo? AggregateRootPropertyInfo { get; init; }
        public object? AggregateRootPropertyValue { get; init; }
    }
    
    public DefaultResourceMapper(ILogger<DefaultResourceMapper<TAggregateRoot, TResource>> logger, IResourceMapperFactory resourceMapperFactory, IChildResourceMapperFactory childResourceMapperFactory, IRouteGenerator routeGenerator)
    {
        _logger = logger;
        _resourceMapperFactory = resourceMapperFactory;
        _childResourceMapperFactory = childResourceMapperFactory;
        _routeGenerator = routeGenerator;
    }
    
    public object? Map(object value)
    {
        if (value is TAggregateRoot aggregateRoot)
        {
            return Map(aggregateRoot);
        }

        return null;
    }
    
    public TResource Map(TAggregateRoot aggregateRoot)
    {
        Uri selfUri = _routeGenerator.Item(aggregateRoot.Id);
        Link selfLink = Link.SelfLink(selfUri);

        TResource resource = new()
        {
            Id = aggregateRoot.Id.Value,
            SelfLink = selfLink
        };

        StructurResourceAttribute? resourceAttribute = typeof(TAggregateRoot).GetCustomAttribute<StructurResourceAttribute>();

        if (resourceAttribute is not null)
        {
            if (resourceAttribute.IsUpdateDisabled is false)
            {
                Uri editFormUri = _routeGenerator.EditForm(aggregateRoot.Id);
                Link editFormLink = Link.EditFormLink(editFormUri);
                resource.Links.Add(IanaLinkRelationConstants.Forms.Edit, editFormLink);
            }

            if (resourceAttribute.IsDeleteDisabled is false)
            {
                Link deleteSelfLink = Link.DeleteSelfLink(selfUri);
                resource.Links.Add(IanaHttpMethodConstants.Delete.ToLowerInvariant(), deleteSelfLink);
            }
        }

        string[] propertiesToExclude = new[] { nameof(Resource.Id), nameof(Resource.SelfLink), nameof(Resource.Links) };
        IEnumerable<PropertyInfo> resourceProperties = typeof(TResource).GetPublicInstanceProperties(propertiesToExclude);
        
        foreach (PropertyInfo resourcePropertyInfo in resourceProperties)
        {
            PropertyInfo? aggregateRootPropertyInfo = typeof(TAggregateRoot).GetProperty(resourcePropertyInfo.Name, BindingFlags.Instance | BindingFlags.Public);
            object? aggregateRootPropertyValue = aggregateRootPropertyInfo?.GetValue(aggregateRoot);

            ResourcePropertyDetails propertyDetails = new(resource, resourcePropertyInfo, aggregateRoot)
            {
                AggregateRootPropertyInfo = aggregateRootPropertyInfo,
                AggregateRootPropertyValue = aggregateRootPropertyValue
            };
                
            switch (resourcePropertyInfo.GetResourcePropertyType())
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
                case ResourcePropertyType.VanillaCollection:
                    MapVanillaCollection(propertyDetails);
                    break;
                case ResourcePropertyType.VanillaScalar:
                    MapVanillaScalar(propertyDetails);
                    break;
                case ResourcePropertyType.Unknown:
                default:
                    _logger.LogWarning("Unable to determine property type for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
                    continue;
            }
        }

        return resource;
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

    private void MapEmbeddedChildResource(ResourcePropertyDetails propertyDetails)
    {
        // TResource.TChildResource <-> TAggregateRoot.TAggregateMember: A member where we present a nested child resource.
                
        if (propertyDetails.AggregateRootPropertyInfo is null || propertyDetails.AggregateRootPropertyInfo.PropertyType.IsAggregateMember() is false)
        {
            _logger.LogWarning("Unable to determine AggregateMember type from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type aggregateMemberType = propertyDetails.AggregateRootPropertyInfo.PropertyType;
        Type childResourceType = propertyDetails.ResourcePropertyInfo.PropertyType;
        object? value = propertyDetails.AggregateRootPropertyValue;

        object? childResource = CreateChildResourceFromAggregateMember(aggregateMemberType, childResourceType, value);

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, childResource);
    }

    private void MapEmbeddedChildResourceCollection(ResourcePropertyDetails propertyDetails)
    {
        // TResource.IEnumerable<TChildResource> <-> TAggregateRoot.IEnumerable<TAggregateMember>: A member collection where we present a nested child resource array.
                    
        Type? resourcePropertyEnumerableElementType = propertyDetails.ResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (resourcePropertyEnumerableElementType is null || resourcePropertyEnumerableElementType.IsChildResource() is false)
        {
            _logger.LogWarning("Unable to determine enumerable ChildResource type for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type? aggregateRootPropertyEnumerableElementType = propertyDetails.AggregateRootPropertyInfo?.PropertyType.GetArrayOrEnumerableElementType();

        if (aggregateRootPropertyEnumerableElementType is null || aggregateRootPropertyEnumerableElementType.IsAggregateMember() is false)
        {
            _logger.LogWarning("Unable to determine enumerable AggregateMember type from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type childResourceCollectionType = typeof(List<>).MakeGenericType(resourcePropertyEnumerableElementType);

        IList childResources = (IList)Activator.CreateInstance(childResourceCollectionType)!;

        if (propertyDetails.AggregateRootPropertyValue is null)
        {
            propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, childResources);
            return;
        }
        
        if (propertyDetails.AggregateRootPropertyValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? childResource = CreateChildResourceFromAggregateMember(aggregateRootPropertyEnumerableElementType, resourcePropertyEnumerableElementType, item);
                        
                if (childResource is not null)
                {
                    childResources?.Add(childResource);
                }
            }
        }

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, childResources);
    }
    
    private void MapEmbeddedLookupResource(ResourcePropertyDetails propertyDetails)
    {
        // TResource.LookupResource <-> TAggregateRoot.TLookup: A member where we present a nested lookup resource.
                
        if (propertyDetails.AggregateRootPropertyInfo is null)
        {
            _logger.LogWarning("Unable to get corresponding property info from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }
        
        if (propertyDetails.AggregateRootPropertyInfo.PropertyType.IsLookup() is false)
        {
            _logger.LogWarning("Property type from the aggregate '{aggregateRootType}' property '{aggregateRootPropertyType}' is expected to be a Lookup for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, propertyDetails.AggregateRootPropertyInfo.Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Lookup? lookup = propertyDetails.AggregateRootPropertyValue as Lookup;

        LookupResource? lookupResource = LookupResourceMapper.MapOptional(lookup);

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, lookupResource);
    }

    private void MapEmbeddedResource(ResourcePropertyDetails propertyDetails)
    {
        // TResource.TResource <-> TAggregateRoot.TAggregateRoot: A related aggregate where we present an embedded resource.

        if (propertyDetails.AggregateRootPropertyInfo is null || propertyDetails.AggregateRootPropertyInfo.PropertyType.IsAggregateRoot() is false)
        {
            _logger.LogWarning("Unable to determine AggregateRoot type from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type embeddedAggregateRootType = propertyDetails.AggregateRootPropertyInfo.PropertyType;
        Type embeddedResourceType = propertyDetails.ResourcePropertyInfo.PropertyType;
        object? value = propertyDetails.AggregateRootPropertyValue;

        object? embeddedResource = CreateResourceFromAggregateRoot(embeddedAggregateRootType, embeddedResourceType, value);

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, embeddedResource);
    }

    private void MapEmbeddedResourceCollection(ResourcePropertyDetails propertyDetails)
    {
        // TResource.IEnumerable<TResource> <-> TAggregateRoot.IEnumerable<TAggregateRoot>: A related resource collection where we present an embedded array.

        Type? resourcePropertyEnumerableElementType = propertyDetails.ResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (resourcePropertyEnumerableElementType is null || resourcePropertyEnumerableElementType.IsResource() is false)
        {
            _logger.LogWarning("Unable to determine enumerable Resource type for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type? aggregateRootPropertyEnumerableElementType = propertyDetails.AggregateRootPropertyInfo?.PropertyType.GetArrayOrEnumerableElementType();

        if (aggregateRootPropertyEnumerableElementType is null || aggregateRootPropertyEnumerableElementType.IsAggregateRoot() is false)
        {
            _logger.LogWarning("Unable to determine enumerable AggregateRoot type from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type embeddedResourceCollectionType = typeof(List<>).MakeGenericType(resourcePropertyEnumerableElementType);

        IList embeddedResources = (IList)Activator.CreateInstance(embeddedResourceCollectionType)!;

        if (propertyDetails.AggregateRootPropertyValue is null)
        {
            propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, embeddedResources);
            return;
        }

        if (embeddedResources is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                var embeddedResource = CreateResourceFromAggregateRoot(aggregateRootPropertyEnumerableElementType, resourcePropertyEnumerableElementType, item);

                if (embeddedResource is not null)
                {
                    embeddedResources?.Add(embeddedResource);
                }
            }
        }

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, embeddedResources);
    }
    
    private void MapRelatedResource(ResourcePropertyDetails propertyDetails)
    {
        // TResource.EntityId<TEntity> <-> TAggregateRoot.EntityId<TEntity>: A related resource where we pass the Id and potentially present a link.

        if (propertyDetails.AggregateRootPropertyInfo is null)
        {
            _logger.LogWarning("Unable to get corresponding property info from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, propertyDetails.AggregateRootPropertyValue);
        
        // Check if there is a related resource property, i.e. if this property is 'WidgetId' then look for 'Widget'
        string relatedResourcePropertyName = propertyDetails.ResourcePropertyInfo.Name.Replace("Id", string.Empty);

        if (typeof(TResource).GetProperties(BindingFlags.Instance | BindingFlags.Public).Any(_ => _.Name.Equals(relatedResourcePropertyName)))
        {
            // Resource is embedded - don't add link
            return;
        }

        Type relatedEntityIdType = propertyDetails.AggregateRootPropertyInfo.PropertyType;
        Type relatedEntityType = relatedEntityIdType.GetGenericArguments().Single();
                
        Uri relationUri = _routeGenerator.Item(relatedEntityType, Guid.Parse(propertyDetails.AggregateRootPropertyValue?.ToString() ?? Guid.Empty.ToString()));
        Link relationLink = Link.RelationLink(relationUri);

        propertyDetails.Resource.Links.Add(relatedResourcePropertyName.ToCamelCase(), relationLink);
    }

    private void MapRelatedResourceCollection(ResourcePropertyDetails propertyDetails)
    {
        // TResource.IEnumerable<EntityId<TAggregateRoot>>: A related resource collection where we present a link.
        // We only need the Id from the Aggregate for this (to build the url), not a related property value.

        Type? resourcePropertyEnumerableElementType = propertyDetails.ResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (resourcePropertyEnumerableElementType is null || resourcePropertyEnumerableElementType.IsEntityId() is false)
        {
            _logger.LogWarning("Unable to determine enumerable EntityId type for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type relatedEntityType = resourcePropertyEnumerableElementType.GetGenericArguments().Single();
        EntityId<TAggregateRoot> parentId = propertyDetails.AggregateRoot.Id;
        string parentIdPropertyName = typeof(TAggregateRoot).Name.ToCamelCase() + "Id";
        QueryParameters queryParameters = new()
        {
            [parentIdPropertyName] = parentId.Value
        };
        Uri relationUri = _routeGenerator.Collection(relatedEntityType, queryParameters);
        Link relationLink = Link.RelationLink(relationUri);

        propertyDetails.Resource.Links.Add(propertyDetails.ResourcePropertyInfo.Name.ToCamelCase(), relationLink);
    }
    
    private void MapVanillaCollection(ResourcePropertyDetails propertyDetails)
    {
        // Either TResource.IEnumerable<VanillaScalar> <-> TAggregateRoot.IEnumerable<ValueObject>: A DDD ValueObject collection where we present a nested array of vanilla values.
        // Or fallback to IEnumerable<vanilla> <-> IEnumerable<vanilla>
                    
        Type? resourcePropertyEnumerableElementType = propertyDetails.ResourcePropertyInfo.PropertyType.GetArrayOrEnumerableElementType();

        if (resourcePropertyEnumerableElementType is null)
        {
            _logger.LogWarning("Unable to determine enumerable element type for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type? aggregateRootPropertyEnumerableElementType = propertyDetails.AggregateRootPropertyInfo?.PropertyType.GetArrayOrEnumerableElementType();

        if (aggregateRootPropertyEnumerableElementType is null)
        {
            _logger.LogWarning("Unable to determine enumerable element type from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
            return;
        }

        Type destinationCollectionType = typeof(List<>).MakeGenericType(resourcePropertyEnumerableElementType);

        IList destinationList = (IList)Activator.CreateInstance(destinationCollectionType)!;

        if (propertyDetails.AggregateRootPropertyValue is null)
        {
            propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, destinationList);
            return;
        }

        if (propertyDetails.AggregateRootPropertyValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                if (aggregateRootPropertyEnumerableElementType.IsValueObject())
                {
                    PropertyInfo? valueObjectValuePropertyInfo = aggregateRootPropertyEnumerableElementType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

                    if (valueObjectValuePropertyInfo is null)
                    {
                        _logger.LogWarning("Unable to get value property of ValueObject from the aggregate '{aggregateRootType}' for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TAggregateRoot).Name, typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
                        return;
                    }

                    object? rawValue = propertyDetails.AggregateRootPropertyValue is null ? null : valueObjectValuePropertyInfo.GetValue(item);

                    destinationList?.Add(rawValue);

                    continue;
                }
                
                destinationList?.Add(item);
            }
        }

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, destinationList);
    }

    private void MapVanillaScalar(ResourcePropertyDetails propertyDetails)
    {
        // Either a TResource.VanillaScalar <-> TAggregateRoot.ValueObject: A DDD value object property which we cast to a regular resource property
        // Or a fallback to a vanilla -> vanilla

        if (propertyDetails.AggregateRootPropertyInfo?.PropertyType.IsValueObject() ?? false)
        {
            PropertyInfo? valueObjectValuePropertyInfo = propertyDetails.AggregateRootPropertyInfo?.PropertyType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

            if (valueObjectValuePropertyInfo is null)
            {
                _logger.LogWarning("Unable to get value property of ValueObject for resource '{resourceType}' property '{resourcePropertyName}'.", typeof(TResource).Name, propertyDetails.ResourcePropertyInfo.Name);
                return;
            }

            object? rawValue = propertyDetails.AggregateRootPropertyValue is null ? null : valueObjectValuePropertyInfo.GetValue(propertyDetails.AggregateRootPropertyValue);

            propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, rawValue);

            return;
        }

        propertyDetails.ResourcePropertyInfo.SetValue(propertyDetails.Resource, propertyDetails.AggregateRootPropertyValue);
    }
}