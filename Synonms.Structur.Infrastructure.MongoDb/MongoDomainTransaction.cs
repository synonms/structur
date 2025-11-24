using MongoDB.Driver;
using Synonms.Structur.Domain.Transactions;

namespace Synonms.Structur.Infrastructure.MongoDb;

public class MongoDomainTransaction : IDomainTransaction
{
    private readonly IClientSessionHandle _session;

    public MongoDomainTransaction(IMongoClient mongoClient)
    {
        _session = mongoClient.StartSession();
        _session.StartTransaction();
    }

    public async Task CommitChangesAsync(CancellationToken cancellationToken)
    {
        if (_session.IsInTransaction)
        {
            await _session.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task RollbackChangesAsync(CancellationToken cancellationToken)
    {
        if (_session.IsInTransaction)
        {
            await _session.AbortTransactionAsync(cancellationToken);
        }
    }
    
    public IClientSessionHandle Session => _session;

    public void Dispose()
    {
        _session.Dispose();
    }
}