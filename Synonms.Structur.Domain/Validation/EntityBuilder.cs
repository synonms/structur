using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.Domain.Validation;

public class EntityBuilder<TEntity>
    where TEntity : Entity<TEntity>
{
    private readonly List<DomainRuleFault> _faults = [];

    public EntityBuilder<TEntity> WithMandatoryValueObject<TValue, TValueObject>(TValue value, Func<TValue, OneOf<TValueObject, IEnumerable<DomainRuleFault>>> createFunc, out TValueObject valueObject)
    {
        TValueObject output = default(TValueObject)!;

        createFunc
            .Invoke(value)
            .Match(
                createdValueObject => output = createdValueObject,
                domainRuleFaults => _faults.AddRange(domainRuleFaults));

        valueObject = output;

        return this;
    }

    public EntityBuilder<TEntity> WithOptionalValueObject<TValue, TValueObject>(TValue? value, Func<TValue?, OneOf<Maybe<TValueObject>, IEnumerable<DomainRuleFault>>> createFunc, out TValueObject? valueObject)
        where TValueObject : class
    {
        TValueObject? output = null;

        createFunc
            .Invoke(value)
            .Match(
                maybeValueObject => output = maybeValueObject.Match(valueObject => valueObject, () => null as TValueObject),
                domainRuleFaults => _faults.AddRange(domainRuleFaults));

        valueObject = output;

        return this;
    }

    public EntityBuilder<TEntity> WithValueObjectCollection<TValue, TValueObject>(List<TValue> values, Func<TValue, OneOf<TValueObject, IEnumerable<DomainRuleFault>>> createFunc, out List<TValueObject> valueObjects)
    {
        List<TValueObject> output = [];
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
            valueObjects = [];
        }
        else
        {
            valueObjects = output;
        }

        return this;
    }

    public EntityBuilder<TEntity> WithFaultIf(Func<bool> failurePredicate, Func<DomainRuleFault> faultOnFailure)
    {
        if (failurePredicate())
        {
            DomainRuleFault fault = faultOnFailure();
            _faults.Add(fault);
        }

        return this;
    }
    
    public Maybe<Fault> Build() =>
        _faults.Count != 0 ? new DomainRulesFault(_faults) : Maybe<Fault>.None;
}