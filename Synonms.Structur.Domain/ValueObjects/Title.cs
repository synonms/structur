using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public enum TitleEnumeration
{
    Unknown,
    Dr, 
    Miss, 
    Mr, 
    Mrs, 
    Ms, 
    Mx, 
    Prof
}

public record Title : StringValueObject<Title>
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<TitleEnumeration>().ToList(); 
    
    private Title(string value) : base(value)
    {
    }

    public static Title From(TitleEnumeration enumeration) => new(enumeration.ToString());
    
    public static OneOf<Title, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<Title>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(value, x =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new Title(matchingAcceptableValue);
            });

    public static OneOf<Maybe<Title>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Title>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static Title Convert(string value) =>
        CreateMandatory(nameof(Title), value)
            .Match(
                valueObject => valueObject,
                _ => From(TitleEnumeration.Unknown));
}