using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum GenderReassignmentEnumeration
{
    Unknown = 0,
    None,	
    Transgender,
    Cisgender,
    Other,
    Unspecified
}

public class GenderReassignment : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<GenderReassignmentEnumeration>().ToList();

    private GenderReassignment(string value) : base(value)
    {
    }

    public static GenderReassignment From(GenderReassignmentEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<GenderReassignment, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<GenderReassignment>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new GenderReassignment(matchingAcceptableValue);
            });

    public static OneOf<Maybe<GenderReassignment>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<GenderReassignment>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static GenderReassignment Convert(string value) =>
        CreateMandatory(nameof(GenderReassignment), value)
            .Match(
                valueObject => valueObject,
                _ => From(GenderReassignmentEnumeration.Unknown));
}