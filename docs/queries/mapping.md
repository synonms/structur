# Resource Mapping

## Context

`Structur` contains three layers of model representation in a vertical feature slice:
- `Aggregate` types are the heart of the domain and encapsulate business logic and rules. They are composed of an aggregate root, aggregate members and value objects.
- `Record` types are simplified internal representations of aggregates used for persistence. They are composed of plain data types, mostly nullable.
- `Resource` types are simplified external representations of aggregates used for API requests and responses. They are composed of plain data types and include variants (`Resource` for aggregate roots, `ChildResource` for aggregate members etc.)

`Synonms.Structur.Api.Core\Schema\Resources` contains the various Resource types that are used to represent aggregates in the API.
`Synonms.Structur.Api.Server\Mapping` contains a set of convention based mapping classes that convert aggregates to resources for presentation via the API.
The primary goal is to allow Developers to focus on the business domain and not worry about the plumbing. They should be able to create an aggregate and a corresponding resource and have Structur automatically handle the mapping between them, including any value objects that are part of the aggregate.

## Query

The current solution is convoluted, with several different Resource classes and complicated mapping classes that are difficult to understand, maintain and extend.
I am looking for ways to keep the same capabilities but implement them in a streamlined and simplified manner.

Goals:
- Maintain the benefits of separate aggregates (internal representation) and resources (external representation):
  - Enable external representations which differ from the internal representation (e.g. omit private or sensitive properties or extend the resource with additional fields).
  - Enable versioning of external representations without affecting the internal representation.
  - Enable the use of different external representations for the same internal representation (e.g. a `User` aggregate may have a `UserResource` for public API and a `UserAdminResource` for admin API).
  - Enable the use of plain data types for external representations, which are easier to serialize and deserialize.
- Maintain the automatic, convention based mapping so Developers don't need to map by hand.
- Enable overriding or extension of the mapping for specific cases where the default mapping is not sufficient.
- Reduce the number of Resource classes and mapping classes to simplify the solution and make it easier to understand, maintain and extend.

Can you review the current solution for resources and mapping and provide some suggestions on what improvements I could make?
