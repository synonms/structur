using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public sealed class TelephoneContactData
{
    public string? Type { get; init; }
    public string? Number { get; init; }
    public bool? IsPrimary { get; init; }
    public string? Label { get; init; }
}

public class TelephoneContact : ComplexValueObject<TelephoneContact>, IComplexValueObject<TelephoneContact, TelephoneContactData>, IComparable, IComparable<TelephoneContact>
{
    private TelephoneContact(TelephoneContactType type, TelephoneNumber number, bool isPrimary, Label? label)
    {
        Type = type;
        Number = number;
        IsPrimary = isPrimary;
        Label = label;
    }

    public TelephoneContactType Type { get; private set; }
    
    public TelephoneNumber Number { get; private set; }
    
    public bool IsPrimary { get; private set; }
    
    public Label? Label { get; private set; }

    public static implicit operator string(TelephoneContact telephoneNumber) => telephoneNumber.Number;

    public static OneOf<TelephoneContact, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, TelephoneContactData data)
    {
        if (data is null)
        {
            return new DomainRuleFault[] { DomainValidationFaults.Required(propertyName) };
        }

        return Validator.CreateBuilder<TelephoneContact>()
            .WithFaultIf($"{propertyName}.{nameof(IsPrimary)}", () => data.IsPrimary is null, DomainValidationFaults.RequiredFieldTemplate)
            .WithMandatoryScalarProperty(data.Type ?? string.Empty, x => TelephoneContactType.CreateMandatory($"{propertyName}.{nameof(Type)}", x), out TelephoneContactType typeValueObject)
            .WithMandatoryScalarProperty(data.Number ?? string.Empty, x => TelephoneNumber.CreateMandatory($"{propertyName}.{nameof(Number)}", x), out TelephoneNumber numberValueObject)
            .WithOptionalScalarProperty(data.Label, x => Label.CreateOptional($"{propertyName}.{nameof(Label)}", x), out Label? labelValueObject)
            .Build(() => new TelephoneContact(typeValueObject, numberValueObject, data.IsPrimary.GetValueOrDefault(), labelValueObject));
    }

    public static OneOf<Maybe<TelephoneContact>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, TelephoneContactData? data)
    {
        if (data is null || string.IsNullOrWhiteSpace(data.Type) || string.IsNullOrWhiteSpace(data.Number) || data.IsPrimary is null)
        {
            return Maybe<TelephoneContact>.None;
        }

        return CreateMandatory(propertyName, data).ToMaybe();
    }

    public TelephoneContactData ToData() => new()
    {
        Type = Type.Value, 
        Number = Number.Value, 
        IsPrimary = IsPrimary, 
        Label = Label?.Value
    };

    public static TelephoneContact Rehydrate(TelephoneContactData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new(
            TelephoneContactType.Rehydrate(ComplexValueObjectData.Required(data.Type, nameof(Type))),
            TelephoneNumber.Rehydrate(ComplexValueObjectData.Required(data.Number, nameof(Number))),
            ComplexValueObjectData.Required(data.IsPrimary, nameof(IsPrimary)),
            data.Label is null ? null : Label.Rehydrate(data.Label));
    }

    protected override object?[] EqualityMembers => 
    [
        Type.Value, 
        Number.Value, 
        IsPrimary, 
        Label?.Value
    ];
    
    public int CompareTo(object? obj)
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

    public int CompareTo(TelephoneContact? other) => string.Compare(Number.Value, other?.Number?.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
}