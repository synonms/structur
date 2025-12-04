namespace Synonms.Structur.Domain.Entities;

public abstract class AggregateRoot<TAggregateRoot> : Entity<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    protected AggregateRoot() : this(EntityId<TAggregateRoot>.Uninitialised, Guid.Empty)
    {
    }
    
    protected AggregateRoot(EntityId<TAggregateRoot> id) : this(id, Guid.Empty)
    {
    }

    protected AggregateRoot(EntityId<TAggregateRoot> id, Guid tenantId) : base(id)
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; private set; }
    
    public EntityTag EntityTag { get; private set; } = EntityTag.New();
}
