---
name: new-value-object
description: Guide for adding new Value Object classes to the internal Sample projects or to any projects consuming the Structur framework. Use this when asked to add new features, aggregates or CRUD endpoints to any consuming projects and a new value object is required, or if asked to add a new value object to an existing aggregate.
---

To add new value objects to a consuming project, follow this process:

1. Determine whether the Value Object encapsulates a single scalar value (Simple Value Object) or multiple properties (Complex Value Object).
2. Determine whether the Value Object is specific to a single aggregate or is likely to be shared across multiple aggregates:
   - If it is likely to be shared, it should be created in the Structur Domain project for greater re-use:
      - Use the "ValueObjects" folder of the Structur Domain project folder for the new Value Object class (`Synonms.Structur.Domain/ValueObjects`).
      - If it is a Complex Value Object, use the "Resources" folder of the Structur Api Core project for the corresponding Value Object Resource class (`Synonms.Structur.Api.Core/Resources`).
   - If it is specific to a single aggregate, it can be created directly in the consuming API project:
     - Determine the Collection Name (pluralised aggregate name) for the parent aggregate. For example, if you are adding a value object to a "Employee" aggregate, the Collection Name would be "Employees".
     - Use the "Features/{CollectionName}/ValueObjects" folder of the consuming API project for the new Value Object class. For example, `Synonms.Structur.Sample.Api/Features/Employees/ValueObjects`.  Create this folder if it does not exist.
     - If it is a Complex Value Object, use the "Features/{CollectionName}/ValueObjects" folder of the consuming Client API project for the new Value Object Resource class. For example, `Synonms.Structur.Sample.ClientApi/Features/Employees/ValueObjects`.  Create this folder if it does not exist.
3. If the new value object is a scalar Value Object then the following steps are required:
   - Create the Simple Value Object class in the value objects folder.
     - The class should be named using the singular name of the value object in Pascal case, for example `EmploymentContractStatus`.
     - Implement `IValueObject<TSelf, TValue>` and inherit from `ValueObject<TSelf, TValue>`, for example `ValueObject<EmploymentContractStatus, string>`.
     - It should present a private constructor taking the underlying value as a parameter.
     - It should present a static factory method that performs validation against mandatory raw values which returns `OneOf<TValueObject, IEnumerable<DomainRuleFault>>` to ensure that only valid instances can be created, for example `public static OneOf<EmploymentContractStatus, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value)`.
     - `CreateMandatory` is the validated path for untrusted input; `Rehydrate` is the trusted persistence entry point. Do not introduce new scalar policies as part of complex-value-object work.
     - Define `Rehydrate` policy explicitly per type and preserve existing policies for existing types, including any validation-backed fallback. Do not invent sentinel IDs, tenant/security values, or other domain data.
     - Expose `public override TValue Value { get; }` and an outward implicit conversion to the raw value. Do not add implicit conversions from raw scalars into value objects. Preserve exact-concrete-type equality.
     - It should present a static factory method that performs validation against optional raw values which returns `OneOf<Maybe<TValueObject>, IEnumerable<DomainRuleFault>>` to ensure that only valid instances can be created or null values converted to `Maybe<T>`, for example `public static OneOf<Maybe<EmploymentContractStatus>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)`.
   - The generic MongoDB provider discovers scalar contracts and delegates to the registered raw-value serializer. A per-type BSON serializer is only needed for an exceptional storage shape; register it before the first serializer lookup.
4. If the new value object is a Complex Value Object then the following steps are required:
   - Create the related Complex Value Object Resource class. This class will define the API contract for the value object. Use the following guidelines:
     - The class should be named using the Value Object name in Pascal case followed by "Resource", for example `EmploymentContractTermsResource`.
     - It should inherit from `ComplexValueObjectResource`.
     - It should define public mutable properties for the resource as specified in the requirements, for example `public string Name { get; set; }` and `public decimal Price { get; set; }`. Names must match the raw DTO properties for conventional output mapping.
     - Scalar properties should be plain C# types (string, int, decimal, etc.) or collections of plain C# types (e.g. `List<string>`).
   - Create the Complex Value Object class in the value objects folder of the API project.  For example `Synonms.Structur.Sample.Api/Features/Employees/ValueObjects`.
     - The class should be named using the singular name of the value object in Pascal case, for example `EmploymentContractTerms`.
     - It should implement `IComplexValueObject<TSelf, TData>`; there is no required domain base class.
     - Define immutable domain state, for example `public EffectiveDate EffectiveFrom { get; private set; }`, using scalar contracts or nested complex values where appropriate.
     - It should present a private constructor taking all underlying values as parameters.
     - Provide validated `CreateMandatory(string propertyName, EmploymentContractTermsData data)` returning `OneOf<EmploymentContractTerms, IEnumerable<DomainRuleFault>>`.
     - Provide `CreateOptional(string propertyName, EmploymentContractTermsData? data)` returning `OneOf<Maybe<EmploymentContractTerms>, IEnumerable<DomainRuleFault>>`. Define when absent/partial input is None versus when complete-invalid input produces faults.
     - Add a domain-owned raw DTO (for example `EmploymentContractTermsData`) containing raw scalar values, nested raw DTOs, and owned collections. Use nullable members where missing input must be distinguished from a valid value such as `0` or `false`.
     - Implement DTO factory overloads, `ToData()` and static `Rehydrate(TData)`. Keep existing raw-parameter factory signatures when extending existing types. Validate commands in factories; trusted reconstruction uses explicit required missing/null policies and existing nested scalar `Rehydrate` policies.
     - Own mutable input collections and return fresh DTO collections from `ToData`. Optionally inherit `ComplexValueObject<TSelf>` and override protected `object?[] EqualityMembers` with raw scalar values and ordered collections to share exact-concrete-type member equality and hashing. The base is independent of `TData`; types with another base can implement equality using `ComplexValueObjectEquality`.
     - Conventional output mapping projects through `ToData`. For exceptional nested shapes, register `ComplexValueObjectResourceMapping.Register<NestedData, NestedResource>(...)`; a `NestedValueObject` adapter is not applied to an already projected `NestedData`. Collection mappings require a unique `IEnumerable<T>` element type and support lists, compatible interfaces and vector arrays; other destinations require adapters.
     - Generic complex BSON serialization delegates to the raw DTO serializer. Register custom serializers before first lookup and verify custom member/scalar shapes against DTO storage and query metadata; parity is not automatic.

**Example:**
```csharp
public class Moniker : ValueObject<Moniker, string>, IValueObject<Moniker, string>
{
    public const int DefaultMaxLength = 250;
    private Moniker(string value) { Value = value; }
    public override string Value { get; }
    public static implicit operator string(Moniker valueObject) => valueObject.Value;
    
    public static OneOf<Moniker, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<Moniker, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<Moniker>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(() => new Moniker(value));

    public static OneOf<Maybe<Moniker>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)  =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<Moniker>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Moniker>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }
    
    // Mirrors this built-in's existing fallback policy, not a new policy for every scalar.
    public static Moniker Rehydrate(string value) =>
        CreateMandatory(nameof(Moniker), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new Moniker(string.Empty));
}
```