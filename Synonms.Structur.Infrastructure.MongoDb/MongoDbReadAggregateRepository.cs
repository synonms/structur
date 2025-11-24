using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Infrastructure.MongoDb;

public class MongoDbReadAggregateRepository<TAggregateRoot> : IReadAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IMongoCollection<TAggregateRoot> _mongoCollection;
    
    public MongoDbReadAggregateRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        if (mongoDatabaseConfiguration.CollectionNamesByAggregateType.ContainsKey(typeof(TAggregateRoot)) is false)
        {
            throw new InvalidOperationException($"Mongo collection name for type {typeof(TAggregateRoot).Name} is not configured.");
        }

        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<TAggregateRoot>(mongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(TAggregateRoot)]);
    }
    
    public virtual Expression<Func<TAggregateRoot, bool>> GlobalFilter => x => true;
    
    public async Task<bool> AnyAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken) =>
        await _mongoCollection.Find(CombineFilters(predicate)).FirstOrDefaultAsync(cancellationToken) is not null;

    public async Task<Maybe<TAggregateRoot>> FindAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken) =>
        await _mongoCollection.Find(CombineFilters(x => x.Id == id)).FirstOrDefaultAsync(cancellationToken);

    public async Task<Maybe<TAggregateRoot>> FindFirstAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken) =>
        await _mongoCollection.Find(CombineFilters(predicate)).FirstOrDefaultAsync(cancellationToken);

    public Task<List<TAggregateRoot>> ListAllAsync(CancellationToken cancellationToken) =>
        _mongoCollection.Find(GlobalFilter).ToListAsync(cancellationToken);

    public Task<List<TAggregateRoot>> ListAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken) =>
        _mongoCollection.Find(CombineFilters(predicate)).ToListAsync(cancellationToken);

    public IQueryable<TAggregateRoot> Query() =>
        _mongoCollection.AsQueryable().Where(GlobalFilter);

    public IQueryable<TAggregateRoot> Query(Expression<Func<TAggregateRoot, bool>> predicate) =>
        _mongoCollection.AsQueryable().Where(GlobalFilter).Where(predicate);

    public Task<PaginatedList<TAggregateRoot>> ReadAllAsync(int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken) =>
        Task.FromResult(PaginatedList<TAggregateRoot>.Create(sortFunc.Invoke(Query()), offset, limit));

    public Task<PaginatedList<TAggregateRoot>> ReadAsync(Expression<Func<TAggregateRoot, bool>> predicate, int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken) =>
        Task.FromResult(PaginatedList<TAggregateRoot>.Create(sortFunc.Invoke(Query(predicate)), offset, limit));

    public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<TAggregateRoot, bool>> predicate, Expression<Func<TAggregateRoot, TResult>> selector, CancellationToken cancellationToken) =>
        Query(predicate).Select(selector).ToListAsync(cancellationToken);
    
    private FilterDefinition<TAggregateRoot> CombineFilters(Expression<Func<TAggregateRoot, bool>> predicate)
    {
        FilterDefinitionBuilder<TAggregateRoot>? builder = Builders<TAggregateRoot>.Filter;
        FilterDefinition<TAggregateRoot>? combinedFilter = builder.And(GlobalFilter, predicate);
        return combinedFilter;
    }
}