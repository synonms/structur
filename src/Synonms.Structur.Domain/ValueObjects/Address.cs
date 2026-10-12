using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public sealed class AddressData
{
    public string? Type { get; init; }
    public string? Line1 { get; init; }
    public string? Line2 { get; init; }
    public string? Line3 { get; init; }
    public string? Line4 { get; init; }
    public string? Postcode { get; init; }
    public string? Label { get; init; }
}

public class Address : ComplexValueObject<Address>, IComplexValueObject<Address, AddressData>
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

    public static OneOf<Address, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, AddressData data)
    {
        if (data is null)
        {
            return new DomainRuleFault[] { DomainValidationFaults.Required(propertyName) };
        }

        return Validator.CreateBuilder<Address>()
            .WithMandatoryScalarProperty(data.Type ?? string.Empty, x => AddressType.CreateMandatory($"{propertyName}.{nameof(Type)}", x), out AddressType addressTypeValueObject)
            .WithMandatoryScalarProperty(data.Line1 ?? string.Empty, x => AddressLine.CreateMandatory($"{propertyName}.{nameof(Line1)}", x), out AddressLine line1ValueObject)
            .WithOptionalScalarProperty(data.Line2, x => AddressLine.CreateOptional($"{propertyName}.{nameof(Line2)}", x), out AddressLine? line2ValueObject)
            .WithOptionalScalarProperty(data.Line3, x => AddressLine.CreateOptional($"{propertyName}.{nameof(Line3)}", x), out AddressLine? line3ValueObject)
            .WithOptionalScalarProperty(data.Line4, x => AddressLine.CreateOptional($"{propertyName}.{nameof(Line4)}", x), out AddressLine? line4ValueObject)
            .WithMandatoryScalarProperty(data.Postcode ?? string.Empty, x => Postcode.CreateMandatory($"{propertyName}.{nameof(Postcode)}", x), out Postcode postcodeValueObject)
            .WithOptionalScalarProperty(data.Label, x => Label.CreateOptional($"{propertyName}.{nameof(Label)}", x), out Label? labelValueObject)
            .Build(() => new Address(addressTypeValueObject, line1ValueObject, line2ValueObject, line3ValueObject, line4ValueObject, postcodeValueObject, labelValueObject));
    }

    public static OneOf<Maybe<Address>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, AddressData? data)
    {
        if (data is null || string.IsNullOrWhiteSpace(data.Type) || string.IsNullOrWhiteSpace(data.Line1) || string.IsNullOrWhiteSpace(data.Postcode))
        {
            return Maybe<Address>.None;
        }

        return CreateMandatory(propertyName, data).ToMaybe();
    }

    public AddressData ToData() => new()
    {
        Type = Type.Value, 
        Line1 = Line1.Value, 
        Line2 = Line2?.Value, 
        Line3 = Line3?.Value,
        Line4 = Line4?.Value, 
        Postcode = Postcode.Value, 
        Label = Label?.Value
    };

    public static Address Rehydrate(AddressData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new(
            AddressType.Rehydrate(ComplexValueObjectData.Required(data.Type, nameof(Type))),
            AddressLine.Rehydrate(ComplexValueObjectData.Required(data.Line1, nameof(Line1))),
            data.Line2 is null ? null : AddressLine.Rehydrate(data.Line2),
            data.Line3 is null ? null : AddressLine.Rehydrate(data.Line3),
            data.Line4 is null ? null : AddressLine.Rehydrate(data.Line4),
            Postcode.Rehydrate(ComplexValueObjectData.Required(data.Postcode, nameof(Postcode))),
            data.Label is null ? null : Label.Rehydrate(data.Label));
    }

    protected override object?[] EqualityMembers => 
    [
        Type.Value, 
        Line1.Value, 
        Line2?.Value, 
        Line3?.Value, 
        Line4?.Value, 
        Postcode.Value, 
        Label?.Value
    ];
}