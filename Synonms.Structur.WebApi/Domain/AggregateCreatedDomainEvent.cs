using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.WebApi.Domain;

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