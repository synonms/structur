using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class Moniker : ValueObject<Moniker, string>, IValueObject<Moniker, string>
{
    public const int DefaultMaxLength = 250;
    
    private Moniker(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(Moniker valueObject) => valueObject.Value;

    public static OneOf<Moniker, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<Moniker, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<Moniker>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(() => new Moniker(value));

    public static OneOf<Maybe<Moniker>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)  =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<Moniker>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Moniker>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }

    public static Moniker Rehydrate(string value) =>
        CreateMandatory(nameof(Moniker), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new Moniker(string.Empty));
}