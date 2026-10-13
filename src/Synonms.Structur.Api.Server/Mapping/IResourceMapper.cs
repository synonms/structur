using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mapping;

public interface IResourceMapper
{
    object? Map(object value);

    ResourceEnvelope<object>? MapEnvelope(object value);
}

public interface IResourceMapper<TAggregateRoot, TResource> : IResourceMapper
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    TResource Map(TAggregateRoot aggregateRoot);

    ResourceEnvelope<TResource> MapEnvelope(TAggregateRoot aggregateRoot) =>
        new(Map(aggregateRoot), new ResourceMetadata(aggregateRoot.Id.Value, Link.EmptyLink()));

    ResourceEnvelope<object>? IResourceMapper.MapEnvelope(object value)
    {
        if (value is not TAggregateRoot aggregateRoot)
        {
            return null;
        }

        ResourceEnvelope<TResource> envelope = MapEnvelope(aggregateRoot);
        return new ResourceEnvelope<object>(envelope.Data!, envelope.Metadata);
    }
}
