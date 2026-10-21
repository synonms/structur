using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Infrastructure.Persistence;

/// <summary>A trusted, explicit persistence boundary. Neither operation executes command validation.</summary>
/// <remarks>
/// Capture must own its entire mutable graph (including audit records, nested DTOs and lists).
/// Restore must copy mutable state, preserve root/member IDs, tenant, tag and audit exactly, and use
/// existing value-object Rehydrate policies. It must not mark audit, emit events or perform writes.
/// No aggregate, value object, API model or service may be embedded in the record graph.
/// Instances are cached with configuration and must be safe to share across repository scopes.
/// </remarks>
public interface IAggregateRecordMapper<TAggregate, TRecord>
    where TAggregate : AggregateRoot<TAggregate>
    where TRecord : class
{
    TRecord Capture(TAggregate aggregate);

    TAggregate Restore(TRecord record);
}
