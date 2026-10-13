using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Api.Server.Events;

public abstract class AggregateCreatedDomainEvent<TAggregateRoot, TResource> : DomainEvent<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>

{
    protected AggregateCreatedDomainEvent(ResourceEnvelope<TResource> envelope, Guid tenantId) : base((EntityId<TAggregateRoot>)envelope.Metadata.Id, tenantId)
    {
        Envelope = envelope;
    }

    public ResourceEnvelope<TResource> Envelope { get; protected set; }

    public override Task<Result<TAggregateRoot>> ApplyAsync(TAggregateRoot? aggregateRoot)
    {
        if (aggregateRoot is not null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNonNull("AggregateCreatedDomainEvent", nameof(TAggregateRoot));
            return Result<TAggregateRoot>.Failure(fault).AsAsync();
        }

        return CreateAggregate(Envelope).AsAsync();
    }

    public abstract Result<TAggregateRoot> CreateAggregate(ResourceEnvelope<TResource> envelope);
}
