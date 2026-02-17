using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Domain.ValueObjects;

public record EmailAddress : StringValueObject<EmailAddress>
{
    private EmailAddress(string value) : base(value)
    {
    }

    public static OneOf<EmailAddress, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<EmailAddress>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.EmailAddress)
            .Build(value, x => new EmailAddress(x));

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