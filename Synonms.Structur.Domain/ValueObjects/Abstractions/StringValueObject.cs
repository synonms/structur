namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class StringValueObject : SimpleValueObject<string>, IComparable, IComparable<SimpleValueObject<string>>
{
    protected const int DefaultMaxLength = int.MaxValue;

    protected StringValueObject(string value) : base(value)
    {
    }
    
    public static implicit operator string(StringValueObject valueObject) => valueObject.Value;

    public int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public int CompareTo(SimpleValueObject<string>? other) => string.Compare(Value, other?.Value ?? string.Empty, StringComparison.Ordinal);
}