using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public sealed class EmailContactData
{
    public string? Type { get; init; }
    public string? Address { get; init; }
    public bool? IsPrimary { get; init; }
    public string? Label { get; init; }
}

public class EmailContact : ComplexValueObject<EmailContact>, IComplexValueObject<EmailContact, EmailContactData>, IComparable, IComparable<EmailContact>
{
    private EmailContact(EmailContactType type, EmailAddress address, bool isPrimary, Label? label)
    {
        Type = type;
        Address = address;
        IsPrimary = isPrimary;
        Label = label;
    }

    public EmailContactType Type { get; private set; }

    public EmailAddress Address { get; private set; }

    public bool IsPrimary { get; private set; }
    
    public Label? Label { get; private set; }

    public static implicit operator string(EmailContact emailAddress) => emailAddress.Address;

    public static OneOf<EmailContact, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, EmailContactData data)
    {
        if (data is null)
        {
            return new DomainRuleFault[] { DomainValidationFaults.Required(propertyName) };
        }

        return Validator.CreateBuilder<EmailContact>()
            .WithFaultIf($"{propertyName}.{nameof(IsPrimary)}", () => data.IsPrimary is null, DomainValidationFaults.RequiredFieldTemplate)
            .WithMandatoryScalarProperty(data.Type ?? string.Empty, x => EmailContactType.CreateMandatory($"{propertyName}.{nameof(Type)}", x), out EmailContactType typeValueObject)
            .WithMandatoryScalarProperty(data.Address ?? string.Empty, x => EmailAddress.CreateMandatory($"{propertyName}.{nameof(Address)}", x), out EmailAddress emailAddressValueObject)
            .WithOptionalScalarProperty(data.Label, x => Label.CreateOptional($"{propertyName}.{nameof(Label)}", x), out Label? labelValueObject)
            .Build(() => new EmailContact(typeValueObject, emailAddressValueObject, data.IsPrimary.GetValueOrDefault(), labelValueObject));
    }

    public static OneOf<Maybe<EmailContact>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, EmailContactData? data)
    {
        if (data is null || string.IsNullOrWhiteSpace(data.Type) || string.IsNullOrWhiteSpace(data.Address) || data.IsPrimary is null)
        {
            return Maybe<EmailContact>.None;
        }

        return CreateMandatory(propertyName, data).ToMaybe();
    }

    public EmailContactData ToData() => new()
    {
        Type = Type.Value, 
        Address = Address.Value, 
        IsPrimary = IsPrimary, 
        Label = Label?.Value
    };

    public static EmailContact Rehydrate(EmailContactData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new(
            EmailContactType.Rehydrate(ComplexValueObjectData.Required(data.Type, nameof(Type))),
            EmailAddress.Rehydrate(ComplexValueObjectData.Required(data.Address, nameof(Address))),
            ComplexValueObjectData.Required(data.IsPrimary, nameof(IsPrimary)),
            data.Label is null ? null : Label.Rehydrate(data.Label));
    }

    protected override object?[] EqualityMembers => 
    [
        Type.Value, 
        Address.Value, 
        IsPrimary, 
        Label?.Value
    ];
    
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is EmailContact other)
        {
            return CompareTo(other);
        }

        return 0;
    }

    public int CompareTo(EmailContact? other) => string.Compare(Address.Value, other?.Address?.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
}