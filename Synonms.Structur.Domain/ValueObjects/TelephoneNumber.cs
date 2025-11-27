using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public class TelephoneNumberResource : ValueObjectResource
{
    [StructurRequired]
    [StructurPattern(TelephoneNumber.NumberRegex)]
    public required string Number { get; init; }
    public required bool IsPrimary { get; init; }
    public string? Label { get; init; }
}

public record TelephoneNumber : ValueObject<TelephoneNumber>
{
    public const string NumberRegex = "^(\\+44|0)\\d{10}$";

    private TelephoneNumber(string number, bool isPrimary, string? label)
    {
        Number = number;
        IsPrimary = isPrimary;
        Label = label;
    }

    public string Number { get; private set; }
    
    public bool IsPrimary { get; private set; }
    
    public string? Label { get; private set; }

    public static implicit operator string(TelephoneNumber telephoneNumber) => telephoneNumber.Number;

    public static OneOf<TelephoneNumber, IEnumerable<DomainRuleFault>>  CreateMandatory(string propertyName, TelephoneNumberResource resource) =>
        ValueObject.CreateBuilder<TelephoneNumber>()
            .WithFaultIfNotPopulated($"{propertyName}.{nameof(Number)}", resource.Number)
            .WithFaultIfNotMatchingPattern($"{propertyName}.{nameof(Number)}", resource.Number, NumberRegex)
            .Build(resource, x => new TelephoneNumber(resource.Number, resource.IsPrimary, resource.Label));

    public static OneOf<Maybe<TelephoneNumber>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, TelephoneNumberResource? resource)
    {
        if (resource is null)
        {
            return Maybe<TelephoneNumber>.None;
        }

        return CreateMandatory(propertyName, resource).ToMaybe();
    }
    
    public override int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is TelephoneNumber other)
        {
            return CompareTo(other);
        }

        return 0;
    }

    public override int CompareTo(TelephoneNumber? other) => Number.CompareTo(other?.Number ?? string.Empty);
}