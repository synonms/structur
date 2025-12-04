using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.WebApi.Domain;

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

        aggregateRoot.MarkDeleted();
        
        return Result<TAggregateRoot>.SuccessAsync(aggregateRoot);
    }
}