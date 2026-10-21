using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Transactions;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Infrastructure.MongoDb.Transactions;

namespace Synonms.Structur.Infrastructure.MongoDb.Aggregates;

public class MongoDbWriteAggregateRepository<TAggregateRoot> : IWriteAggregateRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IMongoCollection<BsonDocument> _mongoCollection;
    private readonly MongoDomainTransaction? _transaction;
    private readonly MongoDbAggregateRecordMapper<TAggregateRoot> _recordMapper;
    
    public MongoDbWriteAggregateRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration, IDomainTransaction domainTransaction)
    {
        if (domainTransaction is MongoDomainTransaction mongoDomainTransaction)
        {
            _transaction = mongoDomainTransaction;
        }

        _recordMapper = new MongoDbAggregateRecordMapper<TAggregateRoot>(mongoDatabaseConfiguration);
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<BsonDocument>(mongoDatabaseConfiguration.GetCollectionName<TAggregateRoot>());
    }
    
    public Task AddAsync(TAggregateRoot entity, CancellationToken cancellationToken)
    {
        BsonDocument recordDocument = _recordMapper.Capture(entity);
        return _transaction is null 
            ? _mongoCollection.InsertOneAsync(recordDocument, cancellationToken: cancellationToken) 
            : _mongoCollection.InsertOneAsync(_transaction.Session, recordDocument, cancellationToken: cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<TAggregateRoot> entities, CancellationToken cancellationToken)
    {
        List<BsonDocument> recordDocuments = entities.Select(_recordMapper.Capture).ToList();
        return _transaction is null 
            ? _mongoCollection.InsertManyAsync(recordDocuments, cancellationToken: cancellationToken) 
            : _mongoCollection.InsertManyAsync(_transaction.Session, recordDocuments, cancellationToken: cancellationToken);
    }

    public Task DeleteAsync(TAggregateRoot entity, CancellationToken cancellationToken) =>
        DeleteAsync(entity.Id, cancellationToken);

    public Task DeleteAsync(EntityId<TAggregateRoot> id, CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(id.Value, GuidRepresentation.Standard));
        return _transaction is null 
            ? _mongoCollection.DeleteOneAsync(filter, cancellationToken: cancellationToken)
            : _mongoCollection.DeleteOneAsync(_transaction.Session, filter, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken)
    {
        Func<TAggregateRoot, bool> compiledPredicate = predicate.Compile();
        List<BsonDocument> documents = await _mongoCollection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(cancellationToken);
        List<Guid> ids = documents
            .Select(_recordMapper.Restore)
            .Where(compiledPredicate)
            .Select(x => x.Id.Value)
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.In("_id", ids.Select(id => new BsonBinaryData(id, GuidRepresentation.Standard)));
        if (_transaction is null)
        {
            await _mongoCollection.DeleteManyAsync(filter, cancellationToken: cancellationToken);
        }
        else
        {
            await _mongoCollection.DeleteManyAsync(_transaction.Session, filter, cancellationToken: cancellationToken);
        }
    }

    public Task UpdateAsync(TAggregateRoot entity, CancellationToken cancellationToken)
    {
        BsonDocument recordDocument = _recordMapper.Capture(entity);
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(entity.Id.Value, GuidRepresentation.Standard));
        return _transaction is null 
            ? _mongoCollection.ReplaceOneAsync(filter, recordDocument, cancellationToken: cancellationToken)
            : _mongoCollection.ReplaceOneAsync(_transaction.Session, filter, recordDocument, cancellationToken: cancellationToken);
    }
}