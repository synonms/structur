using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.Events;

public class DomainEventDispatcher(IEnumerable<IDomainEventHandler> domainEventHandlers) : IDomainEventDispatcher
{
    public async Task<Maybe<Fault>> DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        foreach (IDomainEventHandler domainEventHandler in domainEventHandlers)
        {
            if (domainEventHandler.DomainEventType != domainEvent.GetType())
            {
                continue;
            }
            
            Maybe<Fault> outcome = await domainEventHandler.HandleAsync(domainEvent, cancellationToken);
                
            if (outcome.IsSome)
            {
                return outcome;
            }
        }

        return Maybe<Fault>.None;
    }
}