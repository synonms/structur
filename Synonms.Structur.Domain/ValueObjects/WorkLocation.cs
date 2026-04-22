using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public enum WorkLocationEnumeration
{
    Unknown = 0,
    Office,
    Home,
    Hybrid,
    Roaming,
    Other
}

public class WorkLocation : StringValueObject
{
    public static readonly List<string> AcceptableValues = Enum.GetNames<WorkLocationEnumeration>().Where(x => x != "Unknown").ToList();

    private WorkLocation(string value) : base(value)
    {
    }

    public static WorkLocation From(WorkLocationEnumeration enumeration) => new(enumeration.ToString());

    public static OneOf<WorkLocation, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<WorkLocation>()
            .WithFaultIfNotOneOf(propertyName, value, AcceptableValues)
            .Build(() =>
            {
                // Cross-reference the acceptable values to correct any case differences
                string matchingAcceptableValue = AcceptableValues.FirstOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) ?? value;

                return new WorkLocation(matchingAcceptableValue);
            });

    public static OneOf<Maybe<WorkLocation>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<WorkLocation>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static WorkLocation Convert(string value) =>
        CreateMandatory(nameof(WorkLocation), value)
            .Match(
                valueObject => valueObject,
                _ => From(WorkLocationEnumeration.Unknown));
}

