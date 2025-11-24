using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Validation;

public partial class ValueObjectBuilder<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    public ValueObjectBuilder<TValueObject> WithFaultIfNotPopulated(string propertyName, DateTime value)
    {
        if (value == DateTime.MinValue || value == DateTime.MaxValue)
        {
            _faults.Add(DomainValidationFaults.Required(propertyName));
        }

        return this;
    }

    public ValueObjectBuilder<TValueObject> WithFaultIfValueLessThan(string propertyName, DateTime value, DateTime minimum)
    {
        if (value < minimum)
        {
            _faults.Add(DomainValidationFaults.MinValue(propertyName, minimum));
        }
        
        return this;
    }

    public ValueObjectBuilder<TValueObject> WithFaultIfValueMoreThan(string propertyName, DateTime value, DateTime maximum)
    {
        if (value > maximum)
        {
            _faults.Add(DomainValidationFaults.MaxValue(propertyName, maximum));
        }
        
        return this;
    }
}