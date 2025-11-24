using System.Linq.Expressions;
using MongoDB.Driver;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Transactions;

namespace Synonms.Structur.Infrastructure.MongoDb;

public class MongoDbWriteAggregateRepository<TAggregateRoot> : IWriteAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IMongoCollection<TAggregateRoot> _mongoCollection;
    private readonly MongoDomainTransaction _transaction;
    
    public MongoDbWriteAggregateRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration, IDomainTransaction domainTransaction)
    {
        if (domainTransaction is not MongoDomainTransaction mongoDomainTransaction)
        {
            throw new InvalidOperationException($"Mongo repository requires specific implementation of {nameof(IDomainTransaction)}.");
        }
        
        _transaction = mongoDomainTransaction;
        
        if (mongoDatabaseConfiguration.CollectionNamesByAggregateType.ContainsKey(typeof(TAggregateRoot)) is false)
        {
            throw new InvalidOperationException($"Mongo collection name for type {typeof(TAggregateRoot).Name} is not configured.");
        }

        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<TAggregateRoot>(mongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(TAggregateRoot)]);
    }
    
    public Task AddAsync(TAggregateRoot entity, CancellationToken cancellationToken) =>
        _mongoCollection.InsertOneAsync(_transaction.Session, entity, cancellationToken: cancellationToken);

    public Task AddRangeAsync(IEnumerable<TAggregateRoot> entities, CancellationToken cancellationToken) =>
        _mongoCollection.InsertManyAsync(_transaction.Session, entities, cancellationToken: cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(TAggregateRoot entity, CancellationToken cancellationToken) =>
        _mongoCollection.DeleteOneAsync(_transaction.Session, x => x.Id == entity.Id, cancellationToken: cancellationToken);

    public Task DeleteAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken) =>
        _mongoCollection.DeleteOneAsync(_transaction.Session, x => x.Id == id, cancellationToken: cancellationToken);

    public Task DeleteAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken) =>
        _mongoCollection.DeleteManyAsync(predicate, cancellationToken: cancellationToken);

    public Task<Maybe<TAggregateRoot>> FindAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Maybe<TAggregateRoot>> FindFirstAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<TAggregateRoot>> ListAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<TAggregateRoot>> ListAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IQueryable<TAggregateRoot> Query()
    {
        throw new NotImplementedException();
    }

    public IQueryable<TAggregateRoot> Query(Expression<Func<TAggregateRoot, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedList<TAggregateRoot>> ReadAllAsync(int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedList<TAggregateRoot>> ReadAsync(Expression<Func<TAggregateRoot, bool>> predicate, int offset, int limit, Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> sortFunc, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<TAggregateRoot, bool>> predicate, Expression<Func<TAggregateRoot, TResult>> selector, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(TAggregateRoot entity, CancellationToken cancellationToken) =>
        _mongoCollection.ReplaceOneAsync(_transaction.Session, x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
}