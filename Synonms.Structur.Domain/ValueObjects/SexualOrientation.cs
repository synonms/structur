using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

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

public record SexualOrientation : StringValueObject<SexualOrientation>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<SexualOrientationEnumeration>().ToList();

    private SexualOrientation(string value) : base(value)
    {
    }

    public static SexualOrientation From(SexualOrientationEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<SexualOrientation, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<SexualOrientation>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(value, x =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? value;

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

    public static SexualOrientation Convert(string value) =>
        CreateMandatory(nameof(SexualOrientation), value)
            .Match(
                valueObject => valueObject,
                _ => From(SexualOrientationEnumeration.Unknown));
}