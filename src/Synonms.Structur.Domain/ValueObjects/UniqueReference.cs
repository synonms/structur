using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class UniqueReference : StringValueObject
{
    private UniqueReference(string value) : base(value)
    {
    }

    public static implicit operator string(UniqueReference valueObject) => valueObject.Value;

    public static OneOf<UniqueReference, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<UniqueReference, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<UniqueReference>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(() => new UniqueReference(value));

    public static OneOf<Maybe<UniqueReference>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)  =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<UniqueReference>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<UniqueReference>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }

    public static UniqueReference Convert(string value) =>
        CreateMandatory(nameof(UniqueReference), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new UniqueReference(string.Empty));
}