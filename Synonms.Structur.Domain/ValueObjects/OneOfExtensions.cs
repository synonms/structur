using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public static class OneOfExtensions
{
    public static OneOf<Maybe<TValueObject>, IEnumerable<DomainRuleFault>> ToMaybe<TValueObject>(this OneOf<TValueObject, IEnumerable<DomainRuleFault>> oneOf) where TValueObject : ValueObject<TValueObject> =>
        oneOf.Match(
            valueObject => Maybe<TValueObject>.Some(valueObject),
            domainRuleFaults => new OneOf<Maybe<TValueObject>, IEnumerable<DomainRuleFault>>(domainRuleFaults));
}