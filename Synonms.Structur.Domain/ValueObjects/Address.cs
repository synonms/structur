using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class Address : ComplexValueObject
{
    private Address(AddressType type, AddressLine line1, AddressLine? line2, AddressLine? line3, AddressLine? line4, Postcode postcode, Label? label)
    {
        Type = type;
        Line1 = line1;
        Line2 = line2;
        Line3 = line3;
        Line4 = line4;
        Postcode = postcode;
        Label = label;
    }
    
    public AddressType Type { get; private set; }
    
    public AddressLine Line1 { get; private set; }

    public AddressLine? Line2 { get; private set; }

    public AddressLine? Line3 { get; private set; }

    public AddressLine? Line4 { get; private set; }
    
    public Postcode Postcode { get; private set; }
    
    public Label? Label { get; private set; }

    public static OneOf<Address, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string type, string line1, string? line2, string? line3, string? line4, string postcode, string? label) =>
        Validator.CreateBuilder<Address>()
            .WithMandatoryValueObjectProperty(type, x => AddressType.CreateMandatory($"{propertyName}.{nameof(Type)}", x), out AddressType addressTypeValueObject)
            .WithMandatoryValueObjectProperty(line1, x => AddressLine.CreateMandatory($"{propertyName}.{nameof(Line1)}", x), out AddressLine line1ValueObject)
            .WithOptionalValueObjectProperty(line2, x => AddressLine.CreateOptional($"{propertyName}.{nameof(Line2)}", x), out AddressLine? line2ValueObject)
            .WithOptionalValueObjectProperty(line3, x => AddressLine.CreateOptional($"{propertyName}.{nameof(Line3)}", x), out AddressLine? line3ValueObject)
            .WithOptionalValueObjectProperty(line4, x => AddressLine.CreateOptional($"{propertyName}.{nameof(Line4)}", x), out AddressLine? line4ValueObject)
            .WithMandatoryValueObjectProperty(postcode, x => Postcode.CreateMandatory($"{propertyName}.{nameof(Postcode)}", x), out Postcode postcodeValueObject)
            .WithOptionalValueObjectProperty(label, x => Label.CreateOptional($"{propertyName}.{nameof(Label)}", x), out Label? labelValueObject)
            .Build(() => new Address(addressTypeValueObject, line1ValueObject, line2ValueObject, line3ValueObject, line4ValueObject, postcodeValueObject, labelValueObject));

    public static OneOf<Maybe<Address>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? type, string? line1, string? line2, string? line3, string? line4, string? postcode, string? label)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(line1) || string.IsNullOrWhiteSpace(postcode))
        {
            return Maybe<Address>.None;
        }

        return CreateMandatory(propertyName, type, line1, line2, line3, line4, postcode, label).ToMaybe();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Type;
        yield return Line1;
        yield return Line2;
        yield return Line3;
        yield return Line4;
        yield return Postcode;
        yield return Label;
    }
}