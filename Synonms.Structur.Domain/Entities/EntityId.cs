namespace Synonms.Structur.Domain.Entities;

public abstract record EntityId<TEntity>
    where TEntity : Entity<TEntity>
{
    public abstract bool IsUninitialised { get; }

    public static EntityId<TEntity> Uninitialised => null!;
}

public abstract record EntityId<TEntity, TKey> : EntityId<TEntity>, IComparable, IComparable<EntityId<TEntity, TKey>>
    where TEntity : Entity<TEntity>
    where TKey : IComparable, IComparable<TKey>, IEquatable<TKey>
{
    public abstract TKey Key { get; }
    
    public int CompareTo(EntityId<TEntity, TKey>? other) => 
        other is null ? 1 : Key.CompareTo(other.Key);

    public int CompareTo(object? obj) => 
        obj is null ? 1 : Key.CompareTo(obj);
    
    public override string ToString() => 
        Key?.ToString() ?? string.Empty;
}