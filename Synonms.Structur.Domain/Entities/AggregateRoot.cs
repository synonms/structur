using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Entities;

public abstract class AggregateRoot<TAggregateRoot> : Entity<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    protected AggregateRoot() : this(EntityId<TAggregateRoot>.Uninitialised, Guid.Empty, UserAction.Empty)
    {
    }
    
    protected AggregateRoot(EntityId<TAggregateRoot> id, UserAction createdAction) : this(id, Guid.Empty, createdAction)
    {
    }

    protected AggregateRoot(EntityId<TAggregateRoot> id, Guid tenantId, UserAction createdAction) : base(id, createdAction)
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; private set; }
    
    public EntityTag EntityTag { get; private set; } = EntityTag.New();
    
    protected override void MarkAsUpdated(UserAction updatedAction)
    {
        base.MarkAsUpdated(updatedAction);
        
        EntityTag = EntityTag.New();
    }
}
