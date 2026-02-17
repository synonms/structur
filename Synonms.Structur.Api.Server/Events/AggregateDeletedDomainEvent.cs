using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Api.Server.Events;

public abstract class AggregateDeletedDomainEvent<TAggregateRoot> : DomainEvent<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    protected AggregateDeletedDomainEvent(EntityId<TAggregateRoot> aggregateId, Guid tenantId) : base(aggregateId, tenantId)
    {
    }
    
    public override Task<Result<TAggregateRoot>> ApplyAsync(TAggregateRoot? aggregateRoot)
    {
        if (aggregateRoot is null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNull("AggregateDeletedDomainEvent", nameof(TAggregateRoot));
            return Result<TAggregateRoot>.Failure(fault).AsAsync();
        }

        return DeleteAggregate(aggregateRoot).AsAsync();
    }
    
    public abstract Result<TAggregateRoot> DeleteAggregate(TAggregateRoot aggregateRoot);
}