using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class FriendlyId : StringValueObject
{
    private FriendlyId(string value) : base(value)
    {
    }

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

    public static FriendlyId Convert(string value) =>
        CreateMandatory(nameof(FriendlyId), value)
            .Match(
                valueObject => valueObject,
                _ => new FriendlyId(string.Empty));
}
