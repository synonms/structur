using System.ComponentModel.DataAnnotations.Schema;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public record Currency : StringValueObject<Currency>
{
    private static readonly List<string> AcceptableValues = ["GBP"];
    
    private Currency(string value) : base(value)
    {
    }

    [NotMapped]
    public static readonly Currency None = new("None");
    [NotMapped]
    public static readonly Currency Gbp = new("GBP");

    public static OneOf<Currency, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<Currency>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(value, x =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? value;

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
            _ => new Currency(string.Empty));
}