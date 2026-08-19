using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.Events;

public interface IDomainEventDispatcher
{
    Task<Maybe<Fault>> DispatchAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}