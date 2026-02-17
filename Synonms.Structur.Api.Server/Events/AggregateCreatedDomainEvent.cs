using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Api.Server.Events;

public abstract class AggregateCreatedDomainEvent<TAggregateRoot, TResource> : DomainEvent<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    protected AggregateCreatedDomainEvent(EntityId<TAggregateRoot> aggregateId, TResource resource, Guid tenantId) : base(aggregateId, tenantId)
    {
        Resource = resource;
    }

    public TResource Resource { get; protected set; }

    public override Task<Result<TAggregateRoot>> ApplyAsync(TAggregateRoot? aggregateRoot)
    {
        if (aggregateRoot is not null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNonNull("AggregateCreatedDomainEvent", nameof(TAggregateRoot));
            return Result<TAggregateRoot>.Failure(fault).AsAsync();
        }

        return CreateAggregate(Resource).AsAsync();
    }

    public abstract Result<TAggregateRoot> CreateAggregate(TResource resource);
}