using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class Salutation : StringValueObject
{
    private Salutation(string value) : base(value)
    {
    }

    public static implicit operator string(Salutation salutation) => salutation.Value;

    public static OneOf<Salutation, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<Salutation, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<Salutation>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(() => new Salutation(value));

    public static OneOf<Maybe<Salutation>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value) =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<Salutation>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Salutation>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }

    public static Salutation Convert(string value) =>
        CreateMandatory(nameof(Salutation), value)
            .Match(
                valueObject => valueObject,
                _ => new Salutation(string.Empty));
}
