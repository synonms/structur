using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum IntervalEnumeration
{
    Unknown = 0,
    Second,
    Minute,
    Hour,
    Day,    
    Week,
    Month,
    Quarter,
    Year
}

public class Interval : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<IntervalEnumeration>().Where(x => x != "Unknown").ToList();

    private Interval(string value) : base(value)
    {
    }

    public static Interval From(IntervalEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<Interval, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<Interval>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new Interval(matchingAcceptableValue);
            });

    public static OneOf<Maybe<Interval>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Interval>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static Interval Convert(string value) =>
        CreateMandatory(nameof(Interval), value)
            .Match(
                valueObject => valueObject,
                _ => From(IntervalEnumeration.Unknown));
}

