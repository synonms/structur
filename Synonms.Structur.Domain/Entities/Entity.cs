using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.Entities;

public static class Entity
{
    public static EntityBuilder<TEntity> CreateBuilder<TEntity>() where TEntity : Entity<TEntity> => new();
}

public abstract class Entity<TEntity>
    where TEntity : Entity<TEntity>
{
    protected Entity()
    {
        Id = EntityId<TEntity>.New(); 
    }

    protected Entity(EntityId<TEntity> id)
    {
        Id = id;
    }
    
    public EntityId<TEntity> Id { get; protected init; }

    public bool IsDeleted { get; private set; }

    public void MarkDeleted()
    {
        IsDeleted = true;
    }
    
    public override bool Equals(object? obj)
    {
        if ((obj is Entity<TEntity> other) is false)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (Id.IsEmpty || other.Id.IsEmpty)
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }

    public static bool operator ==(Entity<TEntity>? left, Entity<TEntity>? right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TEntity>? left, Entity<TEntity>? right) =>
        !(left == right);
}