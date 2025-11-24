using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Domain.Events;

public interface IDomainEventRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    Task<Maybe<Fault>> CreateAsync(DomainEvent<TAggregateRoot> domainEvent, CancellationToken cancellationToken = default);

    Task<IEnumerable<DomainEvent<TAggregateRoot>>> ReadAllAsync(EntityId<TAggregateRoot> aggregateId, CancellationToken cancellationToken = default);
}