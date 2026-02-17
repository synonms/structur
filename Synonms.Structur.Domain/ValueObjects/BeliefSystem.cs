using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public enum BeliefSystemEnumeration
{
    Unknown = 0,
    None,	
    Buddhist,
    Christian,
    Hindu,
    Jewish,
    Muslim,
    Sikh,
    Other,
    Unspecified
}

public record BeliefSystem : StringValueObject<BeliefSystem>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<BeliefSystemEnumeration>().ToList();

    private BeliefSystem(string value) : base(value)
    {
    }

    public static BeliefSystem From(BeliefSystemEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<BeliefSystem, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<BeliefSystem>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(value, x =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new BeliefSystem(matchingAcceptableValue);
            });

    public static OneOf<Maybe<BeliefSystem>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<BeliefSystem>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static BeliefSystem Convert(string value) =>
        CreateMandatory(nameof(BeliefSystem), value)
            .Match(
                valueObject => valueObject,
                _ => From(BeliefSystemEnumeration.Unknown));
}