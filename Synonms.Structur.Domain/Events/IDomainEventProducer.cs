using System.Collections.Concurrent;

namespace Synonms.Structur.Domain.Events;

public interface IDomainEventProducer
{
    public IProducerConsumerCollection<DomainEvent> DomainEvents { get; }
}