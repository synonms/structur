using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.WebApi.Domain;

public abstract class AggregateUpdatedDomainEvent<TAggregateRoot, TResource> : DomainEvent<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    protected AggregateUpdatedDomainEvent(EntityId<TAggregateRoot> aggregateId, TResource resource, Guid tenantId) : base(aggregateId, tenantId)
    {
        Resource = resource;
    }
    
    public TResource Resource { get; protected set; }

    public override Task<Result<TAggregateRoot>> ApplyAsync(TAggregateRoot? aggregateRoot)
    {
        if (aggregateRoot is null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNull("AggregateUpdatedDomainEvent", nameof(TAggregateRoot));
            return Result<TAggregateRoot>.Failure(fault).AsAsync();
        }

        return UpdateAggregate(aggregateRoot, Resource).ToResultAsync(() => Result<TAggregateRoot>.SuccessAsync(aggregateRoot));
    }

    public abstract Maybe<Fault> UpdateAggregate(TAggregateRoot aggregateRoot, TResource resource);
}