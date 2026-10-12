using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class UkBankSortCode : ScalarValueObject<UkBankSortCode, string>, IScalarValueObject<UkBankSortCode, string>
{
    private UkBankSortCode(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(UkBankSortCode ukBankSortCode) => ukBankSortCode.Value;

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

    public static UkBankSortCode Rehydrate(string value) =>
        CreateMandatory(nameof(UkBankSortCode), value)
            .Match(
                valueObject => valueObject,
                _ => new UkBankSortCode(string.Empty));
}



