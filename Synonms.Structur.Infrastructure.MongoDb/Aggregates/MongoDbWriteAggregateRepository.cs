using System.Linq.Expressions;
using MongoDB.Driver;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Transactions;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Infrastructure.MongoDb.Transactions;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

public class MongoDbWriteAggregateRepository<TAggregateRoot> : IWriteAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IMongoCollection<TAggregateRoot> _mongoCollection;
    private readonly MongoDomainTransaction? _transaction;
    
    public MongoDbWriteAggregateRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration, IDomainTransaction domainTransaction)
    {
        if (domainTransaction is MongoDomainTransaction mongoDomainTransaction)
        {
            _transaction = mongoDomainTransaction;
        }
        
        if (mongoDatabaseConfiguration.CollectionNamesByAggregateType.ContainsKey(typeof(TAggregateRoot)) is false)
        {
            throw new InvalidOperationException($"Mongo collection name for type {typeof(TAggregateRoot).Name} is not configured.");
        }

        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<TAggregateRoot>(mongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(TAggregateRoot)]);
    }
    
    public Task AddAsync(TAggregateRoot entity, CancellationToken cancellationToken) =>
        _transaction is null 
            ? _mongoCollection.InsertOneAsync(entity, cancellationToken: cancellationToken) 
            : _mongoCollection.InsertOneAsync(_transaction.Session, entity, cancellationToken: cancellationToken);

    public Task AddRangeAsync(IEnumerable<TAggregateRoot> entities, CancellationToken cancellationToken) =>
        _transaction is null 
            ? _mongoCollection.InsertManyAsync(entities, cancellationToken: cancellationToken) 
            : _mongoCollection.InsertManyAsync(_transaction.Session, entities, cancellationToken: cancellationToken);

    public Task DeleteAsync(TAggregateRoot entity, CancellationToken cancellationToken) =>
        _transaction is null 
            ? _mongoCollection.DeleteOneAsync(x => x.Id == entity.Id, cancellationToken: cancellationToken)
            : _mongoCollection.DeleteOneAsync(_transaction.Session, x => x.Id == entity.Id, cancellationToken: cancellationToken);

    public Task DeleteAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken) =>
        _transaction is null 
            ? _mongoCollection.DeleteOneAsync(x => x.Id == id, cancellationToken: cancellationToken)
            : _mongoCollection.DeleteOneAsync(_transaction.Session, x => x.Id == id, cancellationToken: cancellationToken);

    public Task DeleteAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken) =>
        _transaction is null 
            ? _mongoCollection.DeleteManyAsync(predicate, cancellationToken: cancellationToken)
            : _mongoCollection.DeleteManyAsync(_transaction.Session, predicate, cancellationToken: cancellationToken);

    public Task UpdateAsync(TAggregateRoot entity, CancellationToken cancellationToken) =>
        _transaction is null 
            ? _mongoCollection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken)
            : _mongoCollection.ReplaceOneAsync(_transaction.Session, x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
}