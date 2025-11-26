using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public class TelephoneNumberDto
{
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

    public static OneOf<TelephoneNumber, IEnumerable<DomainRuleFault>>  CreateMandatory(string propertyName, TelephoneNumberDto dto) =>
        ValueObject.CreateBuilder<TelephoneNumber>()
            .WithFaultIfNotPopulated($"{propertyName}.{nameof(Number)}", dto.Number)
            .WithFaultIfNotMatchingPattern($"{propertyName}.{nameof(Number)}", dto.Number, NumberRegex)
            .Build(dto, x => new TelephoneNumber(dto.Number, dto.IsPrimary, dto.Label));

    public static OneOf<Maybe<TelephoneNumber>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, TelephoneNumberDto? dto)
    {
        if (dto is null)
        {
            return Maybe<TelephoneNumber>.None;
        }

        return CreateMandatory(propertyName, dto).ToMaybe();
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