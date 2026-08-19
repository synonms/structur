namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class DateOnlyValueObject : SimpleValueObject<DateOnly>, IComparable, IComparable<SimpleValueObject<DateOnly>>
{
    protected DateOnlyValueObject(DateOnly value) : base(value)
    {
    }

    public static implicit operator DateOnly(DateOnlyValueObject valueObject) => valueObject.Value;

    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public int CompareTo(SimpleValueObject<DateOnly>? other) => DateTime.Compare(Value.ToDateTime(TimeOnly.MinValue), other?.Value.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue);
}