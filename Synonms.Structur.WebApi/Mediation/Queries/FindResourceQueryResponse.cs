using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Queries;

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