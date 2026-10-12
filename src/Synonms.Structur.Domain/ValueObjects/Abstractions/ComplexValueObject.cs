namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

/// <summary>
/// Optional equality support for object-shaped values, independent of their data contracts.
/// </summary>
public abstract class ComplexValueObject<TSelf> : IEquatable<TSelf>
    where TSelf : ComplexValueObject<TSelf>
{
    protected abstract object?[] EqualityMembers { get; }

    public bool Equals(TSelf? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (GetType() != other.GetType())
        {
            return false;
        }

        return ComplexValueObjectEquality.Equals(GetType(), other, EqualityMembers, other.EqualityMembers);
    }

    public override bool Equals(object? obj) => obj is TSelf other && Equals(other);

    public override int GetHashCode() => ComplexValueObjectEquality.GetHashCode(GetType(), EqualityMembers);
}
