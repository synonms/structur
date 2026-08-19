using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class UkBankSortCode : StringValueObject
{
    private UkBankSortCode(string value) : base(value)
    {
    }

    public static OneOf<UkBankSortCode, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<UkBankSortCode>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.UkBankSortCode)
            .Build(() => new UkBankSortCode(value));

    public static OneOf<Maybe<UkBankSortCode>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<UkBankSortCode>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static UkBankSortCode Convert(string value) =>
        CreateMandatory(nameof(UkBankSortCode), value)
            .Match(
                valueObject => valueObject,
                _ => new UkBankSortCode(string.Empty));
}



