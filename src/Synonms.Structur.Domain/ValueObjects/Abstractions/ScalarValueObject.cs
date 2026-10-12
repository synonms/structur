namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class ScalarValueObject<TSelf, TValue> : IEquatable<TSelf>
    where TSelf : ScalarValueObject<TSelf, TValue>
{
    public abstract TValue Value { get; }

    bool IEquatable<TSelf>.Equals(TSelf? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;
        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TSelf)obj);
    }

    public override int GetHashCode() =>
        Value is null ? 0 : EqualityComparer<TValue>.Default.GetHashCode(Value);

    public override string ToString() => 
        Value?.ToString() ?? string.Empty;
}