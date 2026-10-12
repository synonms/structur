using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

/// <summary>
/// Defines the common contract for a value object that wraps a scalar value.
/// </summary>
/// <typeparam name="TSelf">The concrete value-object type.</typeparam>
/// <typeparam name="TValue">The wrapped scalar type.</typeparam>
/// <remarks>
/// <see cref="CreateMandatory"/> is the validated construction path for untrusted input.
/// <see cref="Rehydrate"/> is a trusted persistence path: it must not invoke current
/// creation rules and each implementation must deterministically preserve or otherwise
/// explicitly handle representable values that fail those rules. Rehydration must not
/// invent domain data or be used as a substitute for command validation.
/// </remarks>
public interface IValueObject<TSelf, TValue>
    where TSelf : IValueObject<TSelf, TValue>
{
    /// <summary>
    /// Gets the wrapped scalar value.
    /// </summary>
    TValue Value { get; }

    static abstract implicit operator TValue(TSelf valueObject);

    /// <summary>
    /// Creates an instance after applying the type's current mandatory-value validation.
    /// </summary>
    static abstract OneOf<TSelf, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, TValue value);

    /// <summary>
    /// Creates an instance after applying the type's current mandatory-value validation if the value is populated, otherwise returns None.
    /// </summary>
    static abstract OneOf<Maybe<TSelf>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, TValue? value);

    /// <summary>
    /// Creates an instance from trusted persisted state without applying current creation validation.
    /// </summary>
    static abstract TSelf Rehydrate(TValue value);
}

public abstract class ValueObject<TSelf, TValue> : IEquatable<TSelf>
    where TSelf : ValueObject<TSelf, TValue>
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
