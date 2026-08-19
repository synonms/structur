using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mapping;

public interface IResourceMapper
{
    object? Map(object value);
}

public interface IResourceMapper<in TAggregateRoot, out TResource> : IResourceMapper
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    TResource Map(TAggregateRoot aggregateRoot);
}