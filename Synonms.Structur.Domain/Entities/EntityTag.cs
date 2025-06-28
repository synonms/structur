namespace Synonms.Structur.Domain.Entities;

public record EntityTag(Guid Value)
{
    private EntityTag() : this(Guid.Empty)
    {
    }
    
    public static implicit operator EntityTag(Guid tag) => new(tag);
    public static implicit operator Guid(EntityTag tag) => tag.Value;

    public bool IsEmpty => Value.Equals(Guid.Empty);

    public int CompareTo(EntityTag? other) => Value.CompareTo(other?.Value);
    
    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public override string ToString() => Value.ToString();
    
    public static EntityTag New() =>
        new(Guid.NewGuid());

    public static EntityTag Parse(string tag) => 
        new(Guid.Parse(tag));

    public static EntityTag? ParseOptional(string? tag) => 
        tag is null ? null : Parse(tag);

    public static bool TryParse(string tag, out EntityTag entityTag)
    {
        if (Guid.TryParse(tag, out Guid guid))
        {
            entityTag = new EntityTag(guid);
            return true;
        }

        entityTag = Uninitialised;
        return false;
    }

    public static EntityTag Uninitialised => 
        new();
}