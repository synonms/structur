# Framework usability 01: overview and decisions

**Status:** Draft for approval; planning only, not implemented or validated.  
**Scope:** Framework-first reduction of persistence, query, CRUD/event, projection and validated-input boilerplate, followed by Sample migration. No user stories, implementation changes or new database/provider support are part of this documentation change.

## Plan map and implementation order

| Plan | Responsibility | Prerequisites |
|---|---|---|
| [02 — Plain persistence, generation and querying](framework-usability-02-persistence-querying.md) | Raw storage graphs, typed restoration, bounded server queries, generated common path | Approve contracts below; explicit records before generation |
| [05 — Validated inputs and mutations](framework-usability-05-input-mutations.md) | Shared validated editable state, atomic member changes, version scope, no-op semantics | Domain helpers; can start alongside explicit persistence foundation |
| [03 — CRUD, lifecycle events and projections](framework-usability-03-crud-events-projections.md) | Feature delegates, frozen result snapshots, typed replay, history adapters | 02 mapping contracts and 05 mutation contracts |
| [04 — Transactions and durable delivery](framework-usability-04-durable-delivery.md) | Conditional aggregate/event/outbox commit, constraints, sequences, dispatch | 02 write contracts and 03 event envelopes |
| [06 — Rollout and validation](framework-usability-06-rollout-validation.md) | Ordered delivery gates, migration, tests, documentation and measurable outcomes | All framework capabilities before Sample conversion |

Read in numeric order for context; execute in dependency order from plan 06. Generation is a bounded framework deliverable after manual mappings prove the contract, not a prerequisite for correctness. Do not migrate Sample features first and then extract infrastructure.

## Evidence and baseline

Repository instructions, existing specs, directory paths and project metadata were inspected for this plan. Implementation behavior below is the supplied prior-review baseline; source-code behavior was not independently re-reviewed in this documentation task. The first implementation gate must characterize those findings, not assume they are already fixed.

- The [existing persistence spec](persistence-records.md) keeps repository interfaces unchanged and recommends BSON aggregate/record round trips. [Mongo value-object persistence](simplified-value-objects-05-mongodb-persistence.md) documents the current serializer-backed path and distinguishes driver-only fixtures from live database tests. Those are historical/legacy guidance where explicitly superseded here.
- [Multiple representations](resource-mapping-02-multiple-representations.md), [plain resource contracts](resource-mapping-03-plain-contracts.md) and the [resource contract guide](../resource-contracts.md) describe implemented API capabilities. Generic endpoints and output mapping already exist; do not rebuild them.
- [Complex value objects](simplified-value-objects-03-complex-value-objects.md) establishes domain-owned raw DTOs, strict factories and trusted `Rehydrate`. Reuse those contracts without equating API input, persistence and output mapping.
- The [source-generation draft](simplified-value-objects-07-source-generation.md) is an optional VO-adapter proposal, not an existing aggregate persistence generator. The current solution lists no generator project.
- [SDK selection](../../global.json) selects .NET 10.0.100 with latest-feature roll-forward and Microsoft Testing Platform. Existing Domain/Infrastructure projects target `net10.0`; [central package management](../../Directory.Packages.props) currently includes MongoDB Driver/Bson 3.12.0 and xUnit v3, but not Roslyn or a dedicated Mongo Testcontainers package.

### Prior-review issues to characterize

| Area | Baseline problem | Required outcome |
|---|---|---|
| Storage | Employee/member records mirror domain state and contain VOs; default aggregate registration can persist domain types | Recursively plain records, explicit typed restoration; generated common path; no silent direct fallback |
| Queries | Read repository loads documents then compiles predicates; `Query` exposes in-memory `IQueryable`; sorting/paging/select run in memory | Server filter/sort/page/count/select with tenant/deleted scope and bounded translation |
| Input | Employee factories/mutations take envelopes, actions and versions; repeat VO validation and DTO conversion | Validated shared editable input, separate create-only fields/policies, safe metadata capture |
| Mutation | Mutable contact lists, manual member callbacks/version gates; mandatory update skips a null old value | Encapsulation, validate-all-before-mutate, corrective updates, actual-change audit/tag semantics |
| Events | Per-feature lifecycle subclasses/factories capture request services, execute commands and know projections | Framework lifecycle snapshots of resulting state, no services, typed projection handlers |
| History | Timestamp-only replay; delete replay empty; scanner recognizes immediate base subclasses only | Per-aggregate sequence, explicit delete state, stable registrations and old history adapters |
| Durability | Dispatch precedes event insert, aggregate write follows; Sample does not opt into transactions | One aggregate/event/outbox commit before external effects; fail-fast durable mode |

## Binding design decisions (proposed)

1. **Four distinct models:** domain aggregate/VOs, external ClientApi contracts/envelopes, validated command inputs, and storage/event DTOs. A shared shape does not make their trust boundaries interchangeable.
2. **Plain storage is recursive.** Use CLR primitives, `string`, `Guid`, approved date/time types, nullable scalars and lists/nested plain storage DTOs. No VOs, typed IDs/tags, domain roots/members, resources/envelopes or services anywhere in stored graphs. “Record” means POCO storage model, not necessarily the C# `record` keyword. A typed generic base/marker may retain an aggregate type argument as metadata only.
3. **Generate mirrors, not domain rules.** Explicit mappings establish the runtime contract first; source generation then emits raw DTOs, typed mapping and bounded query metadata from declared persisted state. Manual POCO records/mappings remain supported for legacy layouts. Domain classes do not inherit records, expose public setters or require BSON attributes.
4. **No aggregate BSON mapping on the normal path.** Registration resolves a generated or explicit record and mapping, or fails. Preserve old direct aggregate persistence only through a separately named, obsolete opt-in legacy route.
5. **Prefer a deliberate repository breaking change over misleading LINQ.** Replace sync/`IQueryable` read execution with cancellation-aware async operations over a bounded query description. Translate supported domain expressions through storage metadata; unsupported expressions fail before database execution, never compile locally. A typed raw-record escape hatch remains scoped.
6. **Domain validation remains authoritative.** Request shape/schema hints help clients, not replace factories/rules. Shared validated editable values feed create and update; version-specific writable scopes and create-only data remain distinct. No whole-aggregate validation on a scoped update.
7. **Events describe committed results, not requests.** Standard lifecycle payloads contain frozen, data-only result snapshots and captured context. Framework orchestration applies mutations once; replay does not call command factories or resolve request services. Custom business events and external event APIs remain opt-in.
8. **Durable means transactional.** Aggregate, lifecycle event and outbox rows commit atomically with expected tenant/id/tag and uniqueness constraints. External notification delivery is at least once, not exactly once. Explicit non-durable development mode is not a production fallback.
9. **Compatibility is explicit.** BSON codecs, stored schema names, old discriminators, API versions, no-op behavior, missing/null policies and delete visibility have separate compatibility tests. Event schema version is not API version.

## Proposed project boundaries

| Touchpoint | Planned ownership |
|---|---|
| [Domain](../../src/Synonms.Structur.Domain/) | Change-tracking helpers, domain lifecycle abstractions, projection handler contracts; no Mongo dependency |
| [Infrastructure/Persistence](../../src/Synonms.Structur.Infrastructure/Persistence/) | Raw-record validation, typed mapper/descriptor contracts, storage schema metadata |
| [Infrastructure.MongoDb](../../src/Synonms.Structur.Infrastructure.MongoDb/) | Record collections/codecs, expression translation, transactions, constraints, event registry and outbox worker |
| [Api.Server](../../src/Synonms.Structur.Api.Server/) | Feature delegate orchestration, context capture, writable representation validation, existing command/query processors |
| Proposed `src/Synonms.Structur.Generators/` | Incremental persistence generator/analyzers, with a consumer compilation fixture; not a runtime dependency |
| [Sample.Api](../../src/sample/Synonms.Structur.Sample.Api/) | Domain rules, input adapters, feature definitions, optional projection/business handlers |
| [Sample.ClientApi](../../src/sample/Synonms.Structur.Sample.ClientApi/) | Existing public contracts and schema/history hints; never persistence models |

Domain must not reference Infrastructure to use generation metadata. Put minimal persisted-state opt-in declarations in a Domain-compatible location (Domain aggregate metadata namespace); emit mappings in the consuming assembly with Infrastructure available. Provider-specific codec configuration stays outside Domain. All new API names and snippets in these plans are proposed, not existing APIs.

## Global acceptance criteria

- All sections in the plan map have ordered steps, concrete touchpoints, migration decisions and tests with a named delivery gate.
- Employee's common generated route needs **zero handwritten persistence mirror types, zero lifecycle event subclasses and zero lifecycle event factories**; domain/input/feature definitions and optional handlers are counted separately in plan 06.
- Tenant/deleted scope, stable IDs/audit/tags, JSON/Ion/forms/OpenAPI, versioning and read-only representations survive the migration.
- Unsupported queries, invalid record graphs, unknown event schemas and unsafe durability configuration fail explicitly; no client evaluation, read-repair, fabricated identity or uncommitted external effect.
- No claims of completed implementation, tests, trimming/AOT, arbitrary CLR query support or exactly-once delivery are implied by these drafts.

## Principal risks and approval points

- Storage shape drift can make apparently simple raw records incompatible; require golden BSON and live-query fixtures before cutover.
- Generating private-state access can create a trust bypass; generation must target explicit persisted state and trusted restoration only, with compile-time diagnostics.
- Snapshot events increase history size and contain sensitive state; preserve access control/retention, avoid payload logging, and review historical data exposure before external publication.
- Transactions require a replica set/sharded topology and compatible deployment settings. Existing duplicates or ambiguous old event ordering can block migration; do not silently repair them.
- Approve the repository and event breaking changes, canonical name length of 100 for new writes, no-op event suppression, and projection tombstone semantics before implementation. Detailed decisions and rollback gates follow in the companion plans.
