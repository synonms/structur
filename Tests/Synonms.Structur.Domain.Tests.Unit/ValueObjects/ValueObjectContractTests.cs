using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.Tests.Unit.ValueObjects;

public class ValueObjectContractTests
{
    [Fact]
    public void CreateMandatory_UsesSharedValidatedContract()
    {
        OneOf<Moniker, IEnumerable<DomainRuleFault>> validMoniker = CreateMandatory<Moniker, string>("Name", "Ada");
        OneOf<EmailAddress, IEnumerable<DomainRuleFault>> validEmail = CreateMandatory<EmailAddress, string>("Email", "ada@example.com");
        OneOf<Units, IEnumerable<DomainRuleFault>> validUnits = CreateMandatory<Units, int>("Units", 3);
        OneOf<Moniker, IEnumerable<DomainRuleFault>> invalidMoniker = CreateMandatory<Moniker, string>("Name", " ");
        OneOf<EmailAddress, IEnumerable<DomainRuleFault>> invalidEmail = CreateMandatory<EmailAddress, string>("Email", "not-an-email");
        OneOf<Units, IEnumerable<DomainRuleFault>> invalidUnits = CreateMandatory<Units, int>("Units", -1);

        Assert.True(validMoniker.IsLeft);
        Assert.Equal("Ada", ((Moniker)validMoniker).Value);
        Assert.True(validEmail.IsLeft);
        Assert.Equal("ada@example.com", ((EmailAddress)validEmail).Value);
        Assert.True(validUnits.IsLeft);
        Assert.Equal(3, ((Units)validUnits).Value);
        Assert.True(invalidMoniker.IsRight);
        Assert.NotEmpty(invalidMoniker.RightAsEnumerable().Single());
        Assert.True(invalidEmail.IsRight);
        Assert.NotEmpty(invalidEmail.RightAsEnumerable().Single());
        Assert.True(invalidUnits.IsRight);
        Assert.NotEmpty(invalidUnits.RightAsEnumerable().Single());
    }

    [Fact]
    public void Rehydrate_PreservesRepresentableValuesThatFailCurrentValidation()
    {
        Moniker moniker = Rehydrate<Moniker, string>(" ");
        EmailAddress emailAddress = Rehydrate<EmailAddress, string>("not-an-email");
        Units units = Rehydrate<Units, int>(-1);

        Assert.Equal(" ", moniker.Value);
        Assert.Equal("not-an-email", emailAddress.Value);
        Assert.Equal(-1, units.Value);
        Assert.True(Moniker.CreateMandatory(nameof(Moniker), moniker.Value).IsRight);
        Assert.True(EmailAddress.CreateMandatory(nameof(EmailAddress), emailAddress.Value).IsRight);
        Assert.True(Units.CreateMandatory(nameof(Units), units.Value).IsRight);
    }

    [Fact]
    public void Equality_RequiresSameConcreteTypeAndScalarValue()
    {
        IValueObject<Moniker, string> moniker = Moniker.Rehydrate("same");
        IValueObject<EmailAddress, string> emailAddress = EmailAddress.Rehydrate("same");

        Assert.True(moniker.Equals(Moniker.Rehydrate("same")));
        Assert.False(moniker.Equals(emailAddress));
        Assert.False(emailAddress.Equals(moniker));
        Assert.False(moniker.Equals(Moniker.Rehydrate("different")));
    }

    [Fact]
    public void Value_HasNoPublicSetter()
    {
        global::System.Reflection.PropertyInfo? valueProperty = typeof(IValueObject<Moniker, string>).GetProperty(nameof(IValueObject<,>.Value));

        Assert.NotNull(valueProperty);
        Assert.Null(valueProperty.GetSetMethod());
    }

    private static OneOf<TSelf, IEnumerable<DomainRuleFault>> CreateMandatory<TSelf, TValue>(string propertyName, TValue value)
        where TSelf : IValueObject<TSelf, TValue> =>
        TSelf.CreateMandatory(propertyName, value);

    private static TSelf Rehydrate<TSelf, TValue>(TValue value)
        where TSelf : IValueObject<TSelf, TValue> =>
        TSelf.Rehydrate(value);
}
