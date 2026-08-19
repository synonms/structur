namespace Synonms.Structur.Domain.Validation;

public partial class ValidatedInstanceBuilder<T>
{
    public ValidatedInstanceBuilder<T> WithFaultIfNotPopulated(string propertyName, Guid value)
    {
        if (value == Guid.Empty)
        {
            _faults.Add(DomainValidationFaults.Required(propertyName));
        }

        return this;
    }
}