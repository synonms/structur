using System.Text.RegularExpressions;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Validation;

public partial class ValueObjectBuilder<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    public ValueObjectBuilder<TValueObject> WithFaultIfNotPopulated(string propertyName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _faults.Add(DomainValidationFaults.Required(propertyName));
        }

        return this;
    }

    public ValueObjectBuilder<TValueObject> WithFaultIfLengthMoreThan(string propertyName, string value, int maxLength)
    {
        if (value.Length > maxLength)
        {
            _faults.Add(DomainValidationFaults.MaxLength(propertyName, maxLength));
        }

        return this;
    }

    public ValueObjectBuilder<TValueObject> WithFaultIfNotOneOf(string propertyName, string value, List<string> acceptableValues)
    {
        if (acceptableValues.Contains(value, StringComparer.OrdinalIgnoreCase) is false)
        {
            _faults.Add(DomainValidationFaults.UnacceptableValue(propertyName, acceptableValues));
        }

        return this;
    }

    public ValueObjectBuilder<TValueObject> WithFaultIfNotMatchingPattern(string propertyName, string value, string pattern)
    {
        if (Regex.IsMatch(value, pattern) is false)
        {
            _faults.Add(DomainValidationFaults.PatternMismatch(propertyName));
        }
        
        return this;
    }
}