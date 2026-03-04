namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class IntValueObject : SimpleValueObject<int>, IComparable, IComparable<SimpleValueObject<int>>
{
    protected IntValueObject(int value) : base(value)
    {
    }

    public static implicit operator int(IntValueObject valueObject) => valueObject.Value;

    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public int CompareTo(SimpleValueObject<int>? other) => Value.CompareTo(other?.Value);
}