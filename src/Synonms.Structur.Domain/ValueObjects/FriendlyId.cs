using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class FriendlyId : ValueObject<FriendlyId, string>, IValueObject<FriendlyId, string>
{
    public const int DefaultMaxLength = 250;

    private FriendlyId(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(FriendlyId friendlyId) => friendlyId.Value;

    public static FriendlyId New() => new("A123456789"); // TODO: Generate new value

    public static OneOf<FriendlyId, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<FriendlyId, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<FriendlyId>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(() => new FriendlyId(value));

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

    public static FriendlyId Rehydrate(string value) =>
        CreateMandatory(nameof(FriendlyId), value)
            .Match(
                valueObject => valueObject,
                _ => new FriendlyId(string.Empty));
}
