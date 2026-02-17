using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class FindResourceQuery<TAggregateRoot, TResource> : Query
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public FindResourceQuery(EntityId<TAggregateRoot> id)
    {
        Id = id;
    }

    public EntityId<TAggregateRoot> Id { get; }
    
    public EntityTag? IfNoneMatch { get; init; }
}