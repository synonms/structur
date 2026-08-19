using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class EmailContact : ComplexValueObject, IComparable, IComparable<EmailContact>
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

    public static OneOf<EmailContact, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string type, string address, bool isPrimary, string? label) =>
        Validator.CreateBuilder<EmailContact>()
            .WithMandatoryScalarProperty(type, x => EmailContactType.CreateMandatory($"{propertyName}.{nameof(Type)}", x), out EmailContactType typeValueObject)
            .WithMandatoryScalarProperty(address, x => EmailAddress.CreateMandatory($"{propertyName}.{nameof(Address)}", x), out EmailAddress emailAddressValueObject)
            .WithOptionalScalarProperty(label, x => Label.CreateOptional($"{propertyName}.{nameof(Label)}", x), out Label? labelValueObject)
            .Build(() => new EmailContact(typeValueObject, emailAddressValueObject, isPrimary, labelValueObject));

    public static OneOf<Maybe<EmailContact>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? type, string? address, bool? isPrimary, string? label)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(address) || isPrimary is null)
        {
            return Maybe<EmailContact>.None;
        }

        return CreateMandatory(propertyName, type, address, isPrimary.Value, label).ToMaybe();
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
        yield return Type;
        yield return Address;
        yield return IsPrimary;
        yield return Label;
    }
}