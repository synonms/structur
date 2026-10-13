using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Api.Server.Events;

public abstract class AggregateUpdatedDomainEvent<TAggregateRoot, TResource> : DomainEvent<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>

{
    protected AggregateUpdatedDomainEvent(EntityId<TAggregateRoot> aggregateId, ResourceEnvelope<TResource> envelope, Guid tenantId) : base(aggregateId, tenantId)
    {
        Envelope = envelope;
    }

    public ResourceEnvelope<TResource> Envelope { get; protected set; }

    public override Task<Result<TAggregateRoot>> ApplyAsync(TAggregateRoot? aggregateRoot)
    {
        if (aggregateRoot is null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNull("AggregateUpdatedDomainEvent", nameof(TAggregateRoot));
            return Result<TAggregateRoot>.Failure(fault).AsAsync();
        }

        return UpdateAggregate(aggregateRoot, Envelope).ToResultAsync(() => Result<TAggregateRoot>.SuccessAsync(aggregateRoot));
    }

    public abstract Maybe<Fault> UpdateAggregate(TAggregateRoot aggregateRoot, ResourceEnvelope<TResource> envelope);
}
