using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public class EmailAddressDto
{
    [StructurRequired]
    [StructurPattern(EmailAddress.AddressRegex)]
    public required string Address { get; init; }
    public required bool IsPrimary { get; init; }
    public string? Label { get; init; }
}

public record EmailAddress : ValueObject<EmailAddress>
{
    public const string AddressRegex = "^(?!\\.)(?!.*\\.\\.)([a-zA-Z0-9_'+\\-\\.]*)[a-zA-Z0-9_+-]@([a-zA-Z0-9][a-zA-Z0-9\\-]*\\.)+[a-zA-Z]{2,}$";

    private EmailAddress(string address, bool isPrimary, string? label)
    {
        Address = address;
        IsPrimary = isPrimary;
        Label = label;
    }
    
    public string Address { get; private set; }

    public bool IsPrimary { get; private set; }
    
    public string? Label { get; private set; }

    public static implicit operator string(EmailAddress emailAddress) => emailAddress.Address;

    public static OneOf<EmailAddress, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, EmailAddressDto dto) =>
        ValueObject.CreateBuilder<EmailAddress>()
            .WithFaultIfNotPopulated($"{propertyName}.{nameof(Address)}", dto.Address)
            .WithFaultIfNotMatchingPattern($"{propertyName}.{nameof(Address)}", dto.Address, AddressRegex)
            .Build(dto, x => new EmailAddress(dto.Address, dto.IsPrimary, dto.Label));

    public static OneOf<Maybe<EmailAddress>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, EmailAddressDto? dto)
    {
        if (dto is null)
        {
            return Maybe<EmailAddress>.None;
        }

        return CreateMandatory(propertyName, dto).ToMaybe();
    }
    
    public override int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is EmailAddress other)
        {
            return CompareTo(other);
        }

        return 0;
    }

    public override int CompareTo(EmailAddress? other) => Address.CompareTo(other?.Address ?? string.Empty);
}