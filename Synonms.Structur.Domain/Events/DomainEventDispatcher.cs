using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.Events;

public class DomainEventDispatcher(IEnumerable<IDomainEventHandler> domainEventHandlers) : IDomainEventDispatcher
{
    public async Task<Maybe<Fault>> DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        List<Task<Maybe<Fault>>> tasks = domainEventHandlers
            .Where(domainEventHandler => domainEventHandler.DomainEventType == domainEvent.GetType())
            .Select(domainEventHandler => domainEventHandler.HandleAsync(domainEvent, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks);

        Maybe<Fault> outcome = tasks.Select(task => task.Result).Reduce(Fault (faults) => new AggregateFault(faults));
        
        return outcome;
    }
}