using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class UkBankAccountNumber : StringValueObject
{
    private UkBankAccountNumber(string value) : base(value)
    {
    }

    public static OneOf<UkBankAccountNumber, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<UkBankAccountNumber>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.UkBankAccountNumber)
            .Build(() => new UkBankAccountNumber(value));

    public static OneOf<Maybe<UkBankAccountNumber>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<UkBankAccountNumber>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static UkBankAccountNumber Convert(string value) =>
        CreateMandatory(nameof(UkBankAccountNumber), value)
            .Match(
                valueObject => valueObject,
                _ => new UkBankAccountNumber(string.Empty));
}


