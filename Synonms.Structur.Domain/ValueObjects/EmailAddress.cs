using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class EmailAddress : StringValueObject
{
    private EmailAddress(string value) : base(value)
    {
    }

    public static OneOf<EmailAddress, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<EmailAddress>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.EmailAddress)
            .Build(() => new EmailAddress(value));

    public static OneOf<Maybe<EmailAddress>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<EmailAddress>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static EmailAddress Convert(string value) =>
        CreateMandatory(nameof(EmailAddress), value)
            .Match(
                valueObject => valueObject,
                _ => new EmailAddress(string.Empty));
}