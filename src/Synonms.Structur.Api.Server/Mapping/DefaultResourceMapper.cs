using System.Reflection;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mapping;

public class DefaultResourceMapper<TAggregateRoot, TResource> : IResourceMapper<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
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
        => MapEnvelope(aggregateRoot).Data;

    public ResourceEnvelope<TResource> MapEnvelope(TAggregateRoot aggregateRoot)
    {
        ResourceMetadata metadata = CreateMetadataForAggregate(aggregateRoot);
        TResource resource = ResourceContractActivator.Create<TResource>();
        ResourceMappingPlan plan = ResourceMappingPlan.Get(typeof(TAggregateRoot), typeof(TResource));
        foreach (ResourceMappingBinding binding in plan.Bindings)
        {
            _logger.LogDebug("Mapping property {PropertyName} on resource {ResourceType}", binding.Destination.Name, typeof(TResource).Name);
            try
            {
                object? sourceValue = binding.ReadSource?.Invoke(aggregateRoot);
                object? mappedValue = MappingHelper.MapProperty<TResource>(binding, sourceValue, metadata,
                    _resourceMapperFactory, _childResourceMapperFactory, _routeGenerator,
                    typeof(TAggregateRoot).Name, aggregateRoot.Id.Value);
                binding.WriteDestination(resource!, mappedValue);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Failed to map {typeof(TAggregateRoot).FullName} -> {typeof(TResource).FullName}, property '{binding.Destination.Name}'.",
                    exception);
            }
        }

        return new ResourceEnvelope<TResource>(resource, metadata);
    }

    private ResourceMetadata CreateMetadataForAggregate(TAggregateRoot aggregateRoot)
    {
        Uri selfUri = _routeGenerator.Item(typeof(TAggregateRoot), typeof(TResource), aggregateRoot.Id.Value);
        Link selfLink = Link.SelfLink(selfUri);

        ResourceLinks links = new();

        string relativeResourcePath = selfLink.Uri.IsAbsoluteUri ? selfLink.Uri.AbsolutePath : selfLink.Uri.OriginalString;

        StructurResourceAttribute? resourceAttribute = typeof(TAggregateRoot)
            .GetCustomAttributes<StructurResourceAttribute>()
            .SingleOrDefault(attribute => attribute.ResourceType == typeof(TResource));

        if (resourceAttribute is not null)
        {
            if (resourceAttribute.IsUpdateDisabled is false)
            {
                Uri editFormUri = _routeGenerator.EditForm(typeof(TAggregateRoot), typeof(TResource), aggregateRoot.Id.Value);
                Link editFormLink = Link.EditFormLink(editFormUri);
                links.Add(IanaLinkRelationConstants.Forms.Edit, editFormLink);
            }

            if (resourceAttribute.IsDeleteDisabled is false)
            {
                Link deleteSelfLink = Link.DeleteSelfLink(selfUri);
                links.Add(IanaHttpMethodConstants.Delete.ToLowerInvariant(), deleteSelfLink);
            }
        }

        links.Add("projections", Link.ProjectionsLink(new Uri(relativeResourcePath + "/projections", UriKind.Relative)));

        return new ResourceMetadata(aggregateRoot.Id.Value, selfLink, links);
    }
}
