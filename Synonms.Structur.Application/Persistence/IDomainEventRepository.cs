using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Application.Persistence;

public interface IDomainEventRepository
{
    Task<Maybe<Fault>> CreateAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);

    Task<IEnumerable<DomainEvent>> ReadAllAsync(Type aggregateRootType, Guid aggregateId, CancellationToken cancellationToken = default);
}