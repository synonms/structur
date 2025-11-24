namespace Synonms.Structur.Domain.Entities;

public abstract class AggregateMember<TAggregateMember> : Entity<TAggregateMember>
    where TAggregateMember : AggregateMember<TAggregateMember>
{
    protected AggregateMember()
    {
    }
    
    protected AggregateMember(EntityId<TAggregateMember> id) : base(id)
    {
    }
}