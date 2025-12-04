using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Domain.Events;

public abstract class DomainEvent : Entity<DomainEvent>
{
    public Guid TenantId { get; set; }
    
    public abstract string AggregateType { get; }
    
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    
    public abstract void Replay(Projection projection);
}

public abstract class DomainEvent<TAggregateRoot> : DomainEvent
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    protected DomainEvent(EntityId<TAggregateRoot> aggregateId, Guid tenantId)
    {
        TenantId = tenantId;
        AggregateId = aggregateId;
    }
    
    public override string AggregateType => typeof(TAggregateRoot).Name;
    
    public EntityId<TAggregateRoot> AggregateId { get; protected set; }
    
    public abstract Task<Result<TAggregateRoot>> ApplyAsync(TAggregateRoot? aggregateRoot);
}