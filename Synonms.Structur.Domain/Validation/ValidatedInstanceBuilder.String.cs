using System.Text.RegularExpressions;

namespace Synonms.Structur.Domain.Validation;

public partial class ValidatedInstanceBuilder<T>
{
    public ValidatedInstanceBuilder<T> WithFaultIfNotPopulated(string propertyName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _faults.Add(DomainValidationFaults.Required(propertyName));
        }

        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfLengthMoreThan(string propertyName, string value, int maxLength)
    {
        if (value.Length > maxLength)
        {
            _faults.Add(DomainValidationFaults.MaxLength(propertyName, maxLength));
        }

        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfLengthNot(string propertyName, string value, int length)
    {
        if (value.Length != length)
        {
            _faults.Add(DomainValidationFaults.SpecificLength(propertyName, length));
        }

        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfNotOneOf(string propertyName, string value, List<string> acceptableValues)
    {
        if (acceptableValues.Contains(value, StringComparer.OrdinalIgnoreCase) is false)
        {
            _faults.Add(DomainValidationFaults.UnacceptableValue(propertyName, acceptableValues));
        }

        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfNotMatchingPattern(string propertyName, string value, string pattern)
    {
        if (Regex.IsMatch(value, pattern) is false)
        {
            _faults.Add(DomainValidationFaults.PatternMismatch(propertyName));
        }
        
        return this;
    }
}