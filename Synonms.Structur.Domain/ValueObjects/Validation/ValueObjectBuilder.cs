using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects.Validation;

public partial class ValueObjectBuilder<TValueObject>
    where TValueObject : ValueObject<TValueObject>
{
    private readonly List<DomainRuleFault> _faults = [];
    
    public ValueObjectBuilder<TValueObject> WithMandatoryValueObjectProperty<TValue, TValueObjectProperty>(TValue value, Func<TValue, OneOf<TValueObjectProperty, IEnumerable<DomainRuleFault>>> createFunc, out TValueObjectProperty valueObjectProperty)
    {
        TValueObjectProperty output = default(TValueObjectProperty)!;

        createFunc
            .Invoke(value)
            .Match(
                createdValueObject => output = createdValueObject,
                domainRuleFaults => _faults.AddRange(domainRuleFaults));

        valueObjectProperty = output;

        return this;
    }

    public ValueObjectBuilder<TValueObject> WithOptionalValueObjectProperty<TValue, TValueObjectProperty>(TValue? value, Func<TValue?, OneOf<Maybe<TValueObjectProperty>, IEnumerable<DomainRuleFault>>> createFunc, out TValueObjectProperty? valueObjectProperty)
        where TValueObjectProperty : class
    {
        TValueObjectProperty? output = null;

        createFunc
            .Invoke(value)
            .Match(
                maybeValueObject => output = maybeValueObject.Match(valueObject => valueObject, () => null as TValueObjectProperty),
                domainRuleFaults => _faults.AddRange(domainRuleFaults));

        valueObjectProperty = output;

        return this;
    }

    public ValueObjectBuilder<TValueObject> WithValueObjectCollectionProperty<TValue, TValueObjectProperty>(List<TValue> values, Func<TValue, OneOf<TValueObjectProperty, IEnumerable<DomainRuleFault>>> createFunc, out List<TValueObjectProperty> valueObjectsProperty)
    {
        List<TValueObjectProperty> output = [];
        List<DomainRuleFault> accumulatedFaults = [];

        foreach (TValue value in values)
        {
            createFunc
                .Invoke(value)
                .Match(
                    createdValueObject => output.Add(createdValueObject),
                    domainRuleFaults => accumulatedFaults.AddRange(domainRuleFaults));
        }

        if (accumulatedFaults.Any())
        {
            _faults.AddRange(accumulatedFaults);
            valueObjectsProperty = [];
        }
        else
        {
            valueObjectsProperty = output;
        }

        return this;
    }
    
    public ValueObjectBuilder<TValueObject> WithFaultIf(string propertyName, Func<bool> predicate, string faultDetail, params object?[] arguments)
    {
        if (predicate())
        {
            _faults.Add(new DomainRuleFault(faultDetail, new FaultSource(propertyName), arguments));
        }

        return this;
    }

    public OneOf<TValueObject, IEnumerable<DomainRuleFault>> Build(Func<TValueObject> factoryFunc) =>
        _faults.Count != 0 ? _faults : factoryFunc();

    public OneOf<TValueObject, IEnumerable<DomainRuleFault>> Build<TValue>(TValue value, Func<TValue, TValueObject> factoryFunc) =>
        _faults.Count != 0 ? _faults : factoryFunc(value);
}