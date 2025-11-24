using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.Domain.Events;

public interface IDomainEventHandler
{
    Type DomainEventType { get; }
    
    Task<Maybe<Fault>> HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}

public abstract class DomainEventHandler<TDomainEvent> : IDomainEventHandler
    where TDomainEvent : DomainEvent
{
    public Type DomainEventType => typeof(TDomainEvent);

    public async Task<Maybe<Fault>> HandleAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is TDomainEvent typedDomainEvent)
        {
            return await HandleAsync(typedDomainEvent, cancellationToken);
        }

        return new DomainEventFault("Domain event type mismatch. Handler expected {ExpectedDomainEventType}, got {ActualDomainEventType}.", DomainEventType.Name, domainEvent.GetType().Name);
    }

    public abstract Task<Maybe<Fault>> HandleAsync(TDomainEvent updatedEvent, CancellationToken cancellationToken = default);
}