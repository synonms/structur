# Structur Coding Rules

## Domain modeling
- Aggregate roots must inherit from `AggregateRoot<TAggregateRoot>`.
- Aggregate members must inherit from `AggregateMember<TAggregateMember>`.
- Use typed IDs via `EntityId<T>`.
- Track audit via `CreatedAction`, `UpdatedAction`, and `DeletedAction`.
- Use `EntityTag` for optimistic concurrency.
- Use `UpdateMandatoryValue` and `UpdateOptionalValue` for state changes.
- Public methods that modify state should return `Maybe<Fault>`.
- Prefer internal static factory methods that validate and return `Result<TAggregateRoot>`.
- Prefer internal instance methods for aggregate mutations.

## Value objects
- For scalar values, implement `IValueObject<TSelf, TValue>` and use `ValueObject<TSelf, TValue>` for shared equality behavior; expose a read-only `Value` and an outward raw-value conversion.
- For multi-property values, implement `IComplexValueObject<TSelf, TData>` with a domain-owned raw DTO, validated factories, `ToData`, and an explicit trusted `Rehydrate` missing-member policy. Optionally inherit `ComplexValueObject<TSelf>` and override protected `EqualityMembers` for shared exact-type, ordered member equality and hashing; serializers and mappers only require the interface.
- Validate in factory methods, not in constructors.
- Keep scalar rehydration policies unchanged unless separately reviewed; never add implicit conversions from raw input into value objects.
- Complex output mapping projects to the raw DTO first. Register nested adapters for the actual DTO-to-resource pair; a domain-value-object adapter does not apply to its projected DTO.

## Functional error handling
- Avoid exceptions for expected business failures.
- Use `Result<T>` for operations that return a value.
- Use `Maybe<Fault>` for operations that only signal success or failure.
- Use `Match` or `Bind` to chain operations.

## Domain events and projections
- Domain events must inherit from `DomainEvent<TAggregateRoot>`.
- Created, updated, and deleted events should use the framework event base types.
- Keep event creation and application logic aligned with the aggregate lifecycle.
- Use projections for read models that are replayed from event history.

## API and registration
- Use the generic endpoint classes for standard CRUD operations.
- Map aggregates to resources with `StructurResourceAttribute`.
- Use `IResourceMapper` for resource mapping.
- Use `ServiceCollectionExtensions` and Scrutor-based registration patterns where applicable.

## Attributes and metadata
- Use the Structur validation and metadata attributes consistently.
- Keep resource metadata, validation hints, and schema hints on the public API model.

## Testing and quality
- Organise tests by concern, using `Entities`, `Validation`, `Events`, `System`, and `Shared` where it fits.
- Use `Fact` for deterministic tests and `Theory` for parameterised tests.
- Prefer real domain objects over mocking when testing core domain behaviour.

## Style
- Use 4 spaces for indentation.
- Prefer explicit types over `var` when clarity benefits.
- Use nullability annotations correctly.
- Use braces for all control structures.
- Keep methods focused and short.
- Prefer expression-bodied members only for simple members.
- Use pattern matching and switch expressions when they improve readability.

## Naming
- Use PascalCase for types, members, constants, and namespaces.
- Use camelCase for variables and private fields with a leading underscore.
- Keep namespace names aligned to folder structure.
