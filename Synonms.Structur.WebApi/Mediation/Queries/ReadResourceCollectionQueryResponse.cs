using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Queries;

public class ReadResourceCollectionQueryResponse<TAggregateRoot, TResource> : QueryResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public ReadResourceCollectionQueryResponse(PaginatedList<TResource> resourceCollection)
    {
        ResourceCollection = resourceCollection;
    }

    public PaginatedList<TResource> ResourceCollection { get; }
}