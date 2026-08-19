namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class SimpleValueObject<TValue> : IEquatable<SimpleValueObject<TValue>>
{
    protected SimpleValueObject(TValue value)
    {
        Value = value;
    }

    public TValue Value { get; protected set; }
    
    public bool Equals(SimpleValueObject<TValue>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SimpleValueObject<TValue>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TValue>.Default.GetHashCode(Value);
    }
}