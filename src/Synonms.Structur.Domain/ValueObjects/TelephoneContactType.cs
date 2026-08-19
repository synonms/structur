using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum TelephoneContactTypeEnumeration
{
    Unknown = 0,
    HomeLandline,	
    WorkLandline,
    PersonalMobile,
    CompanyMobile,
    Other
}

public class TelephoneContactType : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<TelephoneContactTypeEnumeration>().ToList();

    private TelephoneContactType(string value) : base(value)
    {
    }

    public static TelephoneContactType From(TelephoneContactTypeEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<TelephoneContactType, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<TelephoneContactType>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new TelephoneContactType(matchingAcceptableValue);
            });

    public static OneOf<Maybe<TelephoneContactType>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<TelephoneContactType>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static TelephoneContactType Convert(string value) =>
        CreateMandatory(nameof(TelephoneContactType), value)
            .Match(
                valueObject => valueObject,
                _ => From(TelephoneContactTypeEnumeration.Unknown));
}