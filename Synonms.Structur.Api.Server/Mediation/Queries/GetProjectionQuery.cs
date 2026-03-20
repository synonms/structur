using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class GetProjectionQuery<TAggregateRoot, TProjection> : Query
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TProjection : Projection
{
    public GetProjectionQuery(Guid id)
    {
        Id = (EntityId<TAggregateRoot>)id;
    }
    
    public GetProjectionQuery(EntityId<TAggregateRoot> id)
    {
        Id = id;
    }

    public EntityId<TAggregateRoot> Id { get; }
    
    public EntityTag? IfNoneMatch { get; init; }
}