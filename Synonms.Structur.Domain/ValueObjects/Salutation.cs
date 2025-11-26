using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public record Salutation : StringValueObject<Salutation>
{
    private Salutation(string value) : base(value)
    {
    }

    public static implicit operator string(Salutation salutation) => salutation.Value;

    public static OneOf<Salutation, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<Salutation, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        ValueObject.CreateBuilder<Salutation>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(value, x => new Salutation(x));

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
