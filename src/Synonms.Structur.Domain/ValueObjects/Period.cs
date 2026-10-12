using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public sealed class PeriodData
{
    public int? Units { get; init; }
    public string? Interval { get; init; }
}

public class Period : ComplexValueObject<Period>, IComplexValueObject<Period, PeriodData>
{
    private Period(Units units, Interval interval)
    {
        Units = units;
        Interval = interval;
    }

    public Units Units { get; private set; }

    public Interval Interval { get; private set; }

    public static OneOf<Period, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, PeriodData data)
    {
        if (data is null)
        {
            return new DomainRuleFault[] { DomainValidationFaults.Required(propertyName) };
        }

        return Validator.CreateBuilder<Period>()
            .WithFaultIf($"{propertyName}.{nameof(Units)}", () => data.Units is null, DomainValidationFaults.RequiredFieldTemplate)
            .WithMandatoryScalarProperty(data.Units.GetValueOrDefault(), x => Units.CreateMandatory($"{propertyName}.{nameof(Units)}", x, 0, int.MaxValue), out Units unitsValueObject)
            .WithMandatoryScalarProperty(data.Interval ?? string.Empty, x => Interval.CreateMandatory($"{propertyName}.{nameof(Interval)}", x), out Interval intervalValueObject)
            .Build(() => new Period(unitsValueObject, intervalValueObject));
    }

    public static OneOf<Maybe<Period>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, PeriodData? data)
    {
        if (data is null || data.Units is null || string.IsNullOrWhiteSpace(data.Interval))
        {
            return Maybe<Period>.None;
        }

        return CreateMandatory(propertyName, data).ToMaybe();
    }

    public PeriodData ToData() => new()
    {
        Units = Units.Value, 
        Interval = 
            Interval.Value
    };

    public static Period Rehydrate(PeriodData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new(
            Units.Rehydrate(ComplexValueObjectData.Required(data.Units, nameof(Units))),
            Interval.Rehydrate(ComplexValueObjectData.Required(data.Interval, nameof(Interval))));
    }

    protected override object?[] EqualityMembers => 
    [
        Units.Value, 
        Interval.Value
    ];
}
