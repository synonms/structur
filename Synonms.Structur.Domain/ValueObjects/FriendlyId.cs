using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public record FriendlyId : StringValueObject<FriendlyId>
{
    private FriendlyId(string value) : base(value)
    {
    }

    public static implicit operator string(FriendlyId friendlyId) => friendlyId.Value;

    public static FriendlyId New() => new("A123456789"); // TODO: Generate new value

    public static OneOf<FriendlyId, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<FriendlyId, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        ValueObject.CreateBuilder<FriendlyId>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(value, x => new FriendlyId(x));

    public static OneOf<Maybe<FriendlyId>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value) =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<FriendlyId>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<FriendlyId>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }

    public static FriendlyId Convert(string value) =>
        CreateMandatory(nameof(FriendlyId), value)
            .Match(
                valueObject => valueObject,
                _ => new FriendlyId(string.Empty));
}
