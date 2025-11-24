using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public static class ValueObject
{
    public static ValueObjectBuilder<TValueObject> CreateBuilder<TValueObject>() where TValueObject : ValueObject<TValueObject> => new();
}

public abstract record ValueObject<TValueObject> : IComparable, IComparable<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    public abstract int CompareTo(object? obj);

    public abstract int CompareTo(TValueObject? other);
}

public abstract record StringValueObject<TValueObject> : ValueObject<TValueObject>
    where TValueObject : StringValueObject<TValueObject>
{
    protected const int DefaultMaxLength = int.MaxValue;

    protected StringValueObject(string value)
    {
        Value = value;
    }

    public string Value { get; protected set; }

    public static implicit operator string(StringValueObject<TValueObject> valueObject) => valueObject.Value;

    public override int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public override int CompareTo(TValueObject? other) => string.Compare(Value, other?.Value ?? string.Empty, StringComparison.Ordinal);
}

public abstract record DateTimeValueObject<TValueObject> : ValueObject<TValueObject>
    where TValueObject : DateTimeValueObject<TValueObject>
{
    protected DateTimeValueObject(DateTime value)
    {
        Value = value;
    }

    public DateTime Value { get; protected set; }

    public static implicit operator DateTime(DateTimeValueObject<TValueObject> valueObject) => valueObject.Value;

    public override int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public override int CompareTo(TValueObject? other) => DateTime.Compare(Value, other?.Value ?? DateTime.MinValue);
}

public abstract record DateOnlyValueObject<TValueObject> : ValueObject<TValueObject>
    where TValueObject : DateOnlyValueObject<TValueObject>
{
    protected DateOnlyValueObject(DateOnly value)
    {
        Value = value;
    }

    public DateOnly Value { get; protected set; }

    public static implicit operator DateOnly(DateOnlyValueObject<TValueObject> valueObject) => valueObject.Value;

    public override int CompareTo(object? obj) => Value.CompareTo(obj);
    
    public override int CompareTo(TValueObject? other) => DateTime.Compare(Value.ToDateTime(TimeOnly.MinValue), other?.Value.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue);
}