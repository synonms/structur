using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

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