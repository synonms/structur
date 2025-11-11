using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public record Moniker : StringValueObject<Moniker>
{
    private Moniker(string value) : base(value)
    {
    }

    public static OneOf<Moniker, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<Moniker, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        ValueObject.CreateBuilder<Moniker>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(value, x => new Moniker(x));

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

    public static Moniker Convert(string value) =>
        CreateMandatory(nameof(Moniker), value, value.Length)
            .Match(
                moniker => moniker,
                _ => new Moniker(string.Empty));
}