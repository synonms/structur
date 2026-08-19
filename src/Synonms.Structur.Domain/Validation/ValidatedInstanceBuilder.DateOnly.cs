namespace Synonms.Structur.Domain.Validation;

public partial class ValidatedInstanceBuilder<T>
{
    public ValidatedInstanceBuilder<T> WithFaultIfValueLessThan(string propertyName, DateOnly value, DateOnly minimum)
    {
        if (value < minimum)
        {
            _faults.Add(DomainValidationFaults.MinValue(propertyName, minimum));
        }
        
        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfValueMoreThan(string propertyName, DateOnly value, DateOnly maximum)
    {
        if (value > maximum)
        {
            _faults.Add(DomainValidationFaults.MaxValue(propertyName, maximum));
        }
        
        return this;
    }
}