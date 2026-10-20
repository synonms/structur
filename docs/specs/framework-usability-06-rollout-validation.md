# Framework usability 06: framework-first rollout and validation

**Status:** Draft for approval. No implementation, builds, tests, database migrations or commits have been performed by this planning task.  
**Navigation:** [Overview](framework-usability-01-overview.md) · [Persistence/querying](framework-usability-02-persistence-querying.md) · [CRUD/events/projections](framework-usability-03-crud-events-projections.md) · [Durable delivery](framework-usability-04-durable-delivery.md) · [Input/mutations](framework-usability-05-input-mutations.md).

## 1. Ordered delivery gates

Each gate is an implementation slice, not a user story. Do not delete old paths before its replacement and migration fixtures pass. Gates A–G are framework-first, using minimal framework fixtures; Sample migration is gate H.

| Gate | Ordered implementation work | Exit evidence |
|---|---|---|
| A — Characterization and approval | Verify prior-review baseline; inventory BSON/queries/callers, Employee rules/version masks, stored event schemas/order, handler effects, transaction deployment and duplicates. Approve breaking APIs, names/no-op/delete policies. | Reviewed compatibility inventory and old BSON/event fixtures; each unresolved historical ambiguity has an owner/blocker |
| B — Explicit raw storage | Add raw record/descriptor/mapper contracts, graph validation, codecs/presence policies, manual root/member fixture and explicit restoration. Isolate obsolete direct route. | Plain graph enforcement; old/new BSON parity; IDs/audit/tags and legacy read policies preserved |
| C — Async server query boundary | Add bounded query description/translator and typed raw escape hatch, enforce scope, migrate framework callers, remove misleading `IQueryable`/sync execution. | Rendered-filter tests plus actual Mongo server filter/sort/page/count/select and unsupported-query diagnostics |
| D — Input/mutation foundation | Fix null-old mandatory update; introduce semantic change detection, prepare/apply and member ID/change-scope contracts; prove version-scoped API fixture. | Accumulated faults before mutation; no-op stability, corrective legacy update and late-member failure tests |
| E — Bounded storage generation | Generate DTOs/capture/restore/query descriptors from explicit state; diagnostics, analyzer packaging and manual override fixture. | Deterministic compile/package tests, nested raw graph checks, no computed-field leakage; manual path remains viable |
| F — Feature/lifecycle/projection infrastructure | Add exact-pair definitions/context capture, frozen result events, stable schema registry, legacy readers, pure typed handlers and sequence-based history API. | Framework CRUD fixture with no lifecycle subclasses/factory, old/new replay parity, explicit unknown/ambiguous history failures |
| G — Durable operation pipeline | Shared lazy session, conditional writes/counters/event/outbox commit; classify handlers; leases/retries/idempotency; topology/index readiness and uniqueness strategy. | Replica-set failure/concurrency/worker tests; no pre-commit external effects or NoOp downgrade |
| H — Sample and deployment migration | Migrate Employee then Employment, generated storage and definitions, projection handlers, NI index/backfill, event history adapters; enable supported transactional hosting. | Sample full contract parity and live end-to-end commit/replay/delivery; schema/index/history cutover signed off |
| I — Guides and release | Update instructions/skills/guides/readme in implementation change; document breaking APIs, deployment and operations. Remove obsolete sample boilerplate after compatibility fixtures. | Measured authoring counts, working relative links/examples, release migration checklist and tested rollback boundary |

Dependencies: C and D can proceed independently after the appropriate B contracts; E requires stable B and mapped-query metadata from C; F requires B/D and uses E for the generated common path; G requires C/F write/event contracts. No Sample simplification can be considered complete while E or G is deferred.

## 2. Targeted validation matrix

Use the existing xUnit v3/Microsoft Testing Platform conventions and Central Package Management. Add the smallest useful fixtures, not a new test framework. Existing [framework integration tests](../../tests/Synonms.Structur.Tests.Integration/) include driver/BSON/mapping tests without a live database; those cannot demonstrate server query or transaction correctness.

| Concern | Project/fixture | Required observations |
|---|---|---|
| Domain behavior | [Domain.Tests.Unit](../../tests/Synonms.Structur.Domain.Tests.Unit/) | Null-old corrective update, structural VO/list equality, no-op audit/tag, accumulated scoped faults and atomic member prepare/apply |
| API/input definitions | [Api.Server.Tests.Unit](../../tests/Synonms.Structur.Api.Server.Tests.Unit/) | Exact writable pair, read-only denial, version masks, metadata/ID capture, duplicate override diagnostics, one context snapshot |
| Storage codecs/restoration | Framework Tests.Integration, driver-only fixtures | Old/new BSON golden names/types/null/presence, raw graph validation, legacy representable-invalid/malformed handling, no read writes or regenerated metadata |
| Generator | Proposed generator unit/consumer compile fixtures wired into solution | Deterministic outputs, partial/accessibility/nullability/root/member/VO/list cases, computed exclusion, stable names, all diagnostic IDs, manual override and packaged analyzer reference |
| Query execution | Framework Tests.Integration, **live Mongo replica set** | Filter/sort/skip/take/count/select in captured server commands; bounded data transfer; representative index/explain; no client evaluation; same-element list semantics |
| Query security | Same live fixture | Every Any/Find/List/Count/Select/raw read and history query enforces tenant/deleted scope, including OR predicates, IDs, missing tenant and unauthorized include-deleted attempts |
| Events/history | Framework driver/registry fixtures plus live history reads | Closed generics and old discriminators, unknown schema errors, result snapshots deep-frozen, no request DI during replay, schema/API versions separate |
| Projections | Framework pure handler tests + Sample integration | Ordered sequences with ties in timestamps, pages/high-water mark, deterministic FullName, tombstone/deleted history behavior, no dispatch during replay |
| Atomic durability | Live replica-set fixture | Failure before/after each aggregate/event/counter/outbox write and commit; rollback leaves none, commit exposes all; transient/unknown commit outcomes reconciled |
| Concurrency and constraints | Same live fixture | Two stale-tag writers: one success; same-tenant active NI collision: one success; cross-tenant/deleted/null/missing keys obey constraint; sequences unique/monotonic |
| Delivery | Live DB + deterministic fake recipient | Restart after commit; lease expiry/steal; stale lease cannot ack; crash after effect causes deduplicable retry; poison visibility/backoff; optional stream ordering |
| Cancellation/lifetime | Unit plus live session/worker fixtures | Abort/dispose before commit, uncertainty during commit, no parallel session operations, worker scope independent of request actor/tenant providers |
| Public API regression | [Sample.Tests.Integration](../../src/sample/Synonms.Structur.Sample.Tests.Integration/) | JSON/Ion shapes, nested IDs/links, forms, OpenAPI/history attrs, create/update/delete/read, multiple representations/read-only summary, versioned scoped updates |

### Live Mongo fixture requirements

Add a dedicated MongoDB Testcontainers package centrally only if needed; existing WireMock.Net.Testcontainers is for external API simulation and does not supply proof of a transaction-capable Mongo deployment. Pin a Mongo server image compatible with deployment. Initialize a single-node replica set, wait for primary/transaction readiness and verify a real transaction before running tests. Reuse the fixture within a collection where safe; isolate databases/tenant IDs and never depend on execution order. Use unique fixture indexes matching production.

Server-query tests must insert enough distractor tenants/deleted rows and sorted values to detect collection scans caused by client materialization. Inspect command filters/projections and transferred row counts; latency alone is not evidence. Query tests include `.Value`, typed ID/tag equality, known conversions/VO equality, nested members/lists, nullable/date/enum values, codec-normalized constants and unsupported methods/complex equality.

Use coordinated competing tasks/barriers for concurrency tests rather than probabilistic sleeps. Failure injection can wrap repository/transaction boundaries or use controlled driver/server failpoints where supported; keep those privileges confined to the test fixture. Outbox tests inject a clock/recipient and crash boundaries deterministically.

Run focused project tests and builds at each implementation gate using the current [developer commands](../../.agents/resources/developer-commands.md) and actual `.slnx`/MTP conventions. Do not copy stale `.sln` parsing warnings from old specs without reproducing them. This documentation task runs no tests.

## 3. Compatibility and migration checklist

### Storage/query migration

1. Export/backup representative documents and catalogue raw shapes/missing policies before enabling generated records. Validate manual record graphs and generated output at startup/compile time.
2. Preserve stable field names/codecs first. If a field or codec must change, ship explicit legacy read adapter plus versioned migration; do not rely on raw serializer defaults or read-repair.
3. Migrate repository call sites: existing predicate-based async operations→bounded query descriptions with cancellation; synchronous `Query`/LINQ chains→async bounded query/Select/Count operations; impossible domain predicates→explicit scoped record query or redesigned query. Retain scoped identity lookup.
4. Make aggregate→collection registration require mapping. Existing direct consumers choose the obsolete named legacy route consciously until converted; no automatic legacy fallback.
5. Compare old/new aggregate result state, query result sets and BSON fixtures before cutover. Do not independently dual-write two layouts.

### Event/sequence migration

1. Inventory old discriminator/schema/context availability. Keep alias readers after deleting feature classes; serialization identity cannot depend on those files.
2. Adapt old request payloads only where result state is reconstructible with historical semantics. Missing actor/member IDs or ambiguous ordering blocks conversion rather than producing invented values.
3. Pause/drain writers or lock each aggregate stream during sequence backfill; checkpoint conversion and verify no duplicates/gaps. Resume counters from verified high-water marks.
4. Rebuild projections in isolation from side effects; compare expected historical/final state including deleted aggregates. Approve the tombstone behavior change.
5. Write only the new canonical schema after the compatibility gate; keep old reads for the documented retention window. Unknown schemas remain errors.

### Uniqueness and durable deployment

1. Preflight active per-tenant NI duplicates, normalization differences, missing/null keys and deleted marker interpretation; resolve conflicts explicitly.
2. Backfill raw canonical keys/active marker only through an approved idempotent migration; verify partial unique-index behavior against the actual server.
3. Deploy topology/session support, new indexes/registry and worker code before enabling writers. Durable startup/readiness must verify them and reject unsupported standalone deployments.
4. Classify existing handlers: invariant work moves into the transaction; notifications move to outbox after commit. External integrations retain opt-in subscriptions and idempotency keys.
5. Drain old in-flight requests/dispatch before switching command orchestration. Enable durable mode explicitly in Sample hosting and deployment docs; no silent NoOp.
6. Monitor transaction failures/conflicts/unknown outcomes, outbox depth/oldest age/retries/dead letters, blocked ordered streams, legacy decode diagnostics and uniqueness violations. Do not expose NI/snapshot payloads in metrics or logs.

### Breaking-change release notes required

- Normal Mongo registration now requires a raw record mapping; direct aggregate persistence is obsolete explicit legacy behavior.
- Domain-facing sync/`IQueryable` read APIs are removed in favor of bounded async execution. No arbitrary CLR expression or client fallback is supported.
- Standard lifecycle events are data, not command executors/projection dispatchers; factory and replay extension points move to feature definitions and typed handlers. Legacy external subscriptions get an explicit adapter rather than forced custom event types.
- Public mutable collections/factory envelope signatures change; callers use validated mutation paths.
- New Employee names cap at 100, legacy unrelated edits stay permitted, no-op updates retain tag and emit no event, and deleted projections become hidden/tombstoned.
- Durable mode requires a supported Mongo deployment/indexes and idempotent external handlers; notifications are asynchronous/at least once.

### Rollback boundaries

Before new writers: roll back application configuration after verifying no new-schema data exists. After new-schema commits: use a forward-compatible reader/registry release or an explicit tested reverse migration; do not deploy an old binary that cannot read new event schemas/raw layouts. Preserve counters, events, outbox rows, handler idempotency keys and migration checkpoints. A rollback must never restore pre-commit notification dispatch. Stop writers/workers separately when diagnosing unknown commits; reconcile rather than duplicate commands.

## 4. Measure the common-path simplification

Record baseline and final handwritten artifact counts for Employee, separate from generated outputs:

| Artifact | Supplied baseline to verify | Target after migration |
|---|---|---|
| Storage mirror types | EmployeeRecord + EmployeeEqualOpportunitiesRecord; Employment has two further mirrors | **0 handwritten** mirrors for generated Employee and Employment common paths |
| Lifecycle event subclasses | Employee create/update/delete classes | **0** per feature; framework closes registered lifecycle types |
| Lifecycle factories | EmployeeDomainEventFactory (plus Employment equivalent to inventory) | **0** per standard CRUD feature |
| Feature wiring | Existing registrations/factory DI | One explicit feature definition per writable aggregate/resource pair |
| Editable validation/conversion | Repeated create/update blocks | One shared editable validator and raw adapter per representation, with separate create-only rules |

Count these retained/optional artifacts explicitly, not as a failure to reach zero: domain aggregate/member types; VO validation/rehydration rules and exceptional codecs; feature input adapters/definitions; custom business events; projection handlers and shared name calculation; external/invariant handlers; manual records/mappings for intentional legacy layouts. Report totals and why each exception remains. Generated files do not count as handwritten boilerplate, but their graph/size/diagnostics still require review.

## 5. Documentation and skills to update during implementation

Do not edit these as part of this planning change. At gate I update:

- [README](../../readme.md): supported setup, plain-record registration/generation, bounded async queries, durable hosting/worker and links to guides.
- [Resource contracts guide](../resource-contracts.md) and related mapping specs: preserve ClientApi/envelope/output boundaries; explain prepared input and feature definitions without suggesting reverse output mapping.
- [Aggregate-root skill](../../.agents/skills/new-aggregate-root/SKILL.md) and [member skill](../../.agents/skills/new-aggregate-member/SKILL.md): replace stale Resource/ChildResource inheritance/factory/event instructions, describe explicit stored state and owned collections.
- [Event-handler skill](../../.agents/skills/new-domain-event-handlers/SKILL.md), [projection skill](../../.agents/skills/new-projection/SKILL.md) and [feature skill](../../.agents/skills/new-sample-feature/SKILL.md): lifecycle definitions, pure replay, transaction/external classification, no empty subclasses/factories.
- [Value-object skill](../../.agents/skills/new-value-object/SKILL.md) and [integration-test skill](../../.agents/skills/new-integration-tests/SKILL.md): strict command vs trusted restore, raw storage codecs, generator/manual escapes and actual live replica-set fixtures.
- [Overview resource](../../.agents/resources/structur-overview.md), persistence/serializer specs and deployment/operations guidance: new commit/outbox flow and migration details. Amend old draft status only after implementation evidence exists.

Preserve all unrelated edits, especially the pre-existing changes to `AGENTS.md` and `.agents/resources/developer-commands.md`. A future need to correct command guidance must be an explicit, separately reviewed edit, not a side effect of this plan.

## 6. Documentation completion and approval

The planning deliverable consists of these six linked drafts plus targeted supersession notes in the two earlier persistence specs. Relative companion links and repository paths are the documentation check; code execution is unnecessary here. Approval of this plan authorizes none of the database migrations automatically: each gate still needs its stated evidence and an explicit cutover decision.
