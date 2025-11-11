using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Entities;

public static class Entity
{
    public static EntityBuilder<TEntity> CreateBuilder<TEntity>() where TEntity : Entity<TEntity> => new();
}

public abstract class Entity<TEntity>
    where TEntity : Entity<TEntity>
{
    public event EventHandler OnCreated;
    public event EventHandler OnUpdated;

    protected Entity() : this(EntityId<TEntity>.Uninitialised)
    {
    }

    protected Entity(EntityId<TEntity> id)
    {
        Id = id;
        
        OnCreated?.Invoke(this, EventArgs.Empty);
    }
    
    public EntityId<TEntity> Id { get; protected init; }

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

        if (Id.IsUninitialised || other.Id.IsUninitialised)
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