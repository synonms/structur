using System.ComponentModel;
using Synonms.Structur.Core.System;

namespace Synonms.Structur.Domain.Entities;

[TypeDescriptionProvider(typeof(EntityIdTypeDescriptionProvider))]
public record EntityId<TEntity>(Guid Value) : IComparable, IComparable<EntityId<TEntity>>
    where TEntity : Entity<TEntity>
{
    private EntityId() : this(Guid.Empty)
    {
    }

    public static explicit operator EntityId<TEntity>(Guid id) => new(id);
    public static explicit operator Guid(EntityId<TEntity> id) => id.Value;

    public static explicit operator EntityId<TEntity>?(Guid? id) => id is null ? null : new EntityId<TEntity>(id.Value);
    public static explicit operator Guid?(EntityId<TEntity>? id) => id?.Value;

    public bool IsEmpty => Value.Equals(Guid.Empty);

    public int CompareTo(EntityId<TEntity>? other) => Value.CompareTo(other?.Value);
    
    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public override string ToString() => Value.ToString();
    
    public static EntityId<TEntity> New() =>
        new(Guid.NewGuid().ToComb());

    public static EntityId<TEntity> Parse(string id) => 
        new(Guid.Parse(id));

    public static EntityId<TEntity>? ParseOptional(string? id) => 
        id is null ? null : Parse(id);

    public static bool TryParse(string id, out EntityId<TEntity> entityId)
    {
        if (Guid.TryParse(id, out Guid guid))
        {
            entityId = new EntityId<TEntity>(guid);
            return true;
        }

        entityId = Uninitialised;
        return false;
    }

    public static EntityId<TEntity> Uninitialised => 
        new();
}