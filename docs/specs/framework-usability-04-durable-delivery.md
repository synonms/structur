# Framework usability 04: transactional writes and durable delivery

**Status:** Draft for approval; proposed behavior, not a durability claim.  
**Dependencies:** [Typed storage and queries](framework-usability-02-persistence-querying.md), [lifecycle envelopes](framework-usability-03-crud-events-projections.md) and [prepared mutations](framework-usability-05-input-mutations.md). Deployment/test gates: [plan 06](framework-usability-06-rollout-validation.md).

## 1. Modes and unit-of-work boundary

The supplied review reports that Sample does not enable `WithTransactionSupport`, the default is `NoOpDomainTransaction`, `MongoDomainTransaction` starts a transaction at construction, and event handlers run before event insertion with aggregate persistence later. Merely moving dispatch after insertion would still leave aggregate/event divergence.

**Proposed default for the new CRUD pipeline:** durable Mongo mode requires working session/transaction support. At startup verify configuration/topology and required indexes; expose readiness failure for unavailable durable capability. Do not start a transaction/session in a DI constructor. Lazily open a short-lived operation unit of work at the commit boundary, dispose/abort deterministically, and never share its session concurrently.

An explicit `UseNonDurableDevelopmentMode()` may retain a best-effort path for standalone local Mongo. It must be visibly logged, health-labelled non-durable, documented as unsuitable for durability tests/production, and incompatible with enabling the durable outbox guarantee. A configured durable deployment must never downgrade to NoOp on error.

Proposed unit-of-work API shape:

```text
BeginAsync(capturedContext, cancellation)
CommitChangeAsync(candidateRecord, expectedTag, eventEnvelope, outboxEntries, cancellation)
CommitAsync(cancellation) -> committed / conflict / indeterminate / failed
```

Exact types must follow existing `Result`/fault conventions. The unit of work, not individual repositories, owns session and commit. Every participating repository write must enlist that same session; startup/tests must catch an un-enlisted repository. Do not add a generic cross-database transaction abstraction.

## 2. Atomic write protocol

1. Outside the short transaction, prepare/validate the input and async friendly policies with cancellation; create a private candidate and freeze result snapshot/context. No external effect is allowed.
2. Begin a session transaction. Reuse captured operation/event identities across safe retries; do not regenerate actor, member IDs or timestamps.
3. For update/delete, conditionally replace/update using **tenant + aggregate ID + expected EntityTag + expected deletion state**. A zero matched count maps to the existing concurrency/not-found fault convention without cross-tenant existence probing. Creation uses tenant-scoped identity and database uniqueness constraints.
4. Allocate the next per-aggregate sequence inside the same transaction. A proposed counter row keyed by tenant/aggregate-type/id is incremented atomically; initialize it from the migration high-water mark. Failed transactions roll back allocation. A unique event index on `(TenantId, AggregateType, AggregateId, Sequence)` prevents duplicates; timestamp is descriptive only.
5. Insert the data-only event and one outbox delivery row per eligible external handler/subscription. Include records for custom events when the operation explicitly emits them; allocate their order deterministically within the transaction.
6. Execute explicitly registered transactional invariant handlers in a stable configured order, on the same session and sequentially. They may write internal database state, not perform HTTP/email/queue publication. Expected failures abort all writes. If they need to influence root result state, incorporate that work before snapshot capture rather than mutate it after the snapshot.
7. Commit aggregate, event, counters and outbox atomically. Only after acknowledged commit may normal response/after-commit external dispatch proceed. A worker wake-up signal is best effort; the durable rows are the source of truth.

Order steps 3–6 consistently to avoid lock-order surprises; all are in one transaction so no external observer sees partial success. Hard delete is out of scope; standard delete is a tagged/audited soft-delete mutation.

Avoid `Task.WhenAll` over session operations/transactional handlers. Bound transaction time/size, especially with full snapshots. Cancellation before commit aborts safely; cancellation during commit can leave an unknown result and must not be reported as a proven rollback.

### Retry and uncertain commit

Use the Mongo driver's documented transient transaction/unknown-commit handling with bounded backoff. Retry commit of the same transaction where the driver requires it; rerun an operation only when safe and deterministic. No retry loop may re-invoke external effects or mutate a caller-owned aggregate twice.

Persist unique `EventId`/operation correlation inside the transaction and reconcile indeterminate commits by tenant-scoped durable lookup using a fresh session. If status remains unknown, surface an explicit retryable/indeterminate operational fault with correlation, not a misleading “nothing changed” response. Do not silently reapply a command against a new tag. Repeated HTTP commands are not automatically idempotent; opt-in idempotency keys need a tenant-scoped stored result if offered.

## 3. Database uniqueness, not just existence prechecks

The NI existence precheck in `EmployeeDomainEventFactory` is a friendly error opportunity only. Concurrent requests require a database constraint aligned with read/deleted semantics.

Proposed Employee constraint: unique `(TenantId, NationalInsuranceNumberKey)` for active rows with a populated NI value. The key is a raw normalized string captured using the authoritative domain normalization; query/policy operands use the identical normalization. If the existing stored field is already canonical and compatible, index it directly instead of adding a redundant key. Do not change normalization silently.

- Add an explicit raw active/deletion marker if needed for a supported partial index. Old missing/null audit fields must be migrated to the same active interpretation before enabling the constraint.
- Define partial-filter BSON precisely using operators supported by MongoDB/Driver 3.12.0: include only the explicit active marker and populated supported string key. Do not assume a unique nullable index ignores multiple missing/null values. Verify absent/null/empty NI and old audit shapes in a live database before choosing the final index declaration.
- Delete removes a row from the active constraint atomically; a subsequent create can reuse the NI if that matches current deleted-read semantics. Updates that change NI and any future undelete operation must obey the same constraint.
- Translate duplicate-key failures by known index identity into the established NI/domain conflict fault; preserve unrelated database errors as failures. Do not log the sensitive NI value.
- Rollout must inventory duplicates **per tenant among active records**, decide normalization, resolve duplicates with operator approval, backfill keys/active markers, verify deleted/null cases, then build/verify the unique index before new writers become ready. Never auto-delete duplicates.

## 4. Outbox worker: at-least-once delivery

Proposed plain outbox row fields: `DeliveryId`, `EventId`, tenant/aggregate identity, sequence, stable handler/subscription identity+version, event schema reference or owned payload, status, attempt count, next-attempt time, lease owner/token/expiry, delivered time and sanitized last error category. All are raw types. Keep event/outbox retention coordinated; a worker must not dereference a deleted event.

Worker algorithm:

1. Poll eligible rows using an index on status/next-attempt/lease expiry. Acquire a bounded lease with a single conditional atomic update; use server-compatible time handling and a unique lease token.
2. Resolve the registered handler version and frozen event data using a worker-owned DI scope. No `HttpContext`, `IUserActionProvider`, current request tenant or `IVersionContext` is required.
3. Execute external delivery with an idempotency key derived from delivery/event+handler identity. Pass captured tenant context explicitly. A recipient supporting deduplication should store/accept that key.
4. Mark delivered only with a condition matching tenant/delivery/lease token. On failure, persist attempt/backoff/error category; renew a lease carefully for bounded long work. Expired leases are reclaimable.
5. After a bounded retry policy, retain a visible failed/dead-letter state with authorized inspection/retry tooling and alerts. Manual retries preserve the same delivery identity.

A crash after the external effect but before acknowledgement produces duplicates: delivery is **at least once**. A worker cannot guarantee exactly once for arbitrary external systems. Handler idempotency or an inbox/recipient deduplication mechanism is required for effective-once effects.

A global worker may scan tenants under a privileged service capability, but every claim/update/load/delivery remains bound to the row's tenant and lease. Request-supplied tenant values cannot select worker data. Inspection/retry APIs must be separately authorized and tenant scoped.

### Ordering and handler lifetimes

Default outbox delivery is independent per handler. A subscription that requires ordered aggregate processing opts into a `(tenant, aggregate, handler)` stream: claim only the lowest undelivered sequence for that stream, block later entries on failure, and expose the blocked stream operationally. Do not promise global ordering. Transactional handler order is explicit and deterministic; external handler ordering is not DI registration order.

Create a fresh scoped handler instance per delivery/batch with defined bounds; singleton workers never capture scoped repositories/request contexts. Projection replay is a separate pure path and never dispatches either transactional or external handlers.

## 5. Touchpoints and ordered implementation

| Existing area | Planned change |
|---|---|
| [MongoDomainTransaction](../../src/Synonms.Structur.Infrastructure.MongoDb/Transactions/MongoDomainTransaction.cs) and [Domain transactions](../../src/Synonms.Structur.Domain/Transactions/) | Explicit async operation scope, lazy session, outcomes/disposal, no constructor side effects |
| [MongoDbBuilder](../../src/Synonms.Structur.Infrastructure.MongoDb/Hosting/MongoDbBuilder.cs) / hosting | Fail-fast durable registration, topology/index readiness, explicit development mode |
| [MongoDbWriteAggregateRepository](../../src/Synonms.Structur.Infrastructure.MongoDb/Aggregates/MongoDbWriteAggregateRepository.cs) | Scoped conditional writes, duplicate-index classification, shared session |
| [MongoDbDomainEventRepository](../../src/Synonms.Structur.Infrastructure.MongoDb/Events/MongoDbDomainEventRepository.cs) | Persist without pre-insert external dispatch; sequence/registry support |
| Proposed Mongo `Outbox/` and hosted worker | Plain delivery records, leases, retries, metrics and authorized operations |
| [Command processors](../../src/Synonms.Structur.Api.Server/Mediation/Commands/) | One commit boundary; distinguish invariant and external registrations |
| [Sample.Api](../../src/sample/Synonms.Structur.Sample.Api/) Program/hosting | Explicit durable mode and supported Mongo topology after framework gates |

Implement in this order: shared session/conditional-write fixture → atomic aggregate/event/counter writes → invariant-handler boundary → outbox insertion/worker → uniqueness rollout/readiness → failure/retry/uncertain-commit tests → Sample enablement. Classify existing Employee handlers as transactional or external before moving them; no automatic “all handlers transactional” conversion.

**Acceptance:** Failed writes/handlers leave no aggregate/event/outbox divergence; committed events do not precede aggregate visibility. Concurrent stale-tag writers yield one winner and the established conflict fault. Concurrent active same-tenant NI creates/updates yield one winner by index; other tenants and deleted reuse obey policy. Restarting workers recovers delivery, lease expiry cannot permit stale acknowledgement, and duplicate delivery is observable/idempotent. Durable mode refuses unsupported topology instead of silently using NoOp.

**Targeted live validation:** Use a real replica-set fixture for abort/commit, concurrent tags/uniqueness, sequence allocation, transaction retry and unknown commit reconciliation. Inject crashes after insert, before commit, after commit, after effect and before acknowledgement. Verify cancellation/disposal and no concurrent use of a session. Driver-only serializer tests are not evidence for these properties.

**Compatibility/risks:** Production deployments need replica-set support, transaction-compatible privileges/settings, indexes and worker operation. Split old handler semantics explicitly and document delayed external delivery. Operational costs include transaction contention, snapshot size, outbox retention, poison deliveries and sensitive payload access. Rollback must preserve committed event/outbox data and never return to pre-commit dispatch.
