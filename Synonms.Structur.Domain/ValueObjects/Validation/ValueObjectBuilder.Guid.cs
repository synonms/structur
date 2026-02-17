using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects.Validation;

public partial class ValueObjectBuilder<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    public ValueObjectBuilder<TValueObject> WithFaultIfNotPopulated(string propertyName, Guid value)
    {
        if (value == Guid.Empty)
        {
            _faults.Add(DomainValidationFaults.Required(propertyName));
        }

        return this;
    }
}