using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.Validation;

public partial class ValidatedInstanceBuilder<T>
{
    private readonly List<DomainRuleFault> _faults = [];
    
    public ValidatedInstanceBuilder<T> WithMandatoryScalarProperty<TValue, TValueObjectProperty>(TValue value, Func<TValue, OneOf<TValueObjectProperty, IEnumerable<DomainRuleFault>>> createFunc, out TValueObjectProperty valueObjectProperty)
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

    public ValidatedInstanceBuilder<T> WithOptionalScalarProperty<TValue, TValueObjectProperty>(TValue? value, Func<TValue?, OneOf<Maybe<TValueObjectProperty>, IEnumerable<DomainRuleFault>>> createFunc, out TValueObjectProperty? valueObjectProperty)
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

    public ValidatedInstanceBuilder<T> WithCollectionProperty<TValue, TValueObjectProperty>(List<TValue> values, Func<TValue, OneOf<TValueObjectProperty, IEnumerable<DomainRuleFault>>> createFunc, out List<TValueObjectProperty> valueObjectsProperty)
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
    
    public ValidatedInstanceBuilder<T> WithFaultIf(string propertyName, Func<bool> predicate, string faultDetail, params object?[] arguments)
    {
        if (predicate())
        {
            _faults.Add(new DomainRuleFault(faultDetail, new FaultSource(propertyName), arguments));
        }

        return this;
    }

    public OneOf<T, IEnumerable<DomainRuleFault>> Build(Func<T> factoryFunc) =>
        _faults.Count != 0 ? _faults : factoryFunc();
    
    public Maybe<DomainRulesFault> Build() =>
        _faults.Count != 0 ? new DomainRulesFault(_faults) : Maybe<DomainRulesFault>.None;
}