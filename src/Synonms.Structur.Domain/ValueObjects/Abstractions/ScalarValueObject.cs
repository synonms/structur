using System.Globalization;

namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class ScalarValueObject<TSelf, TValue> : IEquatable<TSelf>, IFormattable
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
        return ((IEquatable<TSelf>)this).Equals((TSelf)obj);
    }

    public override int GetHashCode() =>
        Value is null ? 0 : EqualityComparer<TValue>.Default.GetHashCode(Value);

    public override string ToString() => 
        ToString(null, CultureInfo.InvariantCulture);

    public virtual string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (Value is null)
        {
            return string.Empty;
        }

        string? effectiveFormat = format ?? DefaultFormat;

        return Value is IFormattable formattable
            ? formattable.ToString(effectiveFormat, formatProvider) ?? string.Empty
            : Value.ToString() ?? string.Empty;
    }

    protected virtual string? DefaultFormat => typeof(TValue) switch
    {
        Type type when type == typeof(DateOnly) => "yyyy-MM-dd",
        Type type when type == typeof(DateTime) => "O",
        Type type when type == typeof(DateTimeOffset) => "O",
        Type type when type == typeof(decimal) => "F2",
        Type type when type == typeof(double) => "R",
        _ => null
    };
}