using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Validation;

public partial class ValueObjectBuilder<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    private readonly List<DomainRuleFault> _faults = [];
    
    public OneOf<TValueObject, IEnumerable<DomainRuleFault>> Build<TValue>(TValue value, Func<TValue, TValueObject> factoryFunc) =>
        _faults.Count != 0 ? _faults : factoryFunc(value);
}