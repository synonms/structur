namespace Synonms.Structur.Domain.Entities;

public abstract class AggregateRoot<TAggregateRoot> : Entity<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public AggregateRoot()
    {
        OnUpdated += (sender, args) => EntityTag = EntityTag.New();
    }
    
    public EntityTag EntityTag { get; private set; } = EntityTag.New();
}
