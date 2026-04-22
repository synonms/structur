using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class Period : ComplexValueObject
{
    private Period(Units units, Interval interval)
    {
        Units = units;
        Interval = interval;
    }

    public Units Units { get; private set; }

    public Interval Interval { get; private set; }

    public static OneOf<Period, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, int units, string interval) =>
        Validator.CreateBuilder<Period>()
            .WithMandatoryScalarProperty(units, x => Units.CreateMandatory($"{propertyName}.{nameof(Units)}", x, 0, int.MaxValue), out Units unitsValueObject)
            .WithMandatoryScalarProperty(interval, x => Interval.CreateMandatory($"{propertyName}.{nameof(Interval)}", x), out Interval intervalValueObject)
            .Build(() => new Period(unitsValueObject, intervalValueObject));

    public static OneOf<Maybe<Period>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, int? units, string? interval)
    {
        if (units is null || string.IsNullOrWhiteSpace(interval))
        {
            return Maybe<Period>.None;
        }

        return CreateMandatory(propertyName, units.Value, interval).ToMaybe();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Units;
        yield return Interval;
    }
}

