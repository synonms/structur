using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum EmailContactTypeEnumeration
{
    Unknown = 0,
    Personal,	
    Company,
    Other
}

public class EmailContactType : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<EmailContactTypeEnumeration>().ToList();

    private EmailContactType(string value) : base(value)
    {
    }

    public static EmailContactType From(EmailContactTypeEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<EmailContactType, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<EmailContactType>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new EmailContactType(matchingAcceptableValue);
            });

    public static OneOf<Maybe<EmailContactType>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<EmailContactType>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static EmailContactType Convert(string value) =>
        CreateMandatory(nameof(EmailContactType), value)
            .Match(
                valueObject => valueObject,
                _ => From(EmailContactTypeEnumeration.Unknown));
}