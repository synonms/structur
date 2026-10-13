using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class FindResourceQueryResponse<TAggregateRoot, TResource> : QueryResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>

{
    public FindResourceQueryResponse(ResourceEnvelope<TResource> envelope, EntityTag entityTag)
    {
        Envelope = envelope;
        EntityTag = entityTag;
    }

    public ResourceEnvelope<TResource> Envelope { get; }

    public TResource Resource => Envelope.Data;

    public EntityTag EntityTag { get; }
}
