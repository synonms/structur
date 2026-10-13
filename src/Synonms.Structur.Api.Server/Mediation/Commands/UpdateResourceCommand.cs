using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Commands;

public class UpdateResourceCommand<TAggregateRoot, TResource> : Command
    where TAggregateRoot : AggregateRoot<TAggregateRoot>

{
    public UpdateResourceCommand(EntityId<TAggregateRoot> id, ResourceEnvelope<TResource> envelope)
    {
        Id = id;
        Envelope = envelope;
    }

    public EntityId<TAggregateRoot> Id { get; }

    public ResourceEnvelope<TResource> Envelope { get; }

    public EntityTag? IfMatch { get; init; }
}
