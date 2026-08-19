using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class FindResourceQueryResponse<TAggregateRoot, TResource> : QueryResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public FindResourceQueryResponse(TResource resource, EntityTag entityTag)
    {
        Resource = resource;
        EntityTag = entityTag;
    }

    public TResource Resource { get; }
    
    public EntityTag EntityTag { get; }
}