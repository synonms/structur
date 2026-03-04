using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class EffectiveDate : DateOnlyValueObject
{
    private static readonly DateOnly DefaultMinimum = DateOnly.MinValue;
    private static readonly DateOnly DefaultMaximum = DateOnly.MaxValue;

    private EffectiveDate(DateOnly value) : base(value)
    {
    }

    public static OneOf<EffectiveDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value) =>
        CreateMandatory(propertyName, value, DefaultMinimum, DefaultMaximum);

    public static OneOf<EffectiveDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value, DateOnly minimum, DateOnly maximum) =>
        Validator.CreateBuilder<EffectiveDate>()
            .WithFaultIfValueLessThan(propertyName, value, minimum)
            .WithFaultIfValueMoreThan(propertyName, value, maximum)
            .Build(() => new EffectiveDate(value));

    public static OneOf<Maybe<EffectiveDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value)  =>
        CreateOptional(propertyName, value, DefaultMinimum, DefaultMaximum);

    public static OneOf<Maybe<EffectiveDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value, DateOnly minimum, DateOnly maximum)
    {
        if (value is null)
        {
            return Maybe<EffectiveDate>.None;
        }

        return CreateMandatory(propertyName, value.Value, minimum, maximum).ToMaybe();
    }

    internal static EffectiveDate Convert(DateOnly value) =>
        CreateMandatory(nameof(EffectiveDate), value).Match(
            valueObject => valueObject,
            _ => new EffectiveDate(DateOnly.MinValue));
}
