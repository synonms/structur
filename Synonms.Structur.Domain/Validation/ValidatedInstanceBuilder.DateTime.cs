namespace Synonms.Structur.Domain.Validation;

public partial class ValidatedInstanceBuilder<T>
{
    public ValidatedInstanceBuilder<T> WithFaultIfNotPopulated(string propertyName, DateTime value)
    {
        if (value == DateTime.MinValue || value == DateTime.MaxValue)
        {
            _faults.Add(DomainValidationFaults.Required(propertyName));
        }

        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfValueLessThan(string propertyName, DateTime value, DateTime minimum)
    {
        if (value < minimum)
        {
            _faults.Add(DomainValidationFaults.MinValue(propertyName, minimum));
        }
        
        return this;
    }

    public ValidatedInstanceBuilder<T> WithFaultIfValueMoreThan(string propertyName, DateTime value, DateTime maximum)
    {
        if (value > maximum)
        {
            _faults.Add(DomainValidationFaults.MaxValue(propertyName, maximum));
        }
        
        return this;
    }
}