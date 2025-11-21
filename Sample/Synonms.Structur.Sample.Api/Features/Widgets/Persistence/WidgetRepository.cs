using System.Linq.Expressions;
using Synonms.Structur.Application.Persistence;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Sample.Api.Features.Widgets.Domain;

namespace Synonms.Structur.Sample.Api.Features.Widgets.Persistence;

public class WidgetRepository : IAggregateRepository<Widget>
{
    public Task AddAsync(Widget entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task AddRangeAsync(IEnumerable<Widget> entities, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AnyAsync(Expression<Func<Widget, bool>> predicate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Widget entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(EntityId<Widget> id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Expression<Func<Widget, bool>> predicate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Maybe<Widget>> FindAsync(EntityId<Widget> id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Maybe<Widget>> FindFirstAsync(Expression<Func<Widget, bool>> predicate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<Widget>> ListAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<Widget>> ListAsync(Expression<Func<Widget, bool>> predicate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IQueryable<Widget> Query()
    {
        throw new NotImplementedException();
    }

    public IQueryable<Widget> Query(Expression<Func<Widget, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedList<Widget>> ReadAllAsync(int offset, int limit, Func<IQueryable<Widget>, IQueryable<Widget>> sortFunc, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedList<Widget>> ReadAsync(Expression<Func<Widget, bool>> predicate, int offset, int limit, Func<IQueryable<Widget>, IQueryable<Widget>> sortFunc, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<Widget, bool>> predicate, Expression<Func<Widget, TResult>> selector, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Widget entity, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}