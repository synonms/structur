namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class DateTimeValueObject : SimpleValueObject<DateTime>, IComparable, IComparable<SimpleValueObject<DateTime>>
{
    protected DateTimeValueObject(DateTime value) : base(value)
    {
    }

    public static implicit operator DateTime(DateTimeValueObject valueObject) => valueObject.Value;

    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public int CompareTo(SimpleValueObject<DateTime>? other) => DateTime.Compare(Value, other?.Value ?? DateTime.MinValue);
}