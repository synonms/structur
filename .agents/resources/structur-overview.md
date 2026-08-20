# Structur Overview

## Solution overview
Structur is a DDD-oriented framework for building consistent RESTful CRUD APIs with validation, logging, error handling, Hypermedia, and OpenAPI support.

## Architecture
- **Domain**: DDD entities, aggregates, and value objects.
- **Core**: CQRS, functional utilities, and fault handling.
- **API**: HTTP server infrastructure.
- **Infrastructure**: Persistence and repository implementations.
- **Sample**: Reference implementation showing framework usage.

## Key abstractions
- `Entity<T>`
- `AggregateRoot<T>`
- `AggregateMember<T>`
- `Result<T>` and `Maybe<T>`
- `ICommandHandler<TCommand, TResponse>`
- `IQueryHandler<TQuery, TResponse>`
- `DomainEvent<TAggregateRoot>`

## Request flow
1. A request is routed to the relevant generic endpoint.
2. The endpoint extracts route, query, or body data.
3. The endpoint sends a mediation request when the operation uses one.
4. The handler applies business logic and returns the response.

Create and edit form endpoints are handled directly in the endpoint.

## CRUD endpoint map
| HTTP Method | URL Template | Endpoint |
|---|---|---|
| GET | `/{feature}` | `GetAllEndpoint<TAggregateRoot, TResource>` |
| GET | `/{feature}/create-form` | `CreateFormEndpoint<TAggregateRoot, TResource>` |
| GET | `/{feature}/{id}` | `GetByIdEndpoint<TAggregateRoot, TResource>` |
| GET | `/{feature}/{id}/edit-form` | `EditFormEndpoint<TAggregateRoot, TResource>` |
| POST | `/{feature}` | `PostEndpoint<TAggregateRoot, TResource>` |
| PUT | `/{feature}/{id}` | `PutEndpoint<TAggregateRoot, TResource>` |
| DELETE | `/{feature}/{id}` | `DeleteEndpoint<TAggregateRoot>` |

`{feature}` is pluralised and lowercase. IDs are GUIDs.

## Project map
- `Sample.Api`
- `Api.Server`
- `Api.Core`
- `Domain`
- `Core`
- `Infrastructure`
- `Infrastructure.MongoDb`
- `Testing`
- `Domain.Tests.Unit`
- `Core.Tests.Unit`
- `Api.Server.Tests.Unit`
- `Sample.Tests.Integration`
