namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

public abstract class ComplexValueObject : IEquatable<ComplexValueObject>
{
    public static bool operator ==(ComplexValueObject? a, ComplexValueObject? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(ComplexValueObject? a, ComplexValueObject? b) =>
        !(a == b);

    public virtual bool Equals(ComplexValueObject? other) =>
        other is not null && ValuesAreEqual(other);

    public override bool Equals(object? obj) =>
        obj is ComplexValueObject valueObject && ValuesAreEqual(valueObject);

    public override int GetHashCode() =>
        GetAtomicValues().Aggregate(0, (hashcode, value) => HashCode.Combine(hashcode, value.GetHashCode()));

    protected abstract IEnumerable<object?> GetAtomicValues();

    private bool ValuesAreEqual(ComplexValueObject valueObject) =>
        GetAtomicValues().SequenceEqual(valueObject.GetAtomicValues());
}