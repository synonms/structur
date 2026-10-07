# Persistence records for aggregate storage

## User story
As a **framework consumer**
I want **repositories to persist record models instead of domain aggregates**
So that **the persistence contract can evolve independently from the domain model and tolerate schema drift more safely**

## Acceptance criteria
1. Aggregate repositories continue to expose domain aggregates publicly.
2. MongoDB aggregate repositories store derived `Record` types internally instead of storing aggregate roots directly.
3. Persistence configuration identifies the record type to use for each aggregate root.
4. Record/domain mapping happens automatically inside the persistence layer.
5. Record models can declare nullable properties with defaults so missing or extra database fields do not break deserialisation.
6. The Sample application demonstrates the pattern with concrete aggregate record types.
