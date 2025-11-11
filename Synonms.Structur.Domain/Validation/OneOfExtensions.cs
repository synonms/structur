using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Validation;

public static class OneOfExtensions
{
    public static OneOf<Maybe<TValueObject>, IEnumerable<DomainRuleFault>> ToMaybe<TValueObject>(this OneOf<TValueObject, IEnumerable<DomainRuleFault>> oneOf) where TValueObject : ValueObject<TValueObject> =>
        oneOf.Match(
            valueObject => Maybe<TValueObject>.Some(valueObject),
            domainRuleFaults => new OneOf<Maybe<TValueObject>, IEnumerable<DomainRuleFault>>(domainRuleFaults));
}