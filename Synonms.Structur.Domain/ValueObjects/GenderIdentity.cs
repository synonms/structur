using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public enum GenderIdentityEnumeration
{
    Unknown = 0,
    Male,	
    Female,
    NonBinary,
    Intersex,
    Other,
    Unspecified
}

public record GenderIdentity : StringValueObject<GenderIdentity>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<GenderIdentityEnumeration>().ToList();

    private GenderIdentity(string value) : base(value)
    {
    }

    public static GenderIdentity From(GenderIdentityEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<GenderIdentity, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<GenderIdentity>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(value, x =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new GenderIdentity(matchingAcceptableValue);
            });

    public static OneOf<Maybe<GenderIdentity>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<GenderIdentity>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static GenderIdentity Convert(string value) =>
        CreateMandatory(nameof(GenderIdentity), value)
            .Match(
                valueObject => valueObject,
                _ => From(GenderIdentityEnumeration.Unknown));
}