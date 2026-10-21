using System.Linq.Expressions;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.Aggregates;

public interface IAggregateRepository
{
}

public interface IReadAggregateRepository<TAggregateRoot> : IAggregateRepository
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    Task<bool> AnyAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default);

    Task<Maybe<TAggregateRoot>> FindAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken = default);

    Task<TAggregateRoot?> FindFirstAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregateRoot>> ListAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default);

    Task<long> CountAsync(AggregateQuery<TAggregateRoot> query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TOut>> SelectAsync<TOut>(AggregateQuery<TAggregateRoot> query, Expression<Func<TAggregateRoot, TOut>> selector, CancellationToken cancellationToken = default);
}

public interface IWriteAggregateRepository<TAggregateRoot> : IAggregateRepository
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    Task AddAsync(TAggregateRoot entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TAggregateRoot> entities, CancellationToken cancellationToken = default);
        
    Task DeleteAsync(TAggregateRoot entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken = default);

    Task UpdateAsync(TAggregateRoot entity, CancellationToken cancellationToken = default);
}