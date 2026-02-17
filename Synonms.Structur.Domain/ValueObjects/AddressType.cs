using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public enum AddressTyeEnumeration
{
    Unknown,
    Home, 
    Work, 
    Billing, 
    Shipping, 
    Other
}

public record AddressType : StringValueObject<AddressType>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<AddressTyeEnumeration>().ToList();
    
    private AddressType(string value) : base(value)
    {
    }

    public static AddressType From(AddressTyeEnumeration enumeration) => new(enumeration.ToString());
    
    public static OneOf<AddressType, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<AddressType>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(value, x =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new AddressType(matchingAcceptableValue);
            });

    public static OneOf<Maybe<AddressType>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<AddressType>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static AddressType Convert(string value) =>
        CreateMandatory(nameof(AddressType), value)
            .Match(
                valueObject => valueObject,
                _ => From(AddressTyeEnumeration.Unknown));
}