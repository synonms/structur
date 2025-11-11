using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Validation;

public class ValueObjectBuilder<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    private readonly List<DomainRuleFault> _faults = [];
    
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

    public ValueObjectBuilder<TValueObject> WithFaultIfValueLessThan(string propertyName, int value, int minimum)
    {
        if (value < minimum)
        {
            _faults.Add(DomainValidationFaults.MinValue(propertyName, minimum));
        }
        
        return this;
    }

    public ValueObjectBuilder<TValueObject> WithFaultIfValueMoreThan(string propertyName, int value, int maximum)
    {
        if (value > maximum)
        {
            _faults.Add(DomainValidationFaults.MaxValue(propertyName, maximum));
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

    public OneOf<TValueObject, IEnumerable<DomainRuleFault>> Build<TValue>(TValue value, Func<TValue, TValueObject> factoryFunc) =>
        _faults.Count != 0 ? _faults : factoryFunc(value);
}