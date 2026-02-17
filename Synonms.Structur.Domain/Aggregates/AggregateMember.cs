using Synonms.Structur.Core.Entities;

namespace Synonms.Structur.Domain.Aggregates;

public abstract class AggregateMember<TAggregateMember> : Entity<TAggregateMember>
    where TAggregateMember : AggregateMember<TAggregateMember>
{
    protected AggregateMember() : this(EntityId<TAggregateMember>.Uninitialised)
    {
    }
    
    protected AggregateMember(EntityId<TAggregateMember> id) : base(id)
    {
    }
}