# Simplified Value Objects

## Context

In software development, a value object is a small object that represents a simple entity whose equality is based on its value rather than its identity. 
Value objects are often used to encapsulate and validate data, ensuring that it adheres to specific rules and constraints.
They are beneficial for developers because they add context and meaning to the data they represent, making the code more readable and maintainable, while also providing reuse so business rules are consistently applied.
`Structur` contains two variants of value objects, `SimpleValueObject` which encapsulate a single value and `ComplexValueObject` which can encapsulate multiple values.
They contain validation logic and can be used to enforce business rules, ensuring that aggregates cannot be created in an invalid state.
Value Objects add significant complexity because they require additional code for JSON serialisation and persistence. 
Mapping aggregates to and from the simple types found in `Resource` and `Record` types also requires very complex and fragile code e.g. in `DefaultResourceMapper`.
This complexity overhead exists both in the Structur framework itself and also for consumers of the framework who want to add their own value objects. 

## Query

I am looking for ways to achieve the benefits of value objects without the complexity of implementing them in `Structur`.

Goals:
- Maintain the benefits of value objects, including encapsulation, validation, and business rule enforcement.
- Maintain the use of value objects to encapsulate a single value or multiple values.
- Prefer `System.Text.Json` for JSON serialisation and deserialisation of value objects.
- Provide sensible default implementations of common methods like `.ToString()` (for example ISO8601 format string for dates, 2 decimal place number for decimals etc.).
- Remove/reduce the need for custom JSON parsing and serialisation code for value objects.
- Remove/reduce the need for custom resource mapping code for value objects - ideally implicit casting. 
- Remove/reduce the need for custom persistence mapping code for value objects - ideally implicit casting.
- Simplify the addition of new value objects, both directly in the Structur solution and in consuming projects - ideally so that only the value object itself is needed, no additional converters/parsers etc.

Can you provide some suggestions on how I could achieve this?
