# NEOUO FULL INTEGRATION — PHASE56Q-R5-NAV-SNAPSHOT-SERVICE-PLAN REPORT

## Selected agent / session identity
- selected agent: `ultima-online`
- selected session: `Ultima Online (ultima-online) / main`
- subagents used: **no**

## Workspace path
`C:\.openclaw\workspace-ultima-online`

## Target repo path
`C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`

## Branch / HEAD / latest commit
- branch: `neo/staging-aigm`
- HEAD: `c332dda7ba7b7962205980dcba4d8f09b189bad3`
- latest commit: `c332dda feat: add ServUO navigation region/probe adapter`

## Baseline build result
Command:
- `dotnet build .\ServUO.sln -v:minimal`

Result:
- Build succeeded
- `0 Warning(s)`
- `0 Error(s)`

## Files / reports inspected
### Reports inspected
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_CAPABILITY_INVENTORY_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_ADAPTER_CONTRACT_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_ADAPTER_NOOP_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_SNAPSHOT_MODEL_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_SERVUO_NAV_READ_ADAPTER_PLAN_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_SERVUO_NAV_READ_ADAPTER_REGION_PROBE_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_SERVUO_NAV_READ_ADAPTER_REGION_PROBE_P_COMMIT_REPORT.md`

### Navigation files inspected
- `Scripts/Custom/AIGM/Navigation/IUMGNavigationAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationProbeResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationRegionResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationEntitySnapshot.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationNoOpAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationSnapshot.cs`
- `Scripts/Custom/AIGM/Navigation/UMGServUONavigationAdapter.cs`

## Current navigation stack summary
The current stack now has the right layering to justify a snapshot-composition service plan:
- `IUMGNavigationAdapter` — passive read-only question boundary
- `UMGNavigationNoOpAdapter` — safe fallback adapter implementation
- `UMGServUONavigationAdapter` — first real read-only engine-bound implementation
- `UMGNavigationProbeResult` — passive point/range/LOS result
- `UMGNavigationRegionResult` — passive region result
- `UMGNavigationEntitySnapshot` — passive nearby entity summary DTO
- `UMGNavigationSnapshot` — passive composed navigation state model

Current capabilities:
- `ProbePoint(...)` is real and read-only
- `GetRegionAt(...)` is real and read-only
- `CheckRange(...)` remains deferred/no-op
- `CheckLineOfSight(...)` remains deferred/no-op
- `GetNearbyEntities(...)` remains deferred/empty
- movement authority remains separate
- snapshot model already exists and is ready to be composed into

## Snapshot service purpose
The future Navigation Snapshot Service should be the composition layer between:
- low-level navigation adapter questions
- passive request context from companion/cognition/router middleware
- `UMGNavigationSnapshot` as the unified perception/state surface

Its purpose is **not** to read ServUO directly and **not** to decide movement.
It should:
- accept passive actor identity + coordinate/map input
- invoke the adapter for read-only facts
- assemble a snapshot object
- return a service-level result with success/no-op/deferred semantics

## Recommended service architecture
### Recommended concepts
Evaluate and likely add later:
- `IUMGNavigationSnapshotService`
- `UMGNavigationSnapshotService`
- `UMGNavigationSnapshotRequest`
- `UMGNavigationSnapshotResult`

### Recommended architecture answer
The service should:
- depend on **`IUMGNavigationAdapter`**, not directly on ServUO engine types
- compose a base `UMGNavigationSnapshot` from string identity + map/coordinates
- attach `PositionProbe` and `Region` results using snapshot helper methods or direct assignment
- leave nearby entities empty unless explicitly requested and later supported
- leave landmark/destination placeholders passive until those models exist

### Service shape recommendation
Preferred shape:
1. interface first
2. no-op implementation second
3. real composition implementation third

That mirrors the safe pattern already used in the navigation adapter lane:
- contract
- no-op
- real engine-bound implementation

## Dependency model
### Should the service depend on `IUMGNavigationAdapter`?
**Yes.**

Reason:
- keeps the service generic and testable
- prevents direct ServUO coupling
- preserves the engine-bound adapter as the only layer that knows map/region engine mechanics

### Should the service default to `UMGNavigationNoOpAdapter` or require injection?
**Require injection as the primary design.**

Secondary note:
- a parameterless or convenience constructor may later default to `UMGNavigationNoOpAdapter` only if that matches established local style and clearly signals no-op behavior
- but the main architecture should prefer explicit dependency injection

Reason:
- explicit injection makes wiring visible
- avoids hidden fallback behavior
- keeps testing deterministic

### Should the service be allowed to know about `UMGServUONavigationAdapter` directly?
**No.**

Reason:
- that would collapse the abstraction boundary we just created
- service should know the adapter contract, not the concrete ServUO engine-binding class

### Should actor identity be string-only at first?
**Yes.**

Recommended first-shape actor identity fields:
- `ActorId`
- `ActorProfileKey`

Reason:
- preserves passive composition semantics
- avoids live actor object leakage
- keeps service callable from non-engine contexts later

### Should live actor object references remain outside the service for now?
**Yes.**

Reason:
- no `Mobile`, `BaseCreature`, `BaseHire`, `BaseAI` inside the service
- keeps service from becoming a stealth authority surface

### Should snapshot creation use `UMGNavigationSnapshot.FromPosition(...)` and then attach probe/region results?
**Yes.**

Recommended base flow:
- create snapshot with `FromPosition(...)`
- attach region with `WithRegion(...)`
- attach probe with `WithProbe(...)`
- assign request-driven reason/source/detail
- keep placeholders untouched unless a later feature explicitly fills them

### Should nearby entity filling be deferred until `GetNearbyEntities(...)` is real?
**Yes.**

Reason:
- current adapter intentionally returns empty/no-op for nearby entities
- snapshot service should not invent entities or simulate a scan

### Should landmark/destination fields remain placeholders until landmark model exists?
**Yes.**

Reason:
- no landmark registry/model yet
- no destination model beyond passive placeholder fields
- snapshot service should preserve placeholders rather than fabricate semantics

## Request / result model proposal
### Proposed future `UMGNavigationSnapshotRequest`
Recommended fields:
- `string RequestId`
- `string Source`
- `string Reason`
- `string ActorId`
- `string ActorProfileKey`
- `string MapName`
- `int X`
- `int Y`
- `int Z`
- `bool IncludeNearbyEntities`
- `int NearbyRange`
- `int MaxNearbyResults`
- `DateTime CreatedUtc`

Notes:
- actor identity remains string/profile-key based
- no live actor references
- `IncludeNearbyEntities` should remain safely ignorable until entity scanning is implemented later

### Proposed future `UMGNavigationSnapshotResult`
Recommended fields:
- `bool Succeeded`
- `bool WasNoOp`
- `bool WasDenied`
- `string Reason`
- `string Detail`
- `UMGNavigationSnapshot Snapshot`
- `DateTime CompletedUtc`

Notes:
- `WasNoOp` is useful when the service is wired to a no-op adapter
- `WasDenied` leaves room for future safety gating or caller policy refusal without treating all deferrals as generic failures

## Composition flow
### Proposed first composition flow
1. receive `UMGNavigationSnapshotRequest`
2. validate minimal request shape:
   - map name present
   - coordinates acceptable enough to avoid exceptions
3. create base snapshot via `UMGNavigationSnapshot.FromPosition(...)`
4. set request-driven fields:
   - `Source`
   - `Reason`
   - `ActorId`
   - `ActorProfileKey`
5. call `IUMGNavigationAdapter.ProbePoint(...)`
6. call `IUMGNavigationAdapter.GetRegionAt(...)`
7. attach probe/region results to snapshot
8. if `IncludeNearbyEntities` is true but entity scanning is still deferred:
   - leave `NearbyEntities` empty
   - optionally mark `Detail`/result reason to reflect deferred entity fill policy
9. leave landmark/destination placeholders unchanged
10. return `UMGNavigationSnapshotResult`

### First real composition target
The first real service implementation should only compose:
- base position
- probe result
- region result

It should explicitly defer:
- nearby entities
- landmarks
- destinations
- range/LOS enrichment
- progress/stuck state

## What remains deferred
The snapshot service plan intentionally leaves these out for now:
- real nearby entity composition
- landmark resolution / nearest landmark fill
- destination semantics beyond placeholders
- range/LOS enrichment into snapshot-level composition
- progress/stuck monitor integration
- tracking-report integration
- pursuit/movement/route ownership

## Primary next phase recommendation
**Primary: `Phase 56Q-R5-NAV-SNAPSHOT-SERVICE-CONTRACT`**

Why:
- this keeps the safe pattern intact: contract → no-op → real composition
- request/result/service interface should be stabilized before adding behavior
- it keeps the new service boundary crisp before implementation details arrive
- it prevents rushing a composition class that later needs reshaping

Expected scope of that phase:
- add `IUMGNavigationSnapshotService`
- add `UMGNavigationSnapshotRequest`
- add `UMGNavigationSnapshotResult`
- no composition implementation yet

Suggested later commit message if code-bearing:
- `feat: add navigation snapshot service contract`

## Alternate next phase
**Alternate: `Phase 56Q-R5-LANDMARK-MODEL`**

Why:
- if the team wants semantic place-awareness before service composition grows, a dedicated landmark model can clarify how snapshot placeholders should be filled later

## Authority rules
The future snapshot service must obey all of these:
- snapshot service composes data only
- read adapter reads only
- router remains movement authority
- movement executor remains separate
- tracking remains scan/report only
- no pursuit without explicit pursuit intent
- `StateAccess` remains deferred
- no live object references returned
- no direct `Map`, `Region`, `Mobile`, `BaseHire`, `BaseCreature`, or `BaseAI` dependency in service layer
- service must not own movement, pathfinding, combat, or control mutation

## Performance rules
The future snapshot service must obey all of these:
- no unbounded entity scans
- no map-wide scans
- no pathfinding in snapshot service
- repeated snapshot requests should be rate-limited by caller/orchestrator as needed
- entity/range/LOS expansion must remain capped and deferred until corresponding adapter methods are real
- service should compose from existing adapter calls, not fan out into extra engine reads
- no hidden polling loops

## Cross-repo alignment notes
### NeoUO code repo
Later likely additions:
- `IUMGNavigationSnapshotService.cs`
- `UMGNavigationSnapshotRequest.cs`
- `UMGNavigationSnapshotResult.cs`
- `UMGNavigationSnapshotService.cs`
- later no-op and real implementations if pattern is followed strictly

### NeoUO wiki / NL repo
Later documentation should describe:
- Navigation Snapshot Service purpose
- adapter vs service vs router vs executor boundaries
- why the snapshot service composes only passive facts
- operator expectations around deferred nearby entities / landmarks / destinations

### UO UMG repo
Later UMG surfaces may want:
- navigation snapshot block
- region awareness block
- map probe/passable-blocked block
- route state block
- stuck recovery block
- landmark/destination semantic blocks

### IR glyph lane
Later glyph formalization may need symbols for:
- navigation snapshot
- region
- probe
- blocked/passable
- unknown
- no-op
- read-only adapter

No cross-repo updates were performed in this phase.

## No-mutation confirmation
- no C# files changed: **confirmed**
- no new code files added: **confirmed**
- no ServUO reads implemented: **confirmed**
- no movement implemented: **confirmed**
- no pathfinding implemented: **confirmed**
- no world/control mutation: **confirmed**
- no StateAccess copied/referenced: **confirmed**
- no OpenClaw main workspace touched: **confirmed**
- no wiki/NL repo changed: **confirmed**
- no UO UMG repo changed: **confirmed**
- no IR glyph work changed: **confirmed**
- no subagents used: **confirmed**
- no commit occurred: **confirmed**

## Top findings
1. The snapshot service should depend on `IUMGNavigationAdapter`, not on `UMGServUONavigationAdapter` directly.
2. Actor identity should remain string/profile-key based at first.
3. Base snapshot composition should use `UMGNavigationSnapshot.FromPosition(...)` and then attach probe/region results.
4. Nearby entities, landmarks, and destination semantics should remain deferred/passive until those lanes exist.
5. The service should follow the same safety progression as the adapter lane: contract first, then no-op, then real composition.

## Blockers
No hard blocker exists.

The only real requirement is discipline:
- keep the service passive
- keep the adapter injected
- keep ServUO engine knowledge out of the service layer
