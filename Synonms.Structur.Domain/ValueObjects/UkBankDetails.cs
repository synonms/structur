using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class UkBankDetails : ComplexValueObject
{
    private UkBankDetails(Moniker bankName, UkBankSortCode sortCode, UkBankAccountNumber accountNumber, Moniker accountName, UkBuildingSocietyRollNumber? buildingSocietyRollNumber)
    {
        BankName = bankName;
        SortCode = sortCode;
        AccountNumber = accountNumber;
        AccountName = accountName;
        BuildingSocietyRollNumber = buildingSocietyRollNumber;
    }

    public Moniker BankName { get; private set; }

    public UkBankSortCode SortCode { get; private set; }

    public UkBankAccountNumber AccountNumber { get; private set; }

    public Moniker AccountName { get; private set; }

    public UkBuildingSocietyRollNumber? BuildingSocietyRollNumber { get; private set; }

    public static OneOf<UkBankDetails, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string bankName, string sortCode, string accountNumber, string accountName, string? buildingSocietyRollNumber) =>
        Validator.CreateBuilder<UkBankDetails>()
            .WithMandatoryScalarProperty(bankName, x => Moniker.CreateMandatory($"{propertyName}.{nameof(BankName)}", x), out Moniker bankNameValueObject)
            .WithMandatoryScalarProperty(sortCode, x => UkBankSortCode.CreateMandatory($"{propertyName}.{nameof(SortCode)}", x), out UkBankSortCode sortCodeValueObject)
            .WithMandatoryScalarProperty(accountNumber, x => UkBankAccountNumber.CreateMandatory($"{propertyName}.{nameof(AccountNumber)}", x), out UkBankAccountNumber accountNumberValueObject)
            .WithMandatoryScalarProperty(accountName, x => Moniker.CreateMandatory($"{propertyName}.{nameof(AccountName)}", x), out Moniker accountNameValueObject)
            .WithOptionalScalarProperty(buildingSocietyRollNumber, x => UkBuildingSocietyRollNumber.CreateOptional($"{propertyName}.{nameof(BuildingSocietyRollNumber)}", x), out UkBuildingSocietyRollNumber? buildingSocietyRollNumberValueObject)
            .Build(() => new UkBankDetails(bankNameValueObject, sortCodeValueObject, accountNumberValueObject, accountNameValueObject, buildingSocietyRollNumberValueObject));

    public static OneOf<Maybe<UkBankDetails>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? bankName, string? sortCode, string? accountNumber, string? accountName, string? buildingSocietyRollNumber)
    {
        if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(sortCode) || string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(accountName))
        {
            return Maybe<UkBankDetails>.None;
        }

        return CreateMandatory(propertyName, bankName, sortCode, accountNumber, accountName, buildingSocietyRollNumber).ToMaybe();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return BankName;
        yield return SortCode;
        yield return AccountNumber;
        yield return AccountName;
        yield return BuildingSocietyRollNumber;
    }
}
