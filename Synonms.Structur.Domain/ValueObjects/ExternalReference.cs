using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public record ExternalReference : StringValueObject<ExternalReference>
{
    private ExternalReference(string value) : base(value)
    {
    }

    public static implicit operator string(ExternalReference externalReference) => externalReference.Value;

    public static OneOf<ExternalReference, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<ExternalReference, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        ValueObject.CreateBuilder<ExternalReference>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(value, x => new ExternalReference(x));

    public static OneOf<Maybe<ExternalReference>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)  =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<ExternalReference>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<ExternalReference>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }

    public static ExternalReference Convert(string value) =>
        CreateMandatory(nameof(ExternalReference), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new ExternalReference(string.Empty));
}