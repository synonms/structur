using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public sealed class UkBankDetailsData
{
    public string? BankName { get; init; }
    public string? SortCode { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountName { get; init; }
    public string? BuildingSocietyRollNumber { get; init; }
}

public class UkBankDetails : ComplexValueObject<UkBankDetails>, IComplexValueObject<UkBankDetails, UkBankDetailsData>
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

    public static OneOf<UkBankDetails, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, UkBankDetailsData data)
    {
        if (data is null)
        {
            return new DomainRuleFault[] { DomainValidationFaults.Required(propertyName) };
        }

        return Validator.CreateBuilder<UkBankDetails>()
            .WithMandatoryScalarProperty(data.BankName ?? string.Empty, x => Moniker.CreateMandatory($"{propertyName}.{nameof(BankName)}", x), out Moniker bankNameValueObject)
            .WithMandatoryScalarProperty(data.SortCode ?? string.Empty, x => UkBankSortCode.CreateMandatory($"{propertyName}.{nameof(SortCode)}", x), out UkBankSortCode sortCodeValueObject)
            .WithMandatoryScalarProperty(data.AccountNumber ?? string.Empty, x => UkBankAccountNumber.CreateMandatory($"{propertyName}.{nameof(AccountNumber)}", x), out UkBankAccountNumber accountNumberValueObject)
            .WithMandatoryScalarProperty(data.AccountName ?? string.Empty, x => Moniker.CreateMandatory($"{propertyName}.{nameof(AccountName)}", x), out Moniker accountNameValueObject)
            .WithOptionalScalarProperty(data.BuildingSocietyRollNumber, x => UkBuildingSocietyRollNumber.CreateOptional($"{propertyName}.{nameof(BuildingSocietyRollNumber)}", x), out UkBuildingSocietyRollNumber? buildingSocietyRollNumberValueObject)
            .Build(() => new UkBankDetails(bankNameValueObject, sortCodeValueObject, accountNumberValueObject, accountNameValueObject, buildingSocietyRollNumberValueObject));
    }

    public static OneOf<Maybe<UkBankDetails>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, UkBankDetailsData? data)
    {
        if (data is null || string.IsNullOrWhiteSpace(data.BankName) || string.IsNullOrWhiteSpace(data.SortCode) || string.IsNullOrWhiteSpace(data.AccountNumber) || string.IsNullOrWhiteSpace(data.AccountName))
        {
            return Maybe<UkBankDetails>.None;
        }

        return CreateMandatory(propertyName, data).ToMaybe();
    }

    public UkBankDetailsData ToData() => new()
    {
        BankName = BankName.Value, 
        SortCode = SortCode.Value, 
        AccountNumber = AccountNumber.Value,
        AccountName = AccountName.Value, 
        BuildingSocietyRollNumber = BuildingSocietyRollNumber?.Value
    };

    public static UkBankDetails Rehydrate(UkBankDetailsData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new(
            Moniker.Rehydrate(ComplexValueObjectData.Required(data.BankName, nameof(BankName))),
            UkBankSortCode.Rehydrate(ComplexValueObjectData.Required(data.SortCode, nameof(SortCode))),
            UkBankAccountNumber.Rehydrate(ComplexValueObjectData.Required(data.AccountNumber, nameof(AccountNumber))),
            Moniker.Rehydrate(ComplexValueObjectData.Required(data.AccountName, nameof(AccountName))),
            data.BuildingSocietyRollNumber is null ? null : UkBuildingSocietyRollNumber.Rehydrate(data.BuildingSocietyRollNumber));
    }

    protected override object?[] EqualityMembers => 
    [
        BankName.Value, 
        SortCode.Value, 
        AccountNumber.Value, 
        AccountName.Value, 
        BuildingSocietyRollNumber?.Value
    ];
}
