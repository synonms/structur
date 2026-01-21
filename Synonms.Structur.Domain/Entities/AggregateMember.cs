using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Entities;

public abstract class AggregateMember<TAggregateMember> : Entity<TAggregateMember>
    where TAggregateMember : AggregateMember<TAggregateMember>
{
    protected AggregateMember() : this(EntityId<TAggregateMember>.Uninitialised, UserAction.Empty)
    {
    }
    
    protected AggregateMember(EntityId<TAggregateMember> id, UserAction createdAction) : base(id, createdAction)
    {
    }
}