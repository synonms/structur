using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public record BirthDate : DateOnlyValueObject<BirthDate>
{
    private const int DefaultMinimumAge = 0;
    private const int DefaultMaximumAge = 120;

    private BirthDate(DateOnly value) : base(value)
    {
    }

    public static OneOf<BirthDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value) =>
        CreateMandatory(propertyName, value, DefaultMinimumAge, DefaultMaximumAge);

    public static OneOf<BirthDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value, int minimumAge, int maximumAge) =>
        ValueObject.CreateBuilder<BirthDate>()
            .WithFaultIfValueMoreThan(propertyName, value, DateOnly.FromDateTime(DateTime.Today))
            .WithFaultIfValueLessThan(propertyName, value, DateOnly.FromDateTime(DateTime.Today).AddYears(-maximumAge))
            .WithFaultIfValueMoreThan(propertyName, value, DateOnly.FromDateTime(DateTime.Today).AddYears(-minimumAge))
            .Build(value, x => new BirthDate(x));

    public static OneOf<Maybe<BirthDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value)  =>
        CreateOptional(propertyName, value, DefaultMinimumAge, DefaultMaximumAge);

    public static OneOf<Maybe<BirthDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value, int minimumAge, int maximumAge)
    {
        if (value is null)
        {
            return Maybe<BirthDate>.None;
        }

        return CreateMandatory(propertyName, value.Value, minimumAge, maximumAge).ToMaybe();
    }

    internal static BirthDate Convert(DateOnly value) =>
        CreateMandatory(nameof(BirthDate), value).Match(
            valueObject => valueObject,
            _ => new BirthDate(DateOnly.MinValue));
}
