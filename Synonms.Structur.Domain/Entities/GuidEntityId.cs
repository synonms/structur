using System.ComponentModel;
using Synonms.Structur.Core.System;

namespace Synonms.Structur.Domain.Entities;

[TypeDescriptionProvider(typeof(GuidEntityIdTypeDescriptionProvider))]
public record GuidEntityId<TEntity> : EntityId<TEntity, Guid>
    where TEntity : Entity<TEntity>
{
    public GuidEntityId(Guid key)
    {
        Key = key;
    }

    public static explicit operator GuidEntityId<TEntity>(Guid key) => new(key);
    public static explicit operator Guid(GuidEntityId<TEntity> guidEntityId) => guidEntityId.Key;

    public override Guid Key { get; }
    
    public override bool IsUninitialised => Key.Equals(Guid.Empty);

    public static GuidEntityId<TEntity> From(Guid id) =>
        id == Guid.Empty ? Uninitialised : new GuidEntityId<TEntity>(id);

    public static GuidEntityId<TEntity> New() =>
        new(Guid.NewGuid().ToComb());

    public static GuidEntityId<TEntity> New(Guid id) =>
        id == Guid.Empty ? New() : new GuidEntityId<TEntity>(id);

    public static GuidEntityId<TEntity> Parse(string id) => 
        new(Guid.Parse(id));

    public static GuidEntityId<TEntity>? ParseOptional(string? id) => 
        id is null ? null : Parse(id);

    public static bool TryParse(string id, out GuidEntityId<TEntity> guidEntityId)
    {
        if (Guid.TryParse(id, out Guid guid))
        {
            guidEntityId = new GuidEntityId<TEntity>(guid);
            
            return true;
        }

        guidEntityId = Uninitialised;
        
        return false;
    }

    public new static GuidEntityId<TEntity> Uninitialised => 
        new(Guid.Empty);
}