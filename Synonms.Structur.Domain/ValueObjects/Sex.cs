using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public enum SexEnumeration
{
    Unknown = 0,
    Male,	
    Female
}

public record Sex : StringValueObject<Sex>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<SexEnumeration>().ToList();

    private Sex(string value) : base(value)
    {
    }

    public static Sex From(SexEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<Sex, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<Sex>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(value, x =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new Sex(matchingAcceptableValue);
            });

    public static OneOf<Maybe<Sex>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Sex>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static Sex Convert(string value) =>
        CreateMandatory(nameof(Sex), value)
            .Match(
                valueObject => valueObject,
                _ => From(SexEnumeration.Unknown));
}