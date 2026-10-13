using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Commands;

public class CreateResourceCommand<TAggregateRoot, TResource> : Command
    where TAggregateRoot : AggregateRoot<TAggregateRoot>

{
    public CreateResourceCommand(ResourceEnvelope<TResource> envelope)
    {
        Envelope = envelope;
    }

    public ResourceEnvelope<TResource> Envelope { get; }

}
