using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum MaritalStatusEnumeration
{
    Unknown = 0,
    Married,	
    Single,
    Widowed,
    Divorced,
    Separated,
    CivilPartnership,
    Cohabiting
}

public class MaritalStatus : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<MaritalStatusEnumeration>().ToList();

    private MaritalStatus(string value) : base(value)
    {
    }

    public static MaritalStatus From(MaritalStatusEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<MaritalStatus, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<MaritalStatus>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new MaritalStatus(matchingAcceptableValue);
            });

    public static OneOf<Maybe<MaritalStatus>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<MaritalStatus>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static MaritalStatus Convert(string value) =>
        CreateMandatory(nameof(MaritalStatus), value)
            .Match(
                valueObject => valueObject,
                _ => From(MaritalStatusEnumeration.Unknown));
}