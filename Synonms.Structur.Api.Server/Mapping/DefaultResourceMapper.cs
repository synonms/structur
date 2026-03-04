using System.Reflection;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Lookups;

namespace Synonms.Structur.Api.Server.Mapping;

public class DefaultResourceMapper<TAggregateRoot, TResource> : IResourceMapper<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new() 
{
    private readonly ILogger<DefaultResourceMapper<TAggregateRoot, TResource>> _logger;
    private readonly IResourceMapperFactory _resourceMapperFactory;
    private readonly IChildResourceMapperFactory _childResourceMapperFactory;
    private readonly IRouteGenerator _routeGenerator;

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
        TResource resource = CreateEmptyResourceForAggregate(_routeGenerator, aggregateRoot);
        IEnumerable<PropertyInfo> resourcePropertyInfos = typeof(TResource).GetResourceProperties();
        
        foreach (PropertyInfo resourcePropertyInfo in resourcePropertyInfos)
        {
            PropertyInfo? aggregateRootPropertyInfo = resourcePropertyInfo.GetMatchingPropertyFrom<TAggregateRoot>();
            Type? sourcePropertyType = aggregateRootPropertyInfo?.PropertyType;
            object? sourceValue = aggregateRootPropertyInfo?.GetValue(aggregateRoot);
            Type destinationPropertyType = resourcePropertyInfo.PropertyType;
        
            object? resourcePropertyValue = resourcePropertyInfo.PropertyType.GetResourcePropertyType() switch
            {
                ResourcePropertyType.EmbeddedResource => MappingHelper.MapEmbeddedResource(_resourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedResourceCollection => MappingHelper.MapEmbeddedResourceCollection(_resourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedChildResource => MappingHelper.MapEmbeddedChildResource(_childResourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedChildResourceCollection => MappingHelper.MapEmbeddedChildResourceCollection(_childResourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedLookupResource => MappingHelper.MapOptionalLookup(sourceValue as Lookup),
                ResourcePropertyType.RelatedResource => MappingHelper.MapRelatedResource<TResource>(_routeGenerator, resource.Links, resourcePropertyInfo, sourcePropertyType, sourceValue),
                ResourcePropertyType.RelatedResourceCollection => MappingHelper.MapRelatedResourceCollection(_routeGenerator, resource.Links, resourcePropertyInfo, typeof(TAggregateRoot).Name, aggregateRoot.Id.Value, sourceValue),
                ResourcePropertyType.ComplexValueObjectResource => MappingHelper.MapComplexValueObject(sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.ComplexValueObjectResourceCollection => MappingHelper.MapComplexValueObjectCollection(sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.VanillaCollection => MappingHelper.MapVanillaCollection(sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.VanillaScalar => MappingHelper.MapVanillaScalar(sourcePropertyType, sourceValue),
                _ => null
            };
            
            resourcePropertyInfo.SetValue(resource, resourcePropertyValue);
        }

        return resource;
    }

    private TResource CreateEmptyResourceForAggregate(IRouteGenerator routeGenerator, TAggregateRoot aggregateRoot)
    {
        Uri selfUri = routeGenerator.Item(aggregateRoot.Id);
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
                Uri editFormUri = routeGenerator.EditForm(aggregateRoot.Id);
                Link editFormLink = Link.EditFormLink(editFormUri);
                resource.Links.Add(IanaLinkRelationConstants.Forms.Edit, editFormLink);
            }

            if (resourceAttribute.IsDeleteDisabled is false)
            {
                Link deleteSelfLink = Link.DeleteSelfLink(selfUri);
                resource.Links.Add(IanaHttpMethodConstants.Delete.ToLowerInvariant(), deleteSelfLink);
            }
        }

        return resource;
    }
}