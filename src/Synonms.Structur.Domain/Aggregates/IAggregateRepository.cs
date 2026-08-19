using System.Linq.Expressions;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.Aggregates;

public interface IAggregateRepository
{
}

public interface IReadAggregateRepository<TAggregateRoot> : IAggregateRepository
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    Task<bool> AnyAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken = default);

    Task<Maybe<TAggregateRoot>> FindAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken = default);

    Task<Maybe<TAggregateRoot>> FindFirstAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken = default);

    Task<List<TAggregateRoot>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<List<TAggregateRoot>> ListAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken = default);

    IQueryable<TAggregateRoot> Query();

    IQueryable<TAggregateRoot> Query(Expression<Func<TAggregateRoot, bool>> predicate);

    Task<PaginatedList<TAggregateRoot>> ReadAllAsync(int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken = default);

    Task<PaginatedList<TAggregateRoot>> ReadAsync(Expression<Func<TAggregateRoot, bool>> predicate, int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken = default);

    Task<List<TResult>> SelectAsync<TResult>(Expression<Func<TAggregateRoot, bool>> predicate, Expression<Func<TAggregateRoot, TResult>> selector, CancellationToken cancellationToken = default);
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