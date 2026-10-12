using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

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

public class GenderIdentity : ScalarValueObject<GenderIdentity, string>, IScalarValueObject<GenderIdentity, string>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<GenderIdentityEnumeration>().ToList();

    private GenderIdentity(string value)
    {
        Value = value;
    }

    public override string Value { get; }
    
    public static implicit operator string(GenderIdentity valueObject) => valueObject.Value;

    public static GenderIdentity From(GenderIdentityEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<GenderIdentity, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<GenderIdentity>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

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

    public static GenderIdentity Rehydrate(string value) =>
        CreateMandatory(nameof(GenderIdentity), value)
            .Match(
                valueObject => valueObject,
                _ => From(GenderIdentityEnumeration.Unknown));
}