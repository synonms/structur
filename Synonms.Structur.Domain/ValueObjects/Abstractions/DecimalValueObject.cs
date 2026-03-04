namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class DecimalValueObject : SimpleValueObject<decimal>, IComparable, IComparable<SimpleValueObject<decimal>>
{
    protected DecimalValueObject(decimal value) : base(value)
    {
    }

    public static implicit operator decimal(DecimalValueObject valueObject) => valueObject.Value;

    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public int CompareTo(SimpleValueObject<decimal>? other) => Value.CompareTo(other?.Value);
}