namespace Synonms.Structur.Domain.Validation;

public partial class ValidatedInstanceBuilder<T>
{
    public ValidatedInstanceBuilder<T> WithFaultIfValueLessThan(string propertyName, int value, int minimum)
    {
        if (value < minimum)
        {
            _faults.Add(DomainValidationFaults.MinValue(propertyName, minimum));
        }
        
        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfValueMoreThan(string propertyName, int value, int maximum)
    {
        if (value > maximum)
        {
            _faults.Add(DomainValidationFaults.MaxValue(propertyName, maximum));
        }
        
        return this;
    }
}