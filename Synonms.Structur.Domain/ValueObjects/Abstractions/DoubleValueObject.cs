namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class DoubleValueObject : SimpleValueObject<double>, IComparable, IComparable<SimpleValueObject<double>>
{
    protected DoubleValueObject(double value) : base(value)
    {
    }

    public static implicit operator double(DoubleValueObject valueObject) => valueObject.Value;

    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public int CompareTo(SimpleValueObject<double>? other) => Value.CompareTo(other?.Value);
}