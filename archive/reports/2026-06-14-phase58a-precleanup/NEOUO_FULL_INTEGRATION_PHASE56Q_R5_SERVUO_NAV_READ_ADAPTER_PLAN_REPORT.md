# NEOUO FULL INTEGRATION — PHASE56Q-R5-SERVUO-NAV-READ-ADAPTER-PLAN REPORT

## A. Header
- phase name: `PHASE 56Q-R5-SERVUO-NAV-READ-ADAPTER-PLAN`
- selected agent/session identity: `ultima-online` / `Ultima Online (ultima-online) / main`
- workspace path: `C:\.openclaw\workspace-ultima-online`
- target repo path: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- branch: `neo/staging-aigm`
- HEAD: `326ac784110bae1e89f747e3a11798d6fbffa726`
- latest commit: `326ac78 feat: add UMG navigation snapshot model`
- build result:
  - command: `dotnet build .\ServUO.sln -v:minimal`
  - result: **Build succeeded**
  - errors: **0**
  - warnings: **0**
- warning count/files:
  - none reproduced in this baseline run
  - known recurring unrelated warning files remain the accepted reference set:
    - `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
    - `Scripts\Gumps\AIGMResponseGump.cs`
    - `Scripts\Gumps\AIGMQuestionGump.cs`
    - `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

## B. Current navigation stack summary
Current navigation stack is now sufficient for a first real read-adapter plan:
- `IUMGNavigationAdapter` — read-only question boundary
- `UMGNavigationProbeResult` — passive probe/range/LOS result DTO
- `UMGNavigationRegionResult` — passive region result DTO
- `UMGNavigationEntitySnapshot` — passive nearby entity summary DTO
- `UMGNavigationNoOpAdapter` — safe no-op baseline implementation
- `UMGNavigationSnapshot` — passive composed navigation state model

Current shape:
- contract exists
- no-op adapter exists
- passive snapshot exists
- no real ServUO read adapter exists yet
- no pathfinding is wired into AIGM navigation yet
- no landmark registry exists yet
- no progress/stuck monitor exists yet
- no live movement authority exists yet

## C. Inputs inspected
### Reports inspected
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_CAPABILITY_INVENTORY_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_ADAPTER_CONTRACT_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_ADAPTER_NOOP_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_SNAPSHOT_MODEL_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_SNAPSHOT_MODEL_P_COMMIT_REPORT.md`

### Navigation files inspected
- `Scripts/Custom/AIGM/Navigation/IUMGNavigationAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationProbeResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationRegionResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationEntitySnapshot.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationNoOpAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationSnapshot.cs`

### ServUO / NeoUO files inspected
- `Server/Map.cs`
- `Server/Region.cs`
- `Server/Geometry.cs`
- `Server/Mobile.cs`
- `Scripts/Mobiles/Normal/BaseCreature.cs`
- `Scripts/Mobiles/NPCs/BaseHire.cs`
- `Scripts/Mobiles/AI/BaseAI.cs`
- `Scripts/Skills/Tracking.cs`
- `Scripts/Services/Pathing/MovementPath.cs`
- `Scripts/Custom/AIGM/AIGMSceneScanner.cs`

### Preserved Dev / existing pathing references inspected
In current target repo pathing stack:
- `Scripts/Services/Pathing/FastAStarAlgorithm.cs`
- `Scripts/Services/Pathing/SlowAStarAlgorithm.cs`
- `Scripts/Services/Pathing/Movement.cs`
- `Scripts/Services/Pathing/FastMovement.cs`
- `Scripts/Services/Pathing/MovementPath.cs`
- `Scripts/Services/Pathing/PathFollower.cs`

## D. ServUO read primitive inventory
### Coordinate / point primitives
Located in `Server/Geometry.cs`:
- `Point2D`
- `Point3D`
- constructors from `IPoint2D` / `IPoint3D`
- simple value semantics suitable for DTO composition

Inventory value:
- safe coordinate normalization surface for adapter inputs/outputs
- no mutation authority by itself

### Map primitives
Located in `Server/Map.cs`:
- `GetAverageZ(int x, int y)`
- `CanFit(Point3D p, int height, ...)`
- `CanFit(int x, int y, int z, int height, bool checkBlocksFit, bool checkMobiles, bool requireSurface)`
- `LineOfSight(Point3D org, Point3D dest)`
- `LineOfSight(Mobile from, Point3D target)`
- `LineOfSight(Mobile from, Mobile to)`
- `GetMobilesInRange(Point3D p, int range)`
- `GetItemsInRange(Point3D p, int range)`
- `GetClientsInRange(Point3D p, int range)`

Inventory value:
- enough to support first real read-only region/probe/range/LOS/entity-summary slice
- adapter should wrap them narrowly instead of exposing raw engine usage to cognition/router layers

### Region primitives
Located in `Server/Region.cs`:
- `Region.Find(Point3D p, Map map)`
- `Contains(Point3D p)`
- `GetRegion(Type)`
- `GetRegion(string)`
- `IsPartOf(Type)`
- `IsPartOf<T>()`
- `IsPartOf(string)`
- region hierarchy through `Parent`

Inventory value:
- enough for first passive region-awareness slice
- supports region name + parent-region name + guarded/town/dungeon-style classification via type/name checks in a later adapter implementation

### Range primitives
Located in `Server/Mobile.cs`:
- `GetDistanceToSqrt(Point3D p)`
- `GetDistanceToSqrt(Mobile m)`
- `GetDistanceToSqrt(IPoint2D p)`
- `InRange(...)` usage is pervasive on `Mobile`

Inventory value:
- enough for passive bounded range checks without live movement or pathing
- for first adapter slice, raw coordinate delta / `Utility.InRange` or `GetDistanceToSqrt`-equivalent logic is sufficient

### LOS primitives
Located in `Server/Mobile.cs` and `Server/Map.cs`:
- `Mobile.InLOS(Point3D target)`
- `Mobile.InLOS(Mobile target)`
- `Map.LineOfSight(...)`

Inventory value:
- enough for passive LOS questions
- first adapter should prefer map/point based LOS rather than live-mobile dependent overloads when possible

### Nearby entity primitives
Located in `Server/Map.cs` and already used in `Scripts/Custom/AIGM/AIGMSceneScanner.cs`:
- `GetMobilesInRange(Point3D p, int range)` returns `IPooledEnumerable<Mobile>`
- `GetItemsInRange(Point3D p, int range)` returns `IPooledEnumerable<Item>`
- pooled enumerables require `.Free()` discipline

Inventory value:
- enough for passive nearby summaries
- existing AIGM scene scanner already demonstrates bounded summary extraction without mutation

### Tracking-related perception primitives
Located in `Scripts/Skills/Tracking.cs`:
- `from.GetMobilesInRange(range)`
- tracking-specific filtering and sorting
- tracking arrow / target selection behavior exists but trends toward gameplay/UI behavior rather than pure adapter-level read concerns

Inventory value:
- useful later as perception input or tracking-report layer
- not needed for first ServUO navigation read adapter slice

### Pathing primitives
Located in `Scripts/Services/Pathing/MovementPath.cs` and related files:
- `MovementPath(Mobile m, Point3D goal)`
- A* algorithm selection
- path `Success`
- directional path output

Inventory value:
- valuable later for route planning
- too execution-adjacent / heavyweight for first read-only adapter slice

## E. Safe / deferred / forbidden classification table
### A. Safe read-only candidates for first adapter
1. **Actor coordinate read**
   - source: `Mobile.Location`, `Point3D`
   - classification: **A**
   - reason: direct passive position read

2. **Map / facet name read**
   - source: `Mobile.Map`, `Map.Name`
   - classification: **A**
   - reason: low-risk identity surface

3. **Point DTO conversion**
   - source: `Point2D`, `Point3D`, `Geometry.cs`
   - classification: **A**
   - reason: pure data conversion

4. **Region lookup**
   - source: `Region.Find(Point3D, Map)`
   - classification: **A**
   - reason: direct passive semantic location read

5. **Point passability probe**
   - source: `Map.CanFit(...)`
   - classification: **A**
   - reason: bounded passive probe if called on one point at a time

6. **Average surface Z normalization**
   - source: `Map.GetAverageZ(...)`
   - classification: **A**
   - reason: safe probe helper for future adapter internals

7. **Bounded nearby mobile scan**
   - source: `Map.GetMobilesInRange(Point3D, int)`
   - classification: **A**
   - reason: safe if range capped, result count capped, summaries only, pooled enumerable freed

8. **Bounded nearby item scan**
   - source: `Map.GetItemsInRange(Point3D, int)`
   - classification: **A**
   - reason: same bounded-summary rule as mobiles

9. **LOS check by coordinates / map**
   - source: `Map.LineOfSight(...)`
   - classification: **A**
   - reason: passive geometric question, safe if bounded

10. **Distance / range check**
   - source: `Mobile.GetDistanceToSqrt(...)` or coordinate math
   - classification: **A**
   - reason: passive relation check only

### B. Safe later but not first slice
1. **Tracking report integration**
   - source: `Tracking.cs`
   - classification: **B**
   - reason: useful later, but first adapter should stay purely spatial/navigation-oriented

2. **Pathing / route candidate generation**
   - source: `MovementPath`, A*
   - classification: **B**
   - reason: should wait until read adapter, snapshot service, and progress model exist

3. **Landmark resolution**
   - source: later semantic layer over region / coordinate space
   - classification: **B**
   - reason: needs separate model / registry / heuristic rules

### C. Risky due to performance
1. **Repeated `CanFit` fan-out over many tiles per think**
   - classification: **C**
   - risk: probe storms if caller loops indiscriminately

2. **Unbounded `GetMobilesInRange` / `GetItemsInRange`**
   - classification: **C**
   - risk: broad rectangle scans and heavy pooled enumeration churn

3. **Frequent LOS fan-outs**
   - classification: **C**
   - risk: expensive repeated geometry checks if caller asks many target points repeatedly

4. **Whole-map or large-radius semantic scanning**
   - classification: **C**
   - risk: not acceptable in companion cognition loop

### D. Risky due to live object exposure
1. **Returning live `Mobile` references**
   - classification: **D**
   - risk: adapter boundary leakage into authority / combat / control behavior

2. **Returning live `Item` references**
   - classification: **D**
   - risk: same leakage concern; summary DTOs are safer

3. **Depending on `BaseCreature`, `BaseHire`, `BaseAI` inside navigation contracts**
   - classification: **D**
   - risk: ties read adapter to movement/control surfaces

### E. Mutation / authority surfaces — forbidden for read adapter
1. `MoveToWorld`
2. `Location =`
3. `Direction =`
4. `ControlTarget`
5. `ControlOrder`
6. `Combatant`
7. `CantWalk`
8. `Home`
9. `RangeHome`
10. AI timers / path followers that move or steer
11. `AIGMCompanionStateAccess` as authority bridge

Classification: **E**
Reason: all of these cross from reading into control/execution/authority.

### F. Unclear / needs deeper review
1. **Map.CanSpawnMobile(...) as a probe primitive**
   - classification: **F**
   - reason: likely useful, but current contract is framed around passive point probe semantics and `CanFit` already covers first slice better

2. **Type-based town/dungeon/guarded classification details**
   - classification: **F**
   - reason: exact region type mapping should be reviewed carefully so `RegionKind`, `IsTown`, `IsDungeon`, `IsGuarded` remain stable and meaningful

## F. Proposed read adapter design
### Proposed class name
Primary recommendation:
- `UMGServUONavigationAdapter`

Reason:
- explicit about engine binding
- consistent with existing `UMG*` naming
- avoids ambiguous global `ServUONavigationAdapter`

### Interface to implement
- `IUMGNavigationAdapter`

### Namespace
- `Server.Custom.AIGM`

### Method-by-method implementation plan
#### 1. `ProbePoint(string mapName, int x, int y, int z, string reason = null)`
**Primitive(s) to use**
- map resolution from `mapName` to `Map`
- `Map.CanFit(x, y, z, 16, false, true, true)` or equivalent chosen height policy
- optionally `Map.GetAverageZ(x, y)` only if needed for detail or fallback interpretation

**Inputs**
- `mapName`
- `x`, `y`, `z`
- optional `reason`

**Null / invalid handling**
- blank map name -> `Failure(..., "map_name_missing", ...)`
- unknown map -> `Failure(..., "map_not_found", ...)`
- invalid / out-of-bounds coordinate -> `Failure(..., "point_out_of_bounds", ...)`

**Return DTO helper**
- passable point -> `UMGNavigationProbeResult.Passable(...)`
- blocked point -> `UMGNavigationProbeResult.Blocked(...)`
- invalid input -> `UMGNavigationProbeResult.Failure(...)`

**Suggested failure / reason strings**
- `map_name_missing`
- `map_not_found`
- `point_out_of_bounds`
- `point_passable`
- `point_blocked`

**Performance constraints**
- single-point only
- no neighborhood search
- no multi-point fallback loops in first slice

**What must not happen**
- no movement
- no `MoveToWorld`
- no object mutation
- no pathfinding

#### 2. `GetRegionAt(string mapName, int x, int y, int z)`
**Primitive(s) to use**
- map resolution from `mapName`
- `Region.Find(new Point3D(x, y, z), map)`
- `region.Parent`
- `region.IsPartOf(...)` / region type checks as safely available

**Inputs**
- `mapName`, `x`, `y`, `z`

**Null / invalid handling**
- blank map name -> `Failure(..., "map_name_missing", ...)`
- unknown map -> `Failure(..., "map_not_found", ...)`
- if region is null or default and uninteresting -> `Unknown(...)` or `Found(...)` depending on final semantics choice

**Return DTO helper**
- found region -> `UMGNavigationRegionResult.Found(...)`
- not classifiable -> `UMGNavigationRegionResult.Unknown(...)`
- invalid input -> `UMGNavigationRegionResult.Failure(...)`

**Suggested reason strings**
- `map_name_missing`
- `map_not_found`
- `region_found`
- `region_default`
- `region_unknown`

**Performance constraints**
- one direct lookup only
- no large semantic scans

**What must not happen**
- no region-trigger mutation
- no travel/path logic
- no use of region hooks such as move-entry behavior for decision-making

#### 3. `CheckRange(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int maxRange)`
**Primitive(s) to use**
- map resolution
- coordinate delta / `Utility.InRange`-style logic or lightweight distance calculation
- no need for live actor object requirement

**Inputs**
- source and target coordinates
- `maxRange`

**Null / invalid handling**
- blank map -> `Failure(..., "map_name_missing", ...)`
- unknown map -> `Failure(..., "map_not_found", ...)`
- negative range -> `Failure(..., "range_invalid", ...)`

**Return DTO helper**
- in range -> `UMGNavigationProbeResult.InRange(...)`
- out of range -> `UMGNavigationProbeResult.OutOfRange(...)`

**Suggested reason strings**
- `range_invalid`
- `range_in`
- `range_out`

**Performance constraints**
- constant-time coordinate comparison only

**What must not happen**
- no mobile creation / lookup requirement
- no pathfinding

#### 4. `CheckLineOfSight(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ)`
**Primitive(s) to use**
- map resolution
- `Map.LineOfSight(Point3D, Point3D)`

**Inputs**
- source and destination coordinates

**Null / invalid handling**
- blank map -> `Failure(..., "map_name_missing", ...)`
- unknown map -> `Failure(..., "map_not_found", ...)`
- out-of-bounds point(s) -> `Failure(..., "point_out_of_bounds", ...)`

**Return DTO helper**
- LOS true -> `UMGNavigationProbeResult.LineOfSight(...)`
- LOS false -> `UMGNavigationProbeResult.NoLineOfSight(...)`

**Suggested reason strings**
- `los_true`
- `los_blocked`
- `map_name_missing`
- `map_not_found`

**Performance constraints**
- one LOS check per call
- no LOS fan-out in adapter

**What must not happen**
- no use of live `Mobile.InLOS(...)` if a point-based map call is enough
- no movement or path stepping

#### 5. `GetNearbyEntities(string mapName, int x, int y, int z, int range, int maxResults)`
**Primitive(s) to use**
- map resolution
- `Map.GetMobilesInRange(new Point3D(x, y, z), range)`
- `Map.GetItemsInRange(new Point3D(x, y, z), range)`
- optional distance calculation by coordinates
- pooled enumerable `.Free()` discipline

**Inputs**
- location
- bounded `range`
- bounded `maxResults`

**Null / invalid handling**
- blank map -> empty result preferred or failure-by-logging policy; current interface returns enumerable, so return empty collection for invalid input is safer operationally
- unknown map -> empty collection
- negative range / maxResults <= 0 -> empty collection

**Return DTO mapping rules**
- produce `UMGNavigationEntitySnapshot` only
- no live `Mobile` / `Item` references
- set:
  - `EntityId`
  - `EntityKind`
  - `DisplayName`
  - `MapName`
  - `X/Y/Z`
  - `Distance`
  - `IsPlayer`
  - `IsCreature`
  - `IsItem`
  - `IsDeleted`
  - `SeenUtc`
- `IsHostile` should be conservative in first slice; if hostile classification is not clean without authority leakage, default false or classify by narrow safe heuristics only

**Performance constraints**
- enforce max input range
- enforce max output count
- stop early once cap reached
- always free pooled enumerables
- do not scan whole map

**What must not happen**
- no live object return
- no unbounded enumeration
- no movement / combat / control reads that imply authority

### Contract sufficiency assessment
Current contract is sufficient for first real adapter implementation.

Why:
- it covers point probe, region awareness, range, LOS, and nearby summaries
- it does not force live object exposure
- snapshot model can be composed on top

Possible later expansion candidates (not needed now):
- a richer point probe result with surface Z / adjusted Z
- explicit obstacle kind / tile kind
- explicit capped entity result metadata (truncated / total seen)
- landmark query surface

## G. Snapshot composition recommendation
### Question
Should snapshot filling live inside the read adapter?

### Option A — adapter directly creates snapshots
Pros:
- fewer layers initially

Cons:
- mixes low-level ServUO read primitives with higher-level composition policy
- makes testing and evolution harder
- encourages adapter bloat
- risks coupling snapshot semantics to one engine implementation

### Option B — separate snapshot composer/service
Pros:
- keeps adapter low-level and read-only
- clearer separation between primitive reads and cognition-facing state assembly
- easier to test composition logic separately
- better place to add rate limiting, caller context, and future landmark integration

Cons:
- one extra class/layer

### Recommendation
**Prefer Option B.**

Recommendation:
- keep `UMGServUONavigationAdapter` low-level and read-only
- add a later `UMGNavigationSnapshotService` (or similarly named composer) that calls adapter methods and assembles `UMGNavigationSnapshot`

Reason:
- it preserves the strongest boundary between engine reads and cognition-facing snapshot composition
- it avoids turning the adapter into a hidden orchestration layer

### Recommended future phase tied to this decision
- `Phase 56Q-R5-NAV-SNAPSHOT-SERVICE-MODEL`

## H. Performance rules
The future read adapter must obey all of these:
1. **No whole-map scans**
2. **No unbounded mobile/item queries**
3. **No pathfinding inside the read adapter**
4. **Range queries must have max range and max result caps**
5. **Repeated navigation reads should be rate-limited by caller/service, not solved by spammy adapter polling**
6. **Adapter returns summaries, not live object references**
7. **Entity snapshots must not expose live `Mobile` / `Item` references**
8. **Always free pooled enumerables**
9. **Fail closed on invalid map / point / range**
10. **Prefer point-based LOS and region reads over broad neighborhood fan-out**
11. **No implicit fallback loops across many nearby tiles in the first slice**
12. **If output is truncated by `maxResults`, do not silently continue scanning far beyond the cap**

## I. Authority rules
These rules remain hard constraints:
- read adapter may **read**, not move
- read adapter may **summarize**, not decide
- router remains movement authority for dry-run decision shaping
- tracking remains perception/report only
- `StateAccess` remains deferred and cannot own movement
- UMG remains cognition/proposal, not ServUO mutation
- future movement adapter is separate from navigation read adapter
- `BaseHire`, `BaseCreature`, `BaseAI`, and `Mobile` control/combat fields must not leak into the navigation contract layer as authority surfaces

## J. Cross-repo alignment notes
### NeoUO code repo
Later likely files:
- `UMGServUONavigationAdapter.cs`
- `UMGNavigationSnapshotService.cs`
- later landmark/progress/stuck models
- later tests or build validation artifacts for adapter behavior

### NeoUO wiki / NL repo
Later documentation should describe:
- navigation architecture
- read adapter vs snapshot service vs movement executor boundaries
- operator rules for dry-run vs live
- why tracking stays perception-only

### UO UMG repo
Later UMG surfaces may need:
- navigation snapshot block
- region-awareness block
- landmark semantics
- route planning block
- progress/stuck monitoring block
- gate/blocked/probe semantics

### IR glyph lane
Later glyph formalization may need symbols for:
- region
- landmark
- probe
- blocked
- stuck
- dry-run
- live execution
- tracking scan/report
- pursuit
- possibly a navigation snapshot grouping symbol

No cross-repo updates were performed in this phase.

## K. Recommended next phase
### Primary recommended next phase
**`Phase 56Q-R5-SERVUO-NAV-READ-ADAPTER-REGION-PROBE`**

Justification:
- current contract is sufficient
- `Region.Find(...)` and `Map.CanFit(...)` are clearly present and straightforward
- this gives the first real adapter value while staying narrow and low risk
- it avoids premature entity scanning complexity and avoids pathing
- it proves the engine-binding layer can answer real passive questions without authority leakage

Scope for that next implementation phase should be only:
- add `UMGServUONavigationAdapter.cs`
- implement only:
  - `ProbePoint(...)`
  - `GetRegionAt(...)`
- leave:
  - `CheckRange(...)`
  - `CheckLineOfSight(...)`
  - `GetNearbyEntities(...)`
  in safe conservative or deferred form only if the phase explicitly allows it

### Alternate next phase
**`Phase 56Q-R5-NAV-SNAPSHOT-SERVICE-MODEL`**

Use this instead if the team wants composition architecture locked before the first real adapter implementation.

### Suggested commit message if next phase is code-bearing
- `feat: add ServUO navigation region and point probe adapter`

## L. No-mutation confirmation
- no C# files changed: **confirmed**
- no existing files changed: **confirmed**
- no new code files added: **confirmed**
- no ServUO reads implemented: **confirmed**
- no pathfinding implemented: **confirmed**
- no movement implemented: **confirmed**
- no world/control mutation introduced: **confirmed**
- no StateAccess copied/referenced: **confirmed**
- no BaseHire/Mobile dependency introduced into AIGM navigation contracts: **confirmed**
- no parser/skill/companion/action/command files changed: **confirmed**
- no OpenClaw main workspace touched: **confirmed**
- no wiki/NL repo changed: **confirmed**
- no UO UMG repo changed: **confirmed**
- no IR glyph work changed: **confirmed**
- no subagents used: **confirmed**
- no commit occurred: **confirmed**

## Operator summary
### Top findings
1. The current contract is already good enough for a first real ServUO-bound read adapter.
2. `Region.Find(...)`, `Map.CanFit(...)`, `Map.LineOfSight(...)`, `GetMobilesInRange(...)`, `GetItemsInRange(...)`, `Point2D/Point3D`, and distance helpers provide the needed read-only substrate.
3. The main risk is not missing primitives — it is live-object / authority leakage through `Mobile`, `BaseCreature`, `BaseHire`, `BaseAI`, and pathing surfaces.
4. The existing `AIGMSceneScanner` is a useful proof pattern for bounded passive entity summarization.
5. Snapshot composition should remain a separate service, not be pushed into the adapter.

### Blockers
No hard blocker prevents a first real read adapter.

Meaning:
- first real read adapter appears **safe** if kept narrow
- the safe first slice is **region + point probe only**
- nearby entity / LOS / broader range implementation should follow after the first engine-binding slice proves clean
