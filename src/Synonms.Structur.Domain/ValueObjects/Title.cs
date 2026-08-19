using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

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

public class Title : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<TitleEnumeration>().ToList(); 
    
    private Title(string value) : base(value)
    {
    }

    public static Title From(TitleEnumeration enumeration) => new(enumeration.ToString());
    
    public static OneOf<Title, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<Title>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

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