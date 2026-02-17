using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public record TelephoneContact : ValueObject<TelephoneContact>
{

    private TelephoneContact(TelephoneNumber number, bool isPrimary, Label? label)
    {
        Number = number;
        IsPrimary = isPrimary;
        Label = label;
    }

    public TelephoneNumber Number { get; private set; }
    
    public bool IsPrimary { get; private set; }
    
    public Label? Label { get; private set; }

    public static implicit operator string(TelephoneContact telephoneNumber) => telephoneNumber.Number;

    public static OneOf<TelephoneContact, IEnumerable<DomainRuleFault>>  CreateMandatory(string propertyName, string number, bool isPrimary, string? label) =>
        ValueObject.CreateBuilder<TelephoneContact>()
            .WithMandatoryValueObjectProperty(number, x => TelephoneNumber.CreateMandatory($"{propertyName}.{nameof(Number)}", x), out TelephoneNumber numberValueObject)
            .WithOptionalValueObjectProperty(label, x => Label.CreateOptional($"{propertyName}.{nameof(Label)}", x), out Label? labelValueObject)
            .Build(() => new TelephoneContact(numberValueObject, isPrimary, labelValueObject));

    public static OneOf<Maybe<TelephoneContact>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? number, bool? isPrimary, string? label)
    {
        if (string.IsNullOrWhiteSpace(number) || isPrimary is null)
        {
            return Maybe<TelephoneContact>.None;
        }

        return CreateMandatory(propertyName, number, isPrimary.Value, label).ToMaybe();
    }
    
    public override int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is TelephoneContact other)
        {
            return CompareTo(other);
        }

        return 0;
    }

    public override int CompareTo(TelephoneContact? other) => Number.CompareTo(other?.Number ?? string.Empty);
}