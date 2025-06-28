namespace Synonms.Structur.Domain.Entities;

public abstract class AuditableEntity<TEntity> : Entity<TEntity>
    where TEntity : Entity<TEntity>
{
    protected AuditableEntity(EntityId<TEntity> id) : base(id)
    {
        OnCreated += (sender, args) => CreatedDate = DateTime.UtcNow;
        OnUpdated += (sender, args) => ModifiedDate = DateTime.UtcNow;
    }
    
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}