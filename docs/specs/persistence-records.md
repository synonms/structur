# Persistence records technical specification

> **Proposed supersession (draft, not implemented):** [Framework usability 02](framework-usability-02-persistence-querying.md) replaces this plan's BSON aggregate/record round-trip design and unchanged-repository-interface requirement for the new normal path. It requires recursively plain C# storage records, typed capture/rehydration, bounded async server-side queries and generated mirrors after an explicit-record foundation. Direct aggregate persistence becomes explicit obsolete legacy opt-in, never a fallback. Retain this document as historical context; see the [overview](framework-usability-01-overview.md) for approval status and scope.

## Goal
Persist aggregate data through dedicated record models while preserving the existing domain-facing repository interfaces.

## Scope
- `Synonms.Structur.Infrastructure`
- `Synonms.Structur.Infrastructure.MongoDb`
- Sample aggregate persistence registrations and direct MongoDB test/seeding helpers

## Requirements
1. Introduce aggregate persistence metadata that maps each aggregate root type to:
   - the MongoDB collection name
   - the record type persisted for that aggregate
2. Keep repository interfaces unchanged:
   - `IReadAggregateRepository<TAggregateRoot>`
   - `IWriteAggregateRepository<TAggregateRoot>`
3. MongoDB repositories must:
   - write record documents to MongoDB
   - read record documents from MongoDB
   - map between records and domain aggregates automatically
   - continue applying the existing deleted and tenant filtering semantics
4. Aggregate record types should inherit the existing base record classes in `Synonms.Structur.Infrastructure.Persistence`.
5. Sample record types should use nullable properties and sensible defaults for resilience against schema changes.
6. Existing sample seed/test helpers that access MongoDB collections directly must be updated to use the registered record types.

## Design
1. Add aggregate persistence configuration metadata and expose helper accessors for collection name and record type lookup.
2. Rework MongoDB aggregate repositories to target record-backed BSON documents internally and translate to/from domain aggregates at repository boundaries.
3. Use BSON serialisation for automatic mapping so matching record/domain property graphs do not need handwritten mappers.
4. Add sample record types for employee and employment aggregates, including nested aggregate member records.

## Validation
- Build and test the affected projects.
- Note the pre-existing solution-level `.sln` parsing failure if it still blocks whole-solution validation.
