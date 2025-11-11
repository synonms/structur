using Synonms.Structur.Core.Faults;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.Domain.Validation;

public static class DomainValidationFaults
{
    private const string RequiredFieldTemplate = "Must not be null or empty.";
    private const string MinLengthTemplate = "Must have minimum length of {0} characters.";
    private const string MaxLengthTemplate = "Must have maximum length of {0} characters.";
    private const string SpecificLengthTemplate = "Must be exactly {0} characters.";
    private const string LengthRangeTemplate = "Must be between {0} and {1} characters long.";
    private const string MinValueTemplate = "Must be equal to or greater than {0}.";
    private const string MaxValueTemplate = "Must be equal to or less than {0}.";
    private const string ValueRangeTemplate = "Must be between {0} and {1}.";
    private const string UnacceptableValueTemplate = "Must be in the list of acceptable values [{0}].";
    private const string PatternMismatchTemplate = "Must match expected format.";

    public static DomainRuleFault Required(string propertyName) =>
        new(RequiredFieldTemplate, new FaultSource(propertyName));

    public static DomainRuleFault MaxLength(string propertyName, int maxLength) =>
        new(MaxLengthTemplate, new FaultSource(propertyName), maxLength);

    public static DomainRuleFault MinLength(string propertyName, int minLength) =>
        new(MinLengthTemplate, new FaultSource(propertyName), minLength);

    public static DomainRuleFault SpecificLength(string propertyName, int length) =>
        new(SpecificLengthTemplate, new FaultSource(propertyName), length);

    public static DomainRuleFault LengthRange(string propertyName, int minLength, int maxLength) =>
        new(LengthRangeTemplate, new FaultSource(propertyName), minLength, maxLength);

    public static DomainRuleFault MaxValue(string propertyName, DateTime maxValue) =>
        new(MaxValueTemplate, new FaultSource(propertyName), maxValue);

    public static DomainRuleFault MinValue(string propertyName, DateTime minValue) =>
        new(MinValueTemplate, new FaultSource(propertyName), minValue);

    public static DomainRuleFault MaxValue(string propertyName, DateOnly maxValue) =>
        new(MaxValueTemplate, new FaultSource(propertyName), maxValue);

    public static DomainRuleFault MinValue(string propertyName, DateOnly minValue) =>
        new(MinValueTemplate, new FaultSource(propertyName), minValue);

    public static DomainRuleFault MaxValue(string propertyName, decimal maxValue) =>
        new(MaxValueTemplate, new FaultSource(propertyName), maxValue);

    public static DomainRuleFault MinValue(string propertyName, decimal minValue) =>
        new(MinValueTemplate, new FaultSource(propertyName), minValue);

    public static DomainRuleFault ValueRange(string propertyName, DateOnly minValue, DateOnly maxValue) =>
        new(ValueRangeTemplate, new FaultSource(propertyName), minValue, maxValue);

    public static DomainRuleFault UnacceptableValue(string propertyName, IEnumerable<string> acceptableValues) =>
        new(UnacceptableValueTemplate, new FaultSource(propertyName), string.Join(',', acceptableValues));

    public static DomainRuleFault PatternMismatch(string propertyName) =>
        new(PatternMismatchTemplate, new FaultSource(propertyName));
}