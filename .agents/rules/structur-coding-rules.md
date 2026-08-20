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
- Use `SimpleValueObject<T>` or a scalar specialisation such as `StringValueObject`.
- Use `ComplexValueObject` for multi-property value objects.
- Validate in factory methods, not in constructors.
- Provide `Convert` helpers and implicit conversions where the type is a single scalar.

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
