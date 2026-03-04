using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class EmailContact : ComplexValueObject, IComparable, IComparable<EmailContact>
{
    private EmailContact(EmailAddress address, bool isPrimary, Label? label)
    {
        Address = address;
        IsPrimary = isPrimary;
        Label = label;
    }
    
    public EmailAddress Address { get; private set; }

    public bool IsPrimary { get; private set; }
    
    public Label? Label { get; private set; }

    public static implicit operator string(EmailContact emailAddress) => emailAddress.Address;

    public static OneOf<EmailContact, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string address, bool isPrimary, string? label) =>
        Validator.CreateBuilder<EmailContact>()
            .WithMandatoryValueObjectProperty(address, x => EmailAddress.CreateMandatory($"{propertyName}.{nameof(Address)}", x), out EmailAddress emailAddressValueObject)
            .WithOptionalValueObjectProperty(label, x => Label.CreateOptional($"{propertyName}.{nameof(Label)}", x), out Label? labelValueObject)
            .Build(() => new EmailContact(emailAddressValueObject, isPrimary, labelValueObject));

    public static OneOf<Maybe<EmailContact>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? address, bool? isPrimary, string? label)
    {
        if (string.IsNullOrWhiteSpace(address) || isPrimary is null)
        {
            return Maybe<EmailContact>.None;
        }

        return CreateMandatory(propertyName, address, isPrimary.Value, label).ToMaybe();
    }
    
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

    public int CompareTo(EmailContact? other) => Address.CompareTo(other?.Address ?? string.Empty);
    
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Address;
    }
}