using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Infrastructure.Persistence;

public abstract class AggregateMemberRecord<TAggregateMember>
    where TAggregateMember : AggregateMember<TAggregateMember>
{
    public Guid Id { get; set; } = Guid.Empty;
}