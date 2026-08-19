# Structur Framework

## Solution Overview

**Structur** is a framework to assist in the creation of consistent, feature rich RESTful CRUD APIs.  
It reduces the amount of boilerplate code required to build API services, enabling the Developer to concentrate on the Domain business logic.  
It provides a set of core libraries and tools that can be used to build APIs with features such as Hypermedia, validation, error handling, logging, and more.  
The framework is designed to be flexible and extensible, allowing Developers to easily add custom functionality as needed.  
It also includes support for OpenAPI/Swagger for API documentation and testing, making it easier to create well-documented APIs that are easy to use and maintain.


## Agents
Custom agents are defined in the `.agents` folder and are responsible for specific tasks in the development process.  Markdown files are named `[AGENT_NAME].agent.md` and contain the agent's purpose, responsibilities, and workflows.  The following agents are defined:
- **framework-developer** - Maintains and enhances the core functionality of the Structur framework.
- **implementation-validator** - Validates that code implementations match their technical specifications and identifies gaps between requirements and implementation.
- **integration-test-writer** - Writes integration tests to verify that the system works as expected when all components are integrated.
- **readme-specialist** - Specialised agent for creating and improving README files and project documentation.
- **sample-api-developer** - Implements features in the Sample API projects to demonstrate the usage of the Structur framework.
- **sample-ui-developer** - Implements features in the Sample UI projects to demonstrate the usage of the Structur framework.
- **sample-integration-test-writer** - Writes integration tests for the Sample projects to ensure that the implemented features work as expected.
- **spec-writer** - Writes technical specifications for new features and enhancements.
- **unit-test-writer** - Writes unit tests to define expected behaviour based on User Stories
- **user-story-writer** - Writes User Stories to define the expected behaviour of a feature from the perspective of the end user.

## Workflows
When asked to implement a new feature, follow the detailed instructions in [Feature Implementation](.agents/workflows/feature-implementation.md) to orchestrate the process from user story creation to final validation and testing.


## Architecture Overview

### High-Level Design
**Structur** is a Domain Driven Design (DDD) multipurpose framework to simplify Web API development. The architecture follows these core principles:

1. **Layered Architecture:**
    - **Domain Layer** (`Synonms.Structur.Domain`) - Core DDD entities, value objects, aggregates
    - **Core Layer** (`Synonms.Structur.Core`) - Functional utilities, CQRS, fault handling
    - **API Layer** (`Synonms.Structur.Api.Server`, `Synonms.Structur.Api.Core`) - HTTP server infrastructure
    - **Infrastructure** (`Synonms.Structur.Infrastructure`, `Synonms.Structur.Infrastructure.MongoDb`) - Data persistence
    - **Sample** - Reference implementation demonstrating framework usage

2. **Key DDD Abstractions:**
    - **Entity<T>** - Base entity class with typed identity
    - **AggregateRoot<T>** - Base class for domain aggregate roots with version tracking and audit fields (CreatedAction, UpdatedAction, DeletedAction)
    - **AggregateMember<T>** - Base class for domain aggregate members
    - **Value Objects** - Immutable domain concepts (StringValueObject, SimpleValueObject<T>, ComplexValueObject subclasses)
    - **Domain Events** - Event driven mutations with in-process domain event support via IDomainEventDispatcher, IDomainEventHandler

3. **Functional Programming Support:**
    - `Result<T>` and `Result<T, TFault>` - Railway-oriented result types
    - `Maybe<T>` - Option/Optional pattern
    - `OneOf<T1, T2, ...>` - Discriminated unions
    - Extensions for async operations (ResultExtensions.Async, MaybeExtensions.Async)

4. **CQRS Pattern:**
    - Command/Query separation with `ICommandHandler<TCommand, TResponse>` and `IQueryHandler<TQuery, TResponse>`
    - CqrsBuilder for fluent registration and behaviour decoration
    - Behaviour decorators execute in reverse registration order (last registered = first executed)

5. **Generic Endpoints:**
    - Base endpoint classes for standard CRUD operations (Get, Post, Put, Delete)
    - Automatic registration of endpoints and handlers based on aggregate root attributes

6. **Resources:**
    - Resource classes represent the public API contract and are decorated with attributes to control schema generation and validation
    - Resources can be shared between API and UI projects to ensure consistency
    - Aggregates are automatically mapped to Resources via `IResourceMapper`

### Request Flow

Requests to the API flow as follows:
- Incoming HTTP request is routed to the relevant endpoint based on the URL and HTTP method, for example `GET /employees` would be routed to the registered instance of the `GetAllEndpoint<Employee, EmployeeResource>` endpoint class.
- The endpoint class extracts any relevant information from the request, such as query string parameters for filtering, sorting and pagination in the case of the `GetAllEndpoint`, or the Resource from the request body in the case of the `PostEndpoint` and `PutEndpoint`.
- The endpoint class then sends a request to the Mediation Handler for the relevant operation, for example `GetAllEndpoint<Employee, EmployeeResource>` would send a `ReadResourceCollectionQuery<Employee, EmployeeResource>` request to the `ReadResourceCollectionQueryProcessor<Employee, EmployeeResource>` Mediation Handler and receive a `ReadResourceCollectionQueryResponse<Employee, EmployeeResource>` in reply.  The exceptions to this are the CreateForm and EditForm endpoints which handle the request directly in the endpoint and do not relay a mediation request.
- The Mediation Handler contains the business logic for the operation, and typically interacts with the Domain and Infrastructure layers to perform the necessary actions.  For example, the `ReadResourceCollectionQueryProcessor<Employee, EmployeeResource>` would interact with the Repository to retrieve the relevant Employee Aggregates from the database, map them to EmployeeResource objects using the mapping configuration, and return them in a `ReadResourceCollectionQueryResponse<Employee, EmployeeResource>` response object.

Supported CRUD operations and the related classes are as follows:

| HTTP Method | URL Template                | Endpoint class                                  | Mediation request class                                  | Mediation handler class                                           | Mediation response class                                         | Description                                                                                                                                                                                                                                                          |
|-------------|-----------------------------|-------------------------------------------------|----------------------------------------------------------|-------------------------------------------------------------------|------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `GET`       | `/{feature}`                | `GetAllEndpoint<TAggregateRoot, TResource>`     | `ReadResourceCollectionQuery<TAggregateRoot, TResource>` | `ReadResourceCollectionQueryProcessor<TAggregateRoot, TResource>` | `ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>` | Get all resources for the feature, e.g. `GET /employees` to get all employees.                                                                                                                                                                                       |
| `GET`       | `/{feature}/create-form`    | `CreateFormEndpoint<TAggregateRoot, TResource>` | n/a                                                      | n/a                                                               | n/a                                                              | Get the data needed to create a new resource, such as the fields required along with validation rules, e.g. `GET /employees/create-form` to get the create form for a new employee.                                                                                  |
| `GET`       | `/{feature}/{id}`           | `GetByIdEndpoint<TAggregateRoot, TResource>`    | `FindResourceQuery<TAggregateRoot, TResource>`           | `FindResourceQueryProcessor<TAggregateRoot, TResource>`           | `FindResourceQueryResponse<TAggregateRoot, TResource>`           | Get a single resource by ID, e.g. `GET /employees/11111111-1111-1111-1111-111111111111` to get the employee with ID 11111111-1111-1111-1111-111111111111.                                                                                                            |
| `GET`       | `/{feature}/{id}/edit-form` | `EditFormEndpoint<TAggregateRoot, TResource>`   | n/a                                                      | n/a                                                               | n/a                                                              | Get the data needed to update a single resource by ID, such as the fields required along with validation rules, e.g. `GET /employees/11111111-1111-1111-1111-111111111111/edit-form` to get the edit form for employee with ID 11111111-1111-1111-1111-111111111111. |
| `POST`      | `/{feature}`                | `PostEndpoint<TAggregateRoot, TResource>`       | `CreateResourceCommand<TAggregateRoot, TResource>`       | `CreateResourceCommandProcessor<TAggregateRoot, TResource>`       | `CreateResourceCommandResponse<TAggregateRoot, TResource>`       | Create a new resource, e.g. `POST /employees` to create a new employee.                                                                                                                                                                                              |
| `PUT`       | `/{feature}/{id}`           | `PutEndpoint<TAggregateRoot, TResource>`        | `UpdateResourceCommand<TAggregateRoot, TResource>`       | `UpdateResourceCommandProcessor<TAggregateRoot, TResource>`       | `UpdateResourceCommandResponse<TAggregateRoot, TResource>`       | Update an existing resource by ID, e.g. `PUT /employees/11111111-1111-1111-1111-111111111111` to update the employee with ID 11111111-1111-1111-1111-111111111111.                                                                                                   |
| `DELETE`    | `/{feature}/{id}`           | `DeleteEndpoint<TAggregateRoot>`                | `DeleteResourceCommand<TAggregateRoot>`                  | `DeleteResourceCommandProcessor<TAggregateRoot>`                  | `DeleteResourceCommandResponse<TAggregateRoot>`                  | Delete an existing resource by ID, e.g. `DELETE /employees/11111111-1111-1111-1111-111111111111` to delete the employee with ID 11111111-1111-1111-1111-111111111111.                                                                                                |

`{feature}` is pluralised and lowercase in the URL, and the ID is a GUID.

### Project Dependencies
```
Sample.Api (ASP.NET Core app)
├── Api.Server (HTTP infrastructure)
├── Api.Core (API abstractions)
├── Domain (DDD entities)
├── Core (Utilities)
├── Infrastructure (Data access)
├── Infrastructure.MongoDb (MongoDB implementation)
└── Testing (Test utilities)

Tests
├── Domain.Tests.Unit (xUnit, covers Domain layer)
├── Core.Tests.Unit (xUnit, covers Core layer)
├── Api.Server.Tests.Unit (xUnit, covers API server)
└── Sample.Tests.Integration (Integration tests)
```


## Key Conventions

### Endpoints

The framework provides generic classes for the endpoints and Mediation Handlers which are automatically registered to enable standard CRUD operations for each feature without having to manually add them.  
The registration is initiated and controlled by decorating the Aggregate Root with the `StructurResourceAttribute`.  
The attribute specifies which public facing Resource class is presented via the API endpoints, for example the `Employee` Aggregate Root is mapped to an `EmployeeResource` which is located in the `ClientApi` project to allow sharing of the model with the UI project.


### Domain Events
- Domain Events are defined as classes that inherit from `DomainEvent<TAggregateRoot>` where T is the aggregate root type that the event applies to.  This provides a strong association between the event and the aggregate root, and allows for type safety when handling events.
- Additional base types have been created specifically for the created, updated, and deleted event types (`AggregateCreatedDomainEvent<TAggregateRoot, TResource>`, `AggregateUpdatedDomainEvent<TAggregateRoot, TResource>`, `AggrgeateDeletedDomainEvent<TAggregateRoot>`) which inherit from `DomainEvent<TAggregateRoot>`.
- An implementation of `IDomainEventFactory<TAggregateRoot, TResource>` is required for each aggregate root to enable the framework to create the events when an aggregate is created, updated, or deleted.  This factory is responsible for creating the appropriate event instance based on the type of change that occurred (create, update, delete) and populating the event with the necessary information about the change.
- Structur subverts the common way of implementing Domain Events in that events are created first and then apply themselves to the aggregate root via `DomainEvent<TAggregateRoot>.ApplyAsync(TAggregateRoot? aggregateRoot)`.  This means that the event contains all the information about the change that occurred, and the domain can then apply that change to its state.  This approach has several benefits:
    - It allows for better separation of concerns, as the event is responsible for describing the change, while the domain is responsible for applying that change to its state.
    - It makes it easier to implement event sourcing, as the events can be stored in an event store and replayed to reconstruct the state of the domain at any point in time.
    - It allows for better testing, as the events can be easily created and applied to the domain in isolation, without needing to set up complex test scenarios.
- Domain Events support Projections, which are read models that are updated in response to events via a combination of `Projection<TAggregateRoot>.Replay(EntityId<TAggregateRoot> aggregateId, IEnumerable<DomainEvent> eventHistory)` and `DomainEvent<TAggregateRoot>.Replay(Projection projection)`.

### Aggregates
- All aggregate roots inherit from `AggregateRoot<TAggregateRoot>` where T is the aggregate root type itself
- All aggregate members inherit from `AggregateMember<TAggregateMember>` where T is the aggregate member type itself
- Provide `EntityId<T>` typed ID via constructor
- Track audit via `CreatedAction`, `UpdatedAction`, `DeletedAction` (UserAction value objects)
- Use `EntityTag` for optimistic concurrency (regenerated on each update)
- Use `UpdateMandatoryValue` and `UpdateOptionalValue` protected methods for property updates as this handles audit and entity tag updates consistently across all aggregates
- Public methods that modify state return `Maybe<Fault>` to model failures
- Aggregate roots and members should provide an `internal static` factory method that performs validation and returns `Result<TAggregateRoot>` to ensure that only valid instances can be created (e.g. `Employee.Create(...)`).  The factory method should use the `Validator.CreateBuilder<T>()` to perform validation, create value objects and construct the aggregate instance if validation passes.
- Mutations of an existing aggregate should be performed via `internal` instance methods on the aggregate root which encapsulate the business logic and validation (e.g. `Employee.Update(...)`).  This ensures that the aggregate remains in a consistent state and that all invariants are maintained.  The methods should use the protected update methods to modify properties, which will handle updating the audit fields and entity tag automatically.  The methods should return `Maybe<Fault>` to indicate whether the operation was successful or if there were any validation errors or domain rule violations.

### Value Objects
- Inherit from `SimpleValueObject<T>` for single scalar values, or `ComplexValueObject` for complex objects depending on implementation.
- For single values there are additional base classes like `StringValueObject` and `IntValueObject` which derive from `SimpleValueObject<T>` which should be used where possible instead of directly inheriting from `SimpleValueObject<T>`.  Further base classes can be added to support additional scalar value types.
- Use factory methods for construction and validation (e.g., `Moniker.CreateMandatory("propertyName", "value")`) as they are designed to work with `Validator.CreateBuilder<T>()` when creating Aggregates.
- Provide implicit operators for ergonomic conversions (`implicit operator string(StringValueObject)`)
- Validate in factory methods; constructor can assume valid input
- For single value objects, provide a static `Convert` method which handles validation faults and provides a default value (e.g. `Moniker.Convert("value")`).  This is used by value converters or serializers required by some persistence providers (e.g. `MonikerBsonSerialiser` used by MongoDb).
- For single value objects, provide implicit conversion to/from the underlying type for ease of use. For example, a `Moniker` class that wraps a string can inherit from `StringValueObject` and provide implicit conversions to and from `string`. This allows you to use `Moniker` instances in place of strings without needing explicit casts, while still enforcing any validation rules defined in the factory method.

### Functional Error Handling and Optional types
- Avoid exceptions for business logic errors
- Use `Result<T>` for operations which return an object on success or a fault on failure
- Use `Maybe<Fault>` for operations which do not return an object on success but can fail with a fault (e.g. aggregate instance methods which modify state)
- Chain operations with `Match` or `Bind` methods
- Faults are the error type: `DomainRuleFault`, `ApplicationFault`, `EntityNotFoundFault`, etc.
- Some faults are aggregations of other faults, e.g. `DomainRulesFault` contains multiple `DomainRuleFault` instances - this enables multiple validation faults to be returned from a single operation, which is useful for scenarios such as validating an entire aggregate and returning all validation errors at once rather than failing fast on the first error.
- For success with data: `Result<T>.Success(value)`. For failure: `Result<T>.Failure(fault)`

### Dependency Injection & Auto-Registration
- Core layer uses `ServiceCollectionExtensions` for registration
- Scrutor is used for automatic interface implementation registration via attribute scanning
- Use `InternalsVisibleTo` to expose internals to test projects
- Assembly references stored as static properties (e.g., `SampleApiProject.Assembly`)

### Attributes for Metadata
The framework defines custom attributes for schema generation and validation:
- `[StructurMinLength]`, `[StructurMaxLength]` - String constraints
- `[StructurMinValue]`, `[StructurMaxValue]` - Numeric constraints
- `[StructurPattern]` - Regex pattern validation
- `[StructurRequired]`, `[StructurImmutable]`, `[StructurHidden]`, `[StructurDisabled]`
- `[StructurResource]`, `[StructurProjection]`, `[StructurLookup]` - Schema hints
- `[StructurVersionHistory]` - Track property version changes

### Testing Patterns
- Organize tests in `Entities`, `Validation`, `Events`, `System`, `Shared` folders
- Use `Fact` for deterministic tests, `Theory` for parameterized
- Place shared test utilities in `Shared` folder (TestUser, TestAggregateRoot, etc.)
- Test one scenario per method; method name describes the scenario
- No mocking framework; tests use real domain objects

### Versioning
- `VersionHistory` tracks property changes across versions
- `VersionExtensions` provides version comparison utilities
- Properties can be marked with `[StructurVersionHistory]` to enable tracking
- Useful for API versioning and schema evolution

### API Server Conventions
- Controllers inherit from framework base classes
- Use dependency injection for repositories and handlers
- HttpContext provides correlation IDs and tenant resolution
- Tenants resolved via header or query string strategies
- Response mapping handled by `DefaultResourceMapper`
- OpenAPI/Swagger UI auto-generated from schema attributes

### Infrastructure & Persistence
- MongoDB implementation in `Infrastructure.MongoDb`
- Domain events can be persisted via `IDomainEventRepository`
- Repositories implement `IAggregateRepository<T>`
- Lookups (reference data) via `ILookupRepository`


## Style Guides

- Use 4 spaces for indentation
- Use explicit type declarations and avoid `var` if possible
- Nullability is enabled on all projects and explicit nullability should be used in declarations (e.g. `string? nullableString` and `string nonNullableString`)
- Use braces for all control structures, even if they are optional
- Use meaningful variable and method names that clearly indicate their purpose
- Keep methods short and focused on a single task
- Use XML documentation comments for public methods and classes
- Avoid using magic strings or numbers; use constants instead
- Use async/await for asynchronous operations and allow for cancellation tokens to be passed through async methods where appropriate
- Handle exceptions gracefully and log them appropriately
- Use square bracket collection expression initialisers for creating collections e.g. `List<string> myList = [];`
- Use the Railway paradigm for error handling, returning `Result<T>` types instead of throwing exceptions where appropriate.  Use exceptions only for unexpected circumstances like infrastructure failures, not for expected error conditions like validation failures or not found errors.  This allows the caller to handle expected error conditions without needing to use try/catch blocks, and keeps exceptions reserved for truly exceptional circumstances.
- Use the `Maybe<T>` type for optional values, which is similar to the `Nullable<T>` type but can be used with reference types as well as value types.  This allows for more expressive code when dealing with optional values, and can help to avoid null reference exceptions by forcing the caller to explicitly handle the case where a value may be missing.
- Prefer a single statement per line for clarity, even if the language allows multiple statements on a single line.
- Use expression-bodied members for simple methods and properties where it enhances readability, but prefer block bodies for more complex logic to improve clarity.
- Use pattern matching and switch expressions where appropriate to simplify code and improve readability.


## Naming Conventions

- Classes: PascalCase
- Methods: PascalCase
- Variables: camelCase
- Constants: PascalCase
- Interfaces: I + PascalCase
- Enums: PascalCase
- Properties: PascalCase
- Fields: camelCase with _ prefix for private fields
- Namespaces: PascalCase, aligned with the location in the project (e.g. `Synonms.Structur.Domain.Aggregates` for classes in the `Aggregates` folder of the `Synonms.Structur.Domain` project)


## Build, Test, and Lint Commands

The project uses **Visual Studio 2022** build system (MSBuild) with .NET 9.0.

### Building
```bash
# Full solution build
dotnet build

# Build specific project
dotnet build Synonms.Structur.Domain

# Release build
dotnet build --configuration Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test Tests/Synonms.Structur.Domain.Tests.Unit

# Run single test by name
dotnet test --filter "AggregateRootTests.UpdateProperty_DifferentValue_RecordsUpdatedActionAndUpdatesEntityTag"

# Run with code coverage
dotnet test /p:CollectCoverageFromAttributes=true /p:CoverletOutputFormat=opencover
```

**Test Framework:** xUnit v3 3.1.5 with Microsoft.NET.Test.Sdk.  NSubstitute is used for mocking internal dependencies and WireMock.Net.Testcontainers for simulating external APIs.

### Code Analysis
No built-in linters configured. Code uses nullable reference types and implicit using statements (net9.0 defaults).
