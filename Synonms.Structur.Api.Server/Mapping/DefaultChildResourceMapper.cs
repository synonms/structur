using System.Reflection;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Lookups;

namespace Synonms.Structur.Api.Server.Mapping;

public class DefaultChildResourceMapper<TAggregateMember, TChildResource> : IChildResourceMapper<TAggregateMember, TChildResource>
    where TAggregateMember : AggregateMember<TAggregateMember>
    where TChildResource : ChildResource, new()
{
    private readonly ILogger<DefaultChildResourceMapper<TAggregateMember, TChildResource>> _logger;
    private readonly IResourceMapperFactory _resourceMapperFactory;
    private readonly IChildResourceMapperFactory _childResourceMapperFactory;
    private readonly IRouteGenerator _routeGenerator;

    public DefaultChildResourceMapper(ILogger<DefaultChildResourceMapper<TAggregateMember, TChildResource>> logger, IResourceMapperFactory resourceMapperFactory, IChildResourceMapperFactory childResourceMapperFactory, IRouteGenerator routeGenerator)
    {
        _logger = logger;
        _resourceMapperFactory = resourceMapperFactory;
        _childResourceMapperFactory = childResourceMapperFactory;
        _routeGenerator = routeGenerator;
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
        TChildResource childResource = CreateEmptyResourceForMember(aggregateMember);
        IEnumerable<PropertyInfo> childResourcePropertyInfos = typeof(TChildResource).GetChildResourceProperties();

        foreach (PropertyInfo childResourcePropertyInfo in childResourcePropertyInfos)
        {
            ResourcePropertyType resourcePropertyType = childResourcePropertyInfo.PropertyType.GetResourcePropertyType();
            
            _logger.LogDebug("Mapping property {PropertyName} of type {PropertyType} on child resource {ResourceType} and determined resource property type {ResourcePropertyType}", childResourcePropertyInfo.Name, childResourcePropertyInfo.PropertyType.Name, typeof(TChildResource).Name, resourcePropertyType);

            PropertyInfo? aggregateMemberPropertyInfo = childResourcePropertyInfo.GetMatchingPropertyFrom<TAggregateMember>();
            Type? sourcePropertyType = aggregateMemberPropertyInfo?.PropertyType;
            object? sourceValue = aggregateMemberPropertyInfo?.GetValue(aggregateMember);
            Type destinationPropertyType = childResourcePropertyInfo.PropertyType;
            
            object? childResourcePropertyValue = resourcePropertyType switch
            {
                ResourcePropertyType.EmbeddedResource => MappingHelper.MapEmbeddedResource(_resourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedResourceCollection => MappingHelper.MapEmbeddedResourceCollection(_resourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedChildResource => MappingHelper.MapEmbeddedChildResource(_childResourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedChildResourceCollection => MappingHelper.MapEmbeddedChildResourceCollection(_childResourceMapperFactory, sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.EmbeddedLookupResource => MappingHelper.MapOptionalLookup(sourceValue as Lookup),
                ResourcePropertyType.RelatedResource => MappingHelper.MapRelatedResource<TChildResource>(_routeGenerator, childResource.Links, childResourcePropertyInfo, sourcePropertyType, sourceValue),
                ResourcePropertyType.RelatedResourceCollection => MappingHelper.MapRelatedResourceCollection(_routeGenerator, childResource.Links, childResourcePropertyInfo, typeof(TAggregateMember).Name, aggregateMember.Id.Value, sourceValue),
                ResourcePropertyType.ComplexValueObjectResource => MappingHelper.MapComplexValueObject(sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.ComplexValueObjectResourceCollection => MappingHelper.MapComplexValueObjectCollection(sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.VanillaCollection => MappingHelper.MapVanillaCollection(sourcePropertyType, destinationPropertyType, sourceValue),
                ResourcePropertyType.VanillaScalar => MappingHelper.MapVanillaScalar(sourcePropertyType, sourceValue),
                _ => null
            };
            
            childResourcePropertyInfo.SetValue(childResource, childResourcePropertyValue);
        }

        return childResource;
    }
    
    private TChildResource CreateEmptyResourceForMember(TAggregateMember aggregateMember) =>
        new()
        {
            Id = aggregateMember.Id.Value,
        };
}