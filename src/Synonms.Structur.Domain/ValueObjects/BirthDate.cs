using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class BirthDate : ValueObject<BirthDate, DateOnly>, IValueObject<BirthDate, DateOnly>
{
    private const int DefaultMinimumAge = 0;
    private const int DefaultMaximumAge = 120;

    private BirthDate(DateOnly value)
    {
        Value = value;
    }

    public override DateOnly Value { get; }

    public static implicit operator DateOnly(BirthDate valueObject) => valueObject.Value;

    public static OneOf<BirthDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value) =>
        CreateMandatory(propertyName, value, DefaultMinimumAge, DefaultMaximumAge);

    public static OneOf<BirthDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value, int minimumAge, int maximumAge) =>
        Validator.CreateBuilder<BirthDate>()
            .WithFaultIfValueMoreThan(propertyName, value, DateOnly.FromDateTime(DateTime.Today))
            .WithFaultIfValueLessThan(propertyName, value, DateOnly.FromDateTime(DateTime.Today).AddYears(-maximumAge))
            .WithFaultIfValueMoreThan(propertyName, value, DateOnly.FromDateTime(DateTime.Today).AddYears(-minimumAge))
            .Build(() => new BirthDate(value));

    public static OneOf<Maybe<BirthDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value) =>
        CreateOptional(propertyName, value, DefaultMinimumAge, DefaultMaximumAge);

    public static OneOf<Maybe<BirthDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly value) =>
        CreateOptional(propertyName, value, DefaultMinimumAge, DefaultMaximumAge);

    public static OneOf<Maybe<BirthDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value, int minimumAge, int maximumAge)
    {
        if (value is null)
        {
            return Maybe<BirthDate>.None;
        }

        return CreateMandatory(propertyName, value.Value, minimumAge, maximumAge).ToMaybe();
    }

    public static BirthDate Rehydrate(DateOnly value) =>
        CreateMandatory(nameof(BirthDate), value).Match(
            valueObject => valueObject,
            _ => new BirthDate(DateOnly.MinValue));
}
