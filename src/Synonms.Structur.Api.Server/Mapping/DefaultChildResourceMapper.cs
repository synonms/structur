using System.Reflection;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Lookups;

namespace Synonms.Structur.Api.Server.Mapping;

public class DefaultChildResourceMapper<TAggregateMember, TChildResource> : IChildResourceMapper<TAggregateMember, TChildResource>
    where TAggregateMember : AggregateMember<TAggregateMember>
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
        ResourceEnvelope<TChildResource>? envelope = MapEnvelope(aggregateMember);
        return envelope is null ? default : envelope.Data;
    }

    public ResourceEnvelope<TChildResource>? MapEnvelope(TAggregateMember aggregateMember)
    {
        TChildResource childResource = ResourceContractActivator.Create<TChildResource>();
        ResourceMetadata metadata = new(aggregateMember.Id.Value, Link.EmptyLink(), includesSelfLink: false);
        ResourceMappingPlan plan = ResourceMappingPlan.Get(typeof(TAggregateMember), typeof(TChildResource));
        foreach (ResourceMappingBinding binding in plan.Bindings)
        {
            _logger.LogDebug("Mapping property {PropertyName} on child resource {ResourceType}", binding.Destination.Name, typeof(TChildResource).Name);
            try
            {
                object? sourceValue = binding.ReadSource?.Invoke(aggregateMember);
                object? mappedValue = MappingHelper.MapProperty<TChildResource>(binding, sourceValue, metadata,
                    _resourceMapperFactory, _childResourceMapperFactory, _routeGenerator,
                    typeof(TAggregateMember).Name, aggregateMember.Id.Value);
                binding.WriteDestination(childResource!, mappedValue);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Failed to map {typeof(TAggregateMember).FullName} -> {typeof(TChildResource).FullName}, property '{binding.Destination.Name}'.",
                    exception);
            }
        }

        return new ResourceEnvelope<TChildResource>(childResource, metadata);
    }
}
