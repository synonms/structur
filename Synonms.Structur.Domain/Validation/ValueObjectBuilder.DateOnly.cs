using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Validation;

public partial class ValueObjectBuilder<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    public ValueObjectBuilder<TValueObject> WithFaultIfValueLessThan(string propertyName, DateOnly value, DateOnly minimum)
    {
        if (value < minimum)
        {
            _faults.Add(DomainValidationFaults.MinValue(propertyName, minimum));
        }
        
        return this;
    }

    public ValueObjectBuilder<TValueObject> WithFaultIfValueMoreThan(string propertyName, DateOnly value, DateOnly maximum)
    {
        if (value > maximum)
        {
            _faults.Add(DomainValidationFaults.MaxValue(propertyName, maximum));
        }
        
        return this;
    }
}