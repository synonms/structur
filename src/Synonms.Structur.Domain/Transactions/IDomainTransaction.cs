namespace Synonms.Structur.Domain.Transactions;

public interface IDomainTransaction : IDisposable
{
    Task CommitChangesAsync(CancellationToken cancellationToken);

    Task RollbackChangesAsync(CancellationToken cancellationToken);
}