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

    protected AggregateRoot(EntityId<TAggregateRoot> id, Guid tenantId) : base(id)
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; private set; } = Guid.Empty;
    
    public EntityTag EntityTag { get; private set; } = EntityTag.New();
}
