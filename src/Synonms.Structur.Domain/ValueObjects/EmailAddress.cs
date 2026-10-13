using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class EmailAddress : ScalarValueObject<EmailAddress, string>, IScalarValueObject<EmailAddress, string>
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(EmailAddress valueObject) => valueObject.Value;

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

    public static EmailAddress Rehydrate(string value) => 
        CreateMandatory(nameof(EmailAddress), value)
            .Match(
                valueObject => valueObject,
                _ => new EmailAddress(value));
}