using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Queries;

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