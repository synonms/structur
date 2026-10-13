using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class ReadResourceCollectionQueryResponse<TAggregateRoot, TResource> : QueryResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>

{
    public ReadResourceCollectionQueryResponse(PaginatedList<TResource> resourceCollection, IReadOnlyList<ResourceEnvelope<TResource>>? envelopes = null)
    {
        ResourceCollection = resourceCollection;
        Envelopes = envelopes ?? [];
    }

    public PaginatedList<TResource> ResourceCollection { get; }

    public IReadOnlyList<ResourceEnvelope<TResource>> Envelopes { get; }
}
