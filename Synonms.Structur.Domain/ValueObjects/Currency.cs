using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum CurrencyEnumeration
{
    Unknown = 0,
    GBP
}

public class Currency : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<CurrencyEnumeration>().ToList();
    
    private Currency(string value) : base(value)
    {
    }
    
    public static Currency From(CurrencyEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<Currency, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<Currency>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new Currency(matchingAcceptableValue);
            });

    public static OneOf<Maybe<Currency>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Currency>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    internal static Currency Convert(string value) =>
        CreateMandatory(nameof(Currency), value).Match(
            valueObject => valueObject,
            _ => From(CurrencyEnumeration.Unknown));
}