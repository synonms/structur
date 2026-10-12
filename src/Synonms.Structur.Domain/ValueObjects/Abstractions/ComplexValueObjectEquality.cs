using System.Collections;

namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

/// <summary>Member-wise equality with ordered sequence semantics, independent of DTO record equality.</summary>
public static class ComplexValueObjectEquality
{
    public static bool Equals(Type concreteType, object? other, object?[] members, object?[] otherMembers) =>
        other is not null && other.GetType() == concreteType && SequenceEquals(members, otherMembers);

    public static int GetHashCode(Type concreteType, params object?[] members)
    {
        HashCode hash = new();
        hash.Add(concreteType);
        foreach (object? member in members)
        {
            hash.Add(MemberHash(member));
        }
        return hash.ToHashCode();
    }

    private static bool MemberEquals(object? left, object? right) =>
        left is IEnumerable leftSequence && right is IEnumerable rightSequence && left is not string && right is not string
            ? SequenceEquals(leftSequence, rightSequence)
            : object.Equals(left, right);

    private static bool SequenceEquals(IEnumerable left, IEnumerable right)
    {
        IEnumerator leftEnumerator = left.GetEnumerator();
        IEnumerator rightEnumerator = right.GetEnumerator();
        try
        {
            while (leftEnumerator.MoveNext())
            {
                if (!rightEnumerator.MoveNext() || !MemberEquals(leftEnumerator.Current, rightEnumerator.Current))
                {
                    return false;
                }
            }
            return !rightEnumerator.MoveNext();
        }
        finally
        {
            (leftEnumerator as IDisposable)?.Dispose();
            (rightEnumerator as IDisposable)?.Dispose();
        }
    }

    private static int MemberHash(object? member)
    {
        if (member is not IEnumerable sequence || member is string)
        {
            return member?.GetHashCode() ?? 0;
        }
        HashCode hash = new();
        foreach (object? item in sequence)
        {
            hash.Add(MemberHash(item));
        }
        return hash.ToHashCode();
    }
}
