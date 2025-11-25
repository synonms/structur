using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Transactions;

namespace Synonms.Structur.Infrastructure.MongoDb;

public class MongoDbDomainEventRepository<TAggregateRoot> : IDomainEventRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IMongoCollection<DomainEvent<TAggregateRoot>> _mongoCollection;
    private readonly MongoDomainTransaction? _transaction;

    public MongoDbDomainEventRepository(IDomainEventDispatcher domainEventDispatcher, IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration, IDomainTransaction domainTransaction)
    {
        if (domainTransaction is MongoDomainTransaction mongoDomainTransaction)
        {
            _transaction = mongoDomainTransaction;
        }
        
        _domainEventDispatcher = domainEventDispatcher;

        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<DomainEvent<TAggregateRoot>>(MongoDbConstants.Database.Collections.DomainEvents);
    }
    
    public async Task<Maybe<Fault>> CreateAsync(DomainEvent<TAggregateRoot> domainEvent, CancellationToken cancellationToken = default)
    {
        Maybe<Fault> dispatcherOutcome = await _domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);

        Maybe<Fault> persistenceOutcome = await dispatcherOutcome.BiBindAsync(async () =>
        {
            if (_transaction is null)
            {
                await _mongoCollection.InsertOneAsync(domainEvent, cancellationToken: cancellationToken);
            }
            else
            {
                await _mongoCollection.InsertOneAsync(_transaction.Session, domainEvent, cancellationToken: cancellationToken);
            }
            
            return Maybe<Fault>.None;
        });

        return persistenceOutcome;
    }

    public async Task<IEnumerable<DomainEvent<TAggregateRoot>>> ReadAllAsync(EntityId<TAggregateRoot> aggregateId, CancellationToken cancellationToken = default) =>
        await _mongoCollection.AsQueryable().Where(x => x.AggregateType == typeof(TAggregateRoot).Name && x.AggregateId == aggregateId).ToListAsync(cancellationToken);
}