namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class LongValueObject : SimpleValueObject<long>, IComparable, IComparable<SimpleValueObject<long>>
{
    protected LongValueObject(long value) : base(value)
    {
    }

    public static implicit operator long(LongValueObject valueObject) => valueObject.Value;

    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public int CompareTo(SimpleValueObject<long>? other) => Value.CompareTo(other?.Value);
}