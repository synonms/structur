namespace Synonms.Structur.Domain.Transactions;

public class NoOpDomainTransaction : IDomainTransaction
{
    public void Dispose()
    {
    }

    public Task CommitChangesAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task RollbackChangesAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}