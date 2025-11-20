using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Domain.Projections;

public abstract class Projection
{
    public virtual string Type => GetType().Name;
}

public abstract class Projection<TAggregateRoot> : Projection
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public EntityId<TAggregateRoot> AggregateId { get; protected set; } = EntityId<TAggregateRoot>.Uninitialised;
    
    public void Replay(EntityId<TAggregateRoot> aggregateId, IEnumerable<DomainEvent> eventHistory)
    {
        AggregateId = aggregateId;

        foreach (DomainEvent domainEvent in eventHistory)
        {
            domainEvent.Replay(this);
        }
    }
}