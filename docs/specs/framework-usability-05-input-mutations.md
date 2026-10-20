# Framework usability 05: validated editable input and safe mutations

**Status:** Draft for approval; proposed contracts, not implemented.  
**Parent:** [Overview](framework-usability-01-overview.md). Used by [feature orchestration](framework-usability-03-crud-events-projections.md); persisted/replayed state follows [plan 02](framework-usability-02-persistence-querying.md).

## 1. Separate API contracts, preparation and domain state

Keep [Sample.ClientApi](../../src/sample/Synonms.Structur.Sample.ClientApi/) contracts plain and shared with clients. Retain validation/schema/history attributes, JSON/Ion wire shape, nested envelope metadata, forms/OpenAPI, versioning and multiple representations. An output mapping plan is not an input mapper: never reverse aggregate→resource mappings or use permissive `Rehydrate` to convert untrusted request data.

The supplied review identifies repeated validation and `AddressResource`→`AddressData`/contact/bank conversions in Employee create/update, manual version gates and nested callbacks, and inconsistent name lengths (create defaults to Moniker 250; update and attributes use 100).

**Proposed input pipeline:**

```text
ResourceEnvelope<TResource> + selected representation/version
    -> representation-specific raw input adapter (copy values + allowed member IDs)
    -> shared domain validation (ValidatedInstanceBuilder, accumulate faults)
    -> immutable validated editable input / prepared change plan
    -> create or scoped update, after all validation/policies succeed
```

The raw adapter explicitly copies/converts contract data into domain-owned raw DTOs such as `AddressData`, contact data and bank data. Consolidate each conversion once and reuse it for create/update; do not create a generic reflection-based bidirectional mapper. Existing optional-complex semantics (partial optional data can mean absence) remain as documented unless separately reviewed; do not silently tighten them here.

### Proposed shapes

```text
EmployeeEditableInput              // raw, domain-owned, no API envelope/services
ValidatedEmployeeEditable         // immutable validated VOs, created only by validator
EmployeeCreateOnlyInput           // raw fields editable only at creation
ValidatedEmployeeCreateOnly       // validated immutable creation values
PreparedEmployeeChange            // validated edit mask, values, member operations/IDs
MutationOutcome<TAggregate>       // result aggregate + Changed, using existing faults
```

Validated inputs are command/domain objects and **may** contain VOs; they are not storage/event DTOs and must never be persisted. Keep constructors nonpublic and prevent mutable DTO/list aliasing. Reuse shared editable validation in creation and update, rather than revalidating the same field through two divergent blocks.

Inventory actual Employee/Employment fields before classifying create-only vs editable. Classification must preserve current intended immutability; do not infer that NI, birth date or any other named field is immutable merely because it is convenient for this refactor. Actor, tenant, root IDs and audit are operation context, never user-editable values.

### Validation scope and version selection

- Resolve the applicable version once through the selected registration. Construct an explicit writable-field/member mask for that pair/version; do not let DTO defaults overwrite fields outside the mask.
- Validate all **applicable edited** values, plus invariants involving those edits. Shared validators accept the scope; a validator must not require unrelated fields omitted by an older version.
- Create validates required creation state plus editable scope under existing creation semantics. Update does not run whole-aggregate creation validation or rerun create-only async policies.
- Keep DTO/schema hints aligned with authoritative domain rules; they do not replace those rules. Proposed canonical maximum for new Employee names is **100** for both create and update, matching current resource hints. Preserve longer legacy stored names on read/unrelated scoped updates; explicit replacement names must satisfy 100.
- Unsupported versions and non-writable representations use established client faults before mutation. Preserve the currently characterized ignore/reject behavior for out-of-version input fields; add tests rather than accidentally making deserialization defaults authoritative.
- Aggregate faults through `ValidatedInstanceBuilder` using stable property/member paths and deterministic order. Do not stop at the first nested fault or convert expected rule faults into exceptions.

## 2. Validate all changes before mutating any state

Root/member mutation must be atomic from the caller's perspective, not merely eventually rolled back by the database.

1. Build all candidate scalar/complex values and collection operations without changing the root or members.
2. Capture existing member IDs from authoritative envelope metadata; validate ownership, duplicate IDs, unknown IDs and allowed add/update/remove semantics. Reject IDs belonging to another aggregate/tenant. Route root ID overrides untrusted input.
3. Preserve existing supported collection replacement/merge/delete semantics, including order if meaningful. Distinguish omitted/inapplicable collections from explicit empty collections using the version scope and current protocol rules.
4. Validate every member operation and relevant cross-field rule. An invalid second member must not leave the first changed.
5. Run applicable async policies with cancellation while state is unchanged. These policies are friendly/domain checks, not a substitute for database constraints.
6. Allocate IDs once for validated new members and freeze the prepared plan. Apply to a detached candidate/private owned state with no remaining fallible domain-validation steps; if application can still fail, discard the candidate instead of partially mutating the tracked root.
7. Compute actual change, then record one root audit/tag change per operation when needed. Pass the same captured action through all nested work.

A stateful prepare/apply contract must not allow a stale plan to be applied to a different aggregate/tag. Bind it to tenant/root/expected tag and check identity/version before application.

### Supported member mutation paths

Touch [EmployeeEqualOpportunities](../../src/sample/Synonms.Structur.Sample.Api/Features/Employees/EmployeeEqualOpportunities.cs), [Employment](../../src/sample/Synonms.Structur.Sample.Api/Features/Employments/Employment.cs) and [EmploymentContract](../../src/sample/Synonms.Structur.Sample.Api/Features/Employments/EmploymentContract.cs).

Replace manually repeated `rootUpdatedAction` plumbing with a bounded root-owned mutation scope/change collector. Member mutation returns/prepares a change indicator; root aggregates it and marks itself once. The collector is an operation-local helper, not a service retained by members or persistence records. Do not add a generic change-tracking ORM.

Expose contact/member collections through `IReadOnlyList<T>` or immutable views backed by private owned lists. Avoid exposing a mutable list through an interface that callers can cast back; return a read-only wrapper or immutable snapshot. Supported methods/merge plans own validation, equality, ordering and audit; no public `List<T>` bypass remains. Copy mutable input and output DTO collections. Trusted restore populates owned backing state without invoking user mutation rules.

## 3. Structural equality, no-op updates and corrective legacy edits

Use exact VO semantic equality (type and value), including existing complex equality and ordered sequence equality where order is meaningful. Never compare fresh DTO/list instances by reference. For identity-bearing members, compare stable IDs plus editable state; preserve an existing member identity when values are unchanged. For unordered business collections, specify canonical/order-insensitive equality explicitly rather than silently sorting.

Proposed root helper outcome is an internal change indicator compatible with public `Maybe<Fault>` mutation conventions. `Changed == false` means:

- no root/member updated audit or new EntityTag;
- no persistence write, update lifecycle event, sequence allocation or outbox entry;
- the HTTP response uses the existing resource/tag under the current successful-update status convention.

This is an intentional behavior change if callers currently expect an event/tag for every PUT. Document it and test separately from a stale-tag no-op: expected-tag validation still applies, so “same values” cannot bypass concurrency checks.

Directly fix the coupled null-old-value defect in [AggregateRoot](../../src/Synonms.Structur.Domain/Aggregates/AggregateRoot.cs): `UpdateMandatoryValue` must not silently skip assigning a valid new mandatory value when legacy state has a null old value. Apply valid replacement, count it as a change and mark audit/tag once. Equal non-null values remain no-op; optional null→null remains no-op. Do not treat current create validation as a prerequisite for loading or correcting old state.

An unrelated scoped update to a legacy aggregate with a missing mandatory property must still work according to its explicit rehydration policy; correcting that property must validate the replacement and persist it. No read-repair, sentinel fabrication or whole-aggregate validation is introduced.

## 4. Touchpoints and ordered implementation

| Area | Planned work |
|---|---|
| [Domain aggregates](../../src/Synonms.Structur.Domain/Aggregates/) | Null-old mandatory fix, actual-change reporting, root-owned change scope and encapsulated collection guidance |
| [Domain validation](../../src/Synonms.Structur.Domain/Validation/) | Reusable scope-aware validated-input pattern using existing builder/fault accumulation |
| [Api.Server commands](../../src/Synonms.Structur.Api.Server/Mediation/Commands/) | Select/capture representation context, prepare first, apply once to candidate |
| [Employee](../../src/sample/Synonms.Structur.Sample.Api/Features/Employees/Employee.cs) | Shared editable validation, create-only separation, raw DTO adapter reuse, contact encapsulation |
| [Employee/Employment features](../../src/sample/Synonms.Structur.Sample.Api/Features/) | Member preparation/merge plans and stable IDs, removing manual callbacks |
| [ClientApi features](../../src/sample/Synonms.Structur.Sample.ClientApi/Features/) | Preserve public contracts; align hints/history only for approved rule changes |

Implementation order:

1. Characterize current Employee field/version/optional/collection semantics and error paths; approve the new-write 100-character name limit.
2. Add framework tests and minimal helpers for null-old replacement, semantic no-op tracking and prepare/apply atomicity using a small domain fixture.
3. Define scope-aware prepared input and member-operation contracts; integrate feature delegate orchestration with an API fixture before Sample conversion.
4. Migrate shared Employee raw conversions and validated editable construction, keeping create-only inputs and async policies separate.
5. Migrate nested members/Employment collection plans and read-only collection exposure; remove old callback/list bypass paths only after parity fixtures.
6. Capture actual result state through plan 03; verify event/audit/tag stability and codec restoration together.

## 5. Acceptance, validation and compatibility

**Acceptance:** Create/update share one editable validation path and one raw adapter per representation. All applicable scalar/member faults are returned before any mutation. A failed late member/policy leaves root, members, lists, audit and tag unchanged. IDs are stable through round trip/replay; new IDs allocate once. Unchanged structural state produces no persistence/event/audit changes. Legacy null mandatory state can be explicitly corrected. No form/OpenAPI/wire regression or read-only write route is introduced.

**Targeted validation matrix:**

| Layer | Cases |
|---|---|
| Domain unit | create/update name lengths 100/101, fault accumulation, scalar/complex/list equality, late-member failure atomicity, corrective null-old assignment, optional null transitions, one root tag change |
| API Server unit | exact pair/version mask, omitted vs explicit empty, nested/duplicate/foreign IDs, one actor/version capture, no reverse output mapping, cancellation before apply |
| Framework integration | restored legacy null/long values + unrelated scoped update; corrective valid replacement; failed replacement leaves state unchanged; no-op has no event/outbox |
| Sample integration | Employee + equal-opportunities + Employment contracts, old/new versions, default/Ion nested metadata, forms/OpenAPI hints, read-only summary denied writes |

**Breaking changes:** Public mutable-list access disappears; callers must use validated mutation paths. Domain factory signatures move away from API envelopes to prepared/domain inputs; temporary internal adapters may preserve a migration step but are not persisted. Create now rejects 101–250-character names; old stored values remain loadable. No-op PUT no longer generates activity/tag changes. Record/event storage contracts remain recursively raw even though validated command objects contain VOs.

**Risks:** Over-generalizing input preparation can obscure business rules; keep helpers small and feature-specific. A writable scope that mistakes default values for edits can erase data. Prepared plans must be bound to identity/tag, nested IDs must be captured before discarding envelopes, and custom equality must be explicit rather than inferred from storage DTO equality.
