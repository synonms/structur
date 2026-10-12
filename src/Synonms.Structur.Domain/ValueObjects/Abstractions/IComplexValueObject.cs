using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

/// <summary>
/// An object-shaped value with a domain-owned raw data contract, distinct from scalar value objects.
/// </summary>
/// <remarks>
/// Factories validate untrusted input. Rehydrate reconstructs trusted state using the type's explicit
/// missing-member policy and nested scalar rehydration policies, never the complex validation factory.
/// ToData projects current state without validation and must not expose mutable owned collections.
/// </remarks>
public interface IComplexValueObject<TSelf, TData>
    where TSelf : class, IComplexValueObject<TSelf, TData>
    where TData : class
{
    TData ToData();

    static abstract OneOf<TSelf, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, TData data);

    static abstract OneOf<Maybe<TSelf>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, TData? data);

    static abstract TSelf Rehydrate(TData data);
}
