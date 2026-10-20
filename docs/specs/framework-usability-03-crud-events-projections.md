# Framework usability 03: CRUD, lifecycle events and projection ownership

**Status:** Draft for approval; every API shape below is proposed.  
**Dependencies:** [Raw record mapping](framework-usability-02-persistence-querying.md) and [validated mutations](framework-usability-05-input-mutations.md). Commit and delivery rules are defined in [plan 04](framework-usability-04-durable-delivery.md); sequencing gates are in [plan 06](framework-usability-06-rollout-validation.md).

## 1. Feature definitions replace routine event factories

Keep generic endpoints, output mappers and repeatable resource registration. Add a proposed `FeatureDefinition<TAggregate,TResource>` for the exact registered writable pair, configured with typed creation/update/delete delegates and optional async policies. A representative **proposed pseudocode** registration:

```text
Feature<Employee, EmployeeResource>
    .Create(prepareCreate, createFromValidatedInput)
    .Update(prepareUpdate, applyValidatedChanges)
    .Delete(deleteAggregate)
    .BeforeCreateAsync(checkFriendlyUniqueness)
```

Preparation returns the validated input/change plan from plan 05 using existing `Result<T>`/fault conventions. Mutation delegates receive a captured operation context, not providers to retain in events. Async policy delegates receive cancellation and scoped repositories; they do not mutate aggregates. Support an update policy where a mutable unique field requires it, without automatically rerunning create-only policies.

Framework orchestration in [CreateResourceCommandProcessor](../../src/Synonms.Structur.Api.Server/Mediation/Commands/CreateResourceCommandProcessor.cs), [UpdateResourceCommandProcessor](../../src/Synonms.Structur.Api.Server/Mediation/Commands/UpdateResourceCommandProcessor.cs) and [DeleteResourceCommandProcessor](../../src/Synonms.Structur.Api.Server/Mediation/Commands/DeleteResourceCommandProcessor.cs) must:

1. Select the exact aggregate/resource registration; authorize operation/version as today.
2. Capture immutable tenant, actor/action timestamp, resolved API version, route root ID and nested envelope IDs **once per operation**. Copy metadata out of the envelope; never capture a live provider/envelope in the change plan or event.
3. Prepare/validate input, then run async policies with no mutation. Load/update against tenant/deleted scope and the expected tag.
4. Apply prepared changes to a private candidate aggregate. Creation allocates missing member/root IDs once; update preserves known member IDs and allocates new ones only after validation.
5. If state did not change, return the existing representation/tag without an update event, sequence increment or outbox row. Existing conditional-request rules still apply.
6. Capture result state and audit/tag into a frozen lifecycle payload. Execute the transactional commit from plan 04. No event `Apply` call is needed.
7. Return the mapped committed aggregate through the existing response/envelope/wire path.

Validate feature definitions at startup against `ResourceDirectory`: every enabled write operation needs exactly one delegate, disabled/read-only pairs must not acquire handlers/forms through feature registration, ambiguous definitions/custom overrides fail, and definition versions cannot widen the representation's allow-list. Do not introduce a second routing-registration source. Read-only Employee summary needs no mutation definition.

Keep test seams for context, policy and mutation delegates. A custom operation handler can replace one standard operation explicitly; reject two overrides. Do not select factories by service enumeration order. Existing custom event factories may survive temporarily behind an explicit legacy command adapter, not the default route.

## 2. Data-only standard lifecycle events

Replace routine `EmployeeCreatedEvent`/`EmployeeUpdatedEvent`/`EmployeeDeletedEvent` subclasses and `EmployeeDomainEventFactory` with closed generic framework lifecycle types, e.g. proposed `AggregateCreated<TSnapshot>`, `AggregateUpdated<TSnapshot>`, `AggregateDeleted<TSnapshot>`. Do not force standard events to inherit the current service-bearing `DomainEvent<T>` executable contract.

The proposed persisted envelope is recursively raw:

| Field | Meaning |
|---|---|
| `EventId: Guid` | Stable event identity/idempotency key |
| `TenantId: Guid`, `AggregateId: Guid`, `AggregateType: string` | Scope and stable logical aggregate name |
| `Sequence: long` | Unique committed per-aggregate ordering |
| `EventType: string`, `SchemaVersion: int` | Stable lifecycle/custom event identity and payload schema |
| `OccurredAt: DateTime` | Captured UTC action time, not the ordering authority |
| `Actor: UserActionRecord` | Captured actor/audit data, no provider |
| `ResolvedApiVersion: string?` | Historical command context, independent of event schema version |
| `Payload` | Registry-selected closed plain snapshot DTO; never arbitrary object graph fallback |

A serializer can hold a BSON payload internally, but the registered CLR payload graph must pass the same plain-type validator as records. Do not persist runtime `Type`, assembly-qualified generic names, domain objects, `ResourceEnvelope`, raw mutable request resources, services or delegates.

Use the record capture schema to create a result-state snapshot with plain IDs/audit/tags/nested data. Snapshot schema and storage schema may share generated DTOs at initial version, but event schema identity is pinned: changing the aggregate record must not reinterpret old event payloads automatically.

- Creation contains the validated resulting state, including allocated IDs and any normalization.
- Update contains the complete **resulting** snapshot, not only request fields. Uneditable versioned fields and legacy values remain as they actually were; invalid input never becomes event data.
- Delete contains the resulting soft-deleted snapshot with deletion audit/tag and stable member IDs. No empty delete replay.
- Capture owns all collections and nested DTOs. Runtime events expose deeply immutable snapshot views; serialization DTOs use owned copies with no setters exposed to consumers. Merely marking a list property `init` is insufficient.
- Freeze before dispatch and test that mutating original input, aggregate or serializer DTO cannot alter the event. Do not expose a mutable storage DTO instance as a live payload.

Custom business events remain opt-in with explicit stable schemas/handlers. They use the same context, sequence/commit and data-only rules, but their payloads need not be lifecycle snapshots. Preserve external event publishing as an optional subscription/adapter API; do not require every lifecycle consumer to invent a custom event. Do not automatically publish full sensitive snapshots externally.

## 3. Projection handlers own replay

Move projection dispatch out of event `Replay(Projection)` methods into proposed typed handlers:

```text
IProjectionHandler<TProjection,TEvent>
    Apply(TProjection projection, TEvent event) -> TProjection
```

Handlers are pure/deterministic and can use shared pure calculation functions, not request services, clocks, repositories or external dispatch. A projection registry maps projection/event schemas to handlers; missing required handlers are a startup/replay diagnostic rather than silent ignore. Explicitly declared irrelevant business events may be ignored.

For [Employee projections](../../src/sample/Synonms.Structur.Sample.Api/Features/Employees/Projections/):

- Implement lifecycle handlers for EmployeeSummaryProjection that derive `FullName` in one shared pure helper from result snapshot fields. Event classes must not type-check this projection or duplicate the calculation.
- Proposed delete policy: apply a tombstone (`IsDeleted`, deletion audit), retaining deterministic historical summary state. Current projection endpoints return the existing not-found/deleted convention for tombstoned aggregates; an explicitly authorized historical view may inspect the tombstone.
- Treat changing the previous empty-delete replay to hidden/tombstoned behavior as an intentional observable change. Test old and new deleted histories and update API expectations.
- Replay tenant+aggregate-scoped events ordered by `Sequence`; do not sort solely by `Timestamp`. Reject duplicate/conflicting sequences and unexplained gaps according to the migration policy, rather than guessing.
- Fetch history in bounded server-side sequence pages, with an explicit target sequence/high-water mark for consistent replay. A projection rebuild does not invoke command delegates or external handlers.

Update [GetProjectionQueryProcessor](../../src/Synonms.Structur.Api.Server/Mediation/Queries/GetProjectionQueryProcessor.cs) to use the registry, sequence pagination and current authorization/deleted policy. A request for another tenant's history must never reveal existence or data.

## 4. Registration and historical compatibility

Touch [StructurDomainClassMaps](../../src/Synonms.Structur.Infrastructure.MongoDb/Events/StructurDomainClassMaps.cs), [MongoDbDomainEventRepository](../../src/Synonms.Structur.Infrastructure.MongoDb/Events/MongoDbDomainEventRepository.cs), [Domain events](../../src/Synonms.Structur.Domain/Events/) and [Api.Server registration](../../src/Synonms.Structur.Api.Server/DependencyInjection/).

Replace immediate-`DomainEvent<>`-base scanning as the source of persisted type identity. Register every closed generated lifecycle type from the feature/storage descriptor, plus explicit custom schemas and legacy discriminator aliases. Logical names such as `employee.created` are stable across CLR renames; they are not derived from assembly-qualified generic names. Validate duplicate identities and schema-version chains at startup.

Ordered history migration:

1. Inventory stored discriminators and real payload layouts for Employee/Employment lifecycle events, including nested metadata, audit, tenant, versions and member IDs. Existing scanner limitations mean discovery is not proof of persisted coverage.
2. Register legacy discriminator readers and pure upcasters/adapters to the new canonical lifecycle envelope. Never call old `ApplyAsync`, resolve `IUserActionProvider`/`IVersionContext`, or recreate IDs during projection replay.
3. For request-shaped old events, reconstruct result snapshots only when the original stored context/history makes that deterministic. If required actor/member IDs/version/audit or mutation semantics are absent, report the exact unsupported history and block affected cutover; do not fabricate them.
4. Where necessary run an explicit offline legacy-history conversion using pinned historical semantics and available data, separate from normal reads. Capture provenance and compare reconstructed final state/projections against stored aggregates.
5. Backfill sequences under a write pause or controlled per-aggregate lock. Use a demonstrably durable old ordering key if available. Timestamp plus stable event ID is a deterministic tie-breaker **only after review confirms no causal ambiguity**; ambiguous ties require operator resolution. Record approved ordering and migration checkpoint.
6. Dual-read old/new discriminators through the registry during rollout; write new schema only after adapters pass fixtures. Unknown event names/versions fail explicitly; do not deserialize-and-ignore or apply today's factories.

Do not conflate historical representation/API version with event schema evolution. Upcasters convert schema data, not rerun current business validation. Version handlers and event adapters are independently testable.

## 5. Ordered work and acceptance

1. Add framework immutable operation context and feature-definition validation; prove one framework fixture without Sample event classes.
2. Introduce lifecycle envelope/snapshot capture and stable registry, keeping old history readers explicit.
3. Change processors to prepare→policy→apply once→snapshot→commit, and remove standard-event `Apply`/request-service ownership.
4. Introduce pure typed projection handlers and sequence-based history reads; characterize delete behavior.
5. Add legacy aliases/upcasters and conversion tooling design before retiring current event classes.
6. Migrate Sample lifecycle wiring after durable mode is proven; remove empty subclasses/factories only once old discriminator fixtures remain readable.

**Acceptance:** A feature with standard CRUD registers mutation delegates but no lifecycle classes/factory. Captured tenant/actor/version/IDs are stable and isolated per operation and representation pair. Event payloads are deep-frozen raw result state; one valid mutation executes once. No-op updates emit nothing. Pure replay produces identical summary/tombstone for old-adapted and new histories with no request DI scope and no external notifications.

**Validation:** API Server unit tests cover writable/read-only/duplicate pair validation, single context capture, policy ordering and cancellation. Framework integration fixtures cover generic event registration, discriminator aliases, unknown versions, snapshot isolation and sequence replay. Sample integration fixtures cover default/Ion, nested identity, API versions, FullName and deleted visibility. Live commit/failure behavior belongs to plan 04, not a serializer-only test.

**Breaking changes/risks:** Removing `ApplyAsync`/event-owned `Replay` from the standard path changes public extension contracts. Provide explicit legacy adapters for migration, with no implicit service deserialization; document new feature definitions and projection handlers. Snapshot size and schema coupling require payload limits/retention review. Do not erase legacy readers when deleting feature class files or assume an old event history can always be upcast losslessly.
