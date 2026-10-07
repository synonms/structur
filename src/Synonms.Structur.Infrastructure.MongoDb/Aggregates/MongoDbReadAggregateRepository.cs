using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

public class MongoDbReadAggregateRepository<TAggregateRoot> : IReadAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IMongoCollection<BsonDocument> _mongoCollection;
    private readonly Type _recordType;
    
    public MongoDbReadAggregateRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        _recordType = mongoDatabaseConfiguration.GetRecordType<TAggregateRoot>();
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<BsonDocument>(mongoDatabaseConfiguration.GetCollectionName<TAggregateRoot>());
    }
    
    public virtual FilterDefinition<BsonDocument> GlobalFilter =>
        Builders<BsonDocument>.Filter.Eq(nameof(AggregateRoot<TAggregateRoot>.DeletedAction), BsonNull.Value);
    
    public async Task<bool> AnyAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken)
    {
        Func<TAggregateRoot, bool> compiledPredicate = predicate.Compile();
        List<TAggregateRoot> aggregateRoots = await LoadAsync(GlobalFilter, cancellationToken);
        return aggregateRoots.Any(compiledPredicate);
    }

    public async Task<Maybe<TAggregateRoot>> FindAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.And(
            GlobalFilter,
            Builders<BsonDocument>.Filter.Eq("_id", id.Value));

        BsonDocument? document = await _mongoCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document is null ? Maybe<TAggregateRoot>.None : MapToAggregateRoot(document);
    }

    public async Task<Maybe<TAggregateRoot>> FindFirstAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken)
    {
        Func<TAggregateRoot, bool> compiledPredicate = predicate.Compile();
        List<TAggregateRoot> aggregateRoots = await LoadAsync(GlobalFilter, cancellationToken);
        TAggregateRoot? aggregateRoot = aggregateRoots.FirstOrDefault(compiledPredicate);
        return aggregateRoot is null ? Maybe<TAggregateRoot>.None : aggregateRoot;
    }

    public Task<List<TAggregateRoot>> ListAllAsync(CancellationToken cancellationToken) =>
        LoadAsync(GlobalFilter, cancellationToken);

    public async Task<List<TAggregateRoot>> ListAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken)
    {
        Func<TAggregateRoot, bool> compiledPredicate = predicate.Compile();
        List<TAggregateRoot> aggregateRoots = await LoadAsync(GlobalFilter, cancellationToken);
        return aggregateRoots.Where(compiledPredicate).ToList();
    }

    public IQueryable<TAggregateRoot> Query() =>
        Load(GlobalFilter).AsQueryable();

    public IQueryable<TAggregateRoot> Query(Expression<Func<TAggregateRoot, bool>> predicate) =>
        Query().Where(predicate);

    public Task<PaginatedList<TAggregateRoot>> ReadAllAsync(int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken) =>
        Task.FromResult(PaginatedList<TAggregateRoot>.Create(sortFunc.Invoke(Query()), offset, limit));

    public Task<PaginatedList<TAggregateRoot>> ReadAsync(Expression<Func<TAggregateRoot, bool>> predicate, int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken) =>
        Task.FromResult(PaginatedList<TAggregateRoot>.Create(sortFunc.Invoke(Query(predicate)), offset, limit));

    public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<TAggregateRoot, bool>> predicate, Expression<Func<TAggregateRoot, TResult>> selector, CancellationToken cancellationToken) =>
        Task.FromResult(Query(predicate).Select(selector).ToList());

    protected TAggregateRoot MapToAggregateRoot(BsonDocument document) =>
        MongoDbRecordMapper.MapToAggregateRoot<TAggregateRoot>(document, _recordType);

    protected async Task<List<TAggregateRoot>> LoadAsync(FilterDefinition<BsonDocument> filter, CancellationToken cancellationToken)
    {
        List<BsonDocument> documents = await _mongoCollection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(MapToAggregateRoot).ToList();
    }

    protected List<TAggregateRoot> Load(FilterDefinition<BsonDocument> filter)
    {
        using IAsyncCursor<BsonDocument> cursor = _mongoCollection.FindSync(filter, cancellationToken: CancellationToken.None);
        return cursor.ToList().Select(MapToAggregateRoot).ToList();
    }
}