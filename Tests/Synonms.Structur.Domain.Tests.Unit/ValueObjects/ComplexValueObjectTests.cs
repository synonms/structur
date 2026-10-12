using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.Tests.Unit.ValueObjects;

public class ComplexValueObjectTests
{
    [Fact]
    public void Equality_UsesSameMembersThroughTypedObjectAndInterfaceCalls()
    {
        TestValue value = new("name", [1, 2], null);
        TestValue equal = new("name", [1, 2], null);

        Assert.True(value.Equals(equal));
        Assert.True(value.Equals((object)equal));
        Assert.True(((IEquatable<TestValue>)value).Equals(equal));
        Assert.Equal(value.GetHashCode(), equal.GetHashCode());
        Assert.False(value.Equals(new TestValue("other", [1, 2], null)));
        Assert.False(value.Equals(new TestValue("name", [2, 1], null)));
        Assert.False(value.Equals(new TestValue("name", [1, 2, 3], null)));
        Assert.False(value.Equals(new TestValue("name", [1, 2], "label")));
    }

    [Fact]
    public void Equality_RejectsNullUnrelatedAndDifferentRuntimeTypes()
    {
        TestValue value = new("name", [1, 2], null);
        TestValue derived = new DerivedValue("name", [1, 2], null);

        Assert.False(value.Equals((TestValue?)null));
        Assert.False(value.Equals((object?)null));
        Assert.False(value.Equals("name"));
        Assert.False(value.Equals(derived));
        Assert.False(derived.Equals(value));
        Assert.True(derived.Equals(new DerivedValue("name", [1, 2], null)));
    }

    [Fact]
    public void Equality_ReferenceComparisonDoesNotReadMembers()
    {
        UnreadableValue value = new();

        Assert.True(value.Equals(value));
        Assert.True(value.Equals((object)value));
        Assert.False(value.Equals((UnreadableValue?)null));
        Assert.False(value.Equals(new object()));
    }

    private class TestValue(string name, int[] numbers, string? label) : ComplexValueObject<TestValue>
    {
        protected override object?[] EqualityMembers => [name, numbers, label];
    }

    private sealed class DerivedValue(string name, int[] numbers, string? label) : TestValue(name, numbers, label);

    private sealed class UnreadableValue : ComplexValueObject<UnreadableValue>
    {
        protected override object?[] EqualityMembers => throw new InvalidOperationException("Members should not be read.");
    }
}
