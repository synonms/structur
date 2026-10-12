using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum SexualOrientationEnumeration
{
    Unknown = 0,
    Heterosexual,	
    Gay,
    Lesbian,
    Bisexual,
    Other,
    Unspecified
}

public class SexualOrientation : ValueObject<SexualOrientation, string>, IValueObject<SexualOrientation, string>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<SexualOrientationEnumeration>().ToList();

    private SexualOrientation(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(SexualOrientation sexualOrientation) => sexualOrientation.Value;

    public static SexualOrientation From(SexualOrientationEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<SexualOrientation, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<SexualOrientation>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new SexualOrientation(matchingAcceptableValue);
            });

    public static OneOf<Maybe<SexualOrientation>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<SexualOrientation>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static SexualOrientation Rehydrate(string value) =>
        CreateMandatory(nameof(SexualOrientation), value)
            .Match(
                valueObject => valueObject,
                _ => From(SexualOrientationEnumeration.Unknown));
}