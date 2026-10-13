using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class ExternalReference : ScalarValueObject<ExternalReference, string>, IScalarValueObject<ExternalReference, string>
{
    public const int DefaultMaxLength = 250;
    
    private ExternalReference(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(ExternalReference externalReference) => externalReference.Value;

    public static OneOf<ExternalReference, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<ExternalReference, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<ExternalReference>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(() => new ExternalReference(value));

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

    public static ExternalReference Rehydrate(string value) =>
        CreateMandatory(nameof(ExternalReference), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new ExternalReference(value));
}