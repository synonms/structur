namespace Synonms.Structur.Domain.Entities;

public abstract class AggregateRoot<TAggregateRoot> : Entity<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    protected AggregateRoot()
    {
    }
    
    protected AggregateRoot(EntityId<TAggregateRoot> id) : base(id)
    {
    }

    public EntityTag EntityTag { get; private set; } = EntityTag.New();
}
