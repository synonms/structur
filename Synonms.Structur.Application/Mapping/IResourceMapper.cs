using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Application.Mapping;

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