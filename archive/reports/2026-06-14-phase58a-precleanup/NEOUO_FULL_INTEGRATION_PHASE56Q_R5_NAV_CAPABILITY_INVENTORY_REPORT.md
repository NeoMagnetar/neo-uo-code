# NEOUO FULL INTEGRATION PHASE56Q-R5 NAV CAPABILITY INVENTORY REPORT

Date: 2026-06-08 12:18 -09:00
Target repo: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`

## A. Header
- phase: `PHASE 56Q-R5-NAV-CAPABILITY-INVENTORY`
- branch: `neo/staging-aigm`
- HEAD: `d6444f5813a248f0a75746622db1b46d52ab91b5`
- latest commit: `d6444f58 feat: wire UMG router to no-op movement executor`
- build result:
  - `dotnet build .\ServUO.sln -v:minimal`
  - **Build succeeded**
  - **0 Warning(s)**
  - **0 Error(s)**
- warning count/files for this run:
  - none reproduced in this run
  - known recurring unrelated files remain:
    - `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
    - `Scripts\Gumps\AIGMResponseGump.cs`
    - `Scripts\Gumps\AIGMQuestionGump.cs`
    - `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

## B. Executive summary
### Main findings
1. The codebase already contains substantial low-level ServUO navigation primitives:
   - coordinate structs (`Point2D`, `Point3D`)
   - map/tile/surface fit checks (`Map`, `CanFit`, Z helpers)
   - region lookup and containment (`Region`, `Region.Find`, `Contains`, `IsPartOf`)
   - range and line-of-sight helpers (`InRange`, `GetDistanceToSqrt`, `InLOS`)
   - AI/mobile movement surfaces in `Mobile`, `BaseCreature`, `BaseAI`, and `BaseHire`
2. The preserved Dev repo contains meaningful reusable patterns for pathing/travel/tracking, especially:
   - `Scripts\Services\Pathing\MovementPath.cs`
   - `FastAStarAlgorithm.cs`
   - `SlowAStarAlgorithm.cs`
   - `PathFollower.cs`
   - `Scripts\Skills\Tracking.cs`
   - `Scripts\Services\AIGM\PlayerAutoFollow.cs`
3. The biggest risk is not missing primitives; it is **authority leakage**:
   - direct control mutation (`ControlOrder`, `ControlTarget`, `Combatant`, `CantWalk`, `Home`, `RangeHome`)
   - implicit coupling between tracking/travel/state storage and movement execution
   - trying to reuse preserved Dev router/state code too literally
4. The current target repo already has a good dry-run ladder:
   - intent
   - bounded state
   - gate model
   - non-executing router
   - execution contract
   - no-op executor
   - router-to-no-op wiring
5. The next implementation step should remain model/adapter-oriented, not jump straight to autonomous navigation execution.

### Highest-value ServUO primitives
- `Map.CanFit(...)`
- Z/surface helpers such as `GetAverageZ`/fit-style tile evaluation
- `Region.Find(...)`, `Contains(...)`, `IsPartOf(...)`
- `InRange(...)`, `GetDistanceToSqrt(...)`
- `InLOS(...)` / line-of-sight helpers
- `MovementPath` / A* pathing classes in preserved Dev
- tracking skill target/direction logic as perception source

### Biggest navigation risks
- direct router mutation of ServUO control/combat fields
- letting tracking or travel systems quietly become movement authority
- reusing `AIGMCompanionStateAccess` as a movement shortcut
- pathing that is too heavy/noisy for frequent companion cognition loops
- lack of a clean navigation snapshot / progress / landmark model before execution logic

### Recommended next implementation phase
- **1st recommendation: `Phase 56Q-R5-NAV-ADAPTER-CONTRACT — Add read-only navigation adapter contract`**
- Reason: the inventory shows enough low-level engine capability exists, but it should be wrapped before higher-level autonomous navigation logic is modeled.

## C. Current dry-run movement stack
Already present in target:
- `Scripts/Custom/AIGM/Movement/UMGMovementIntentKind.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateKind.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateDecision.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- `Scripts/Custom/AIGM/Movement/IUMGMovementExecutor.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementExecutionRequest.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementExecutionResult.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementNoOpExecutor.cs`

Current status:
- intent exists
- gate decisions exist
- bounded movement state exists
- router exists and is gate-aware
- router supports state-only HOLD / STOP / SUSPEND / RESUME / COMBAT_INTERRUPTION / CLEAR
- execution contract exists
- no-op executor exists
- router can emit dry-run execution requests for move/follow/guard/travel/pursuit candidate intents
- no live movement yet
- no `BaseHire` / `Mobile` dependency in the execution contract
- no `StateAccess` dependency in the movement stack

## D. Map capability inventory
### Core files / locations found
Likely core locations in target ServUO tree:
- `Scripts\Map.cs`
- `Scripts\Point2D.cs`
- `Scripts\Point3D.cs`
- tile-related/core data files under the root `Scripts` layer (plus static/land/tile consumers across systems)

### Relevant Map methods / concepts found by inventory
- `CanFit(...)`
- `CanSpawnMobile(...)`
- land/static tile access patterns (`GetLandTile`, `GetStaticTiles`, land/static inspection by tile matrix/data)
- range queries:
  - `GetMobilesInRange(...)`
  - `GetItemsInRange(...)`
  - `GetClientsInRange(...)`
- region and sector relationships through map/region access
- line-of-sight / position checks via map/mobile helpers

### What Map can answer now
Map-level capability appears sufficient to answer:
- whether a coordinate exists on a facet/map
- whether a coordinate can physically fit a mobile/item
- what land/static tiles are present at or around a location
- what mobiles/items/clients are nearby
- what region/area a location falls inside, with region help

### What should be wrapped by a future navigation adapter
Should be hidden behind a read-only navigation adapter:
- fit/standability queries
- nearby-mobile / nearby-item scans
- region lookup by coordinate
- basic LOS/range checks
- tile/surface probing

### What is too low-level for UMG
Too low-level to expose directly to UMG cognition:
- raw tile matrices / raw statics collections
- direct sector traversal
- direct low-level map fit/surface loops

### What should be cached or rate-limited
- repeated region lookup on the same location
- repeated standability probing for nearby points
- repeated nearby mobile scans at short cadence
- repeated LOS fan-outs during path planning

### Risk notes
- `Map` is powerful but low-level; direct use in router cognition would sprawl quickly
- fit/surface queries need bounded call patterns to avoid excessive server load

## E. Region capability inventory
### Core file / location found
Likely region core location:
- `Scripts\Region.cs`

### Relevant Region methods / concepts found by inventory
- `Region.Find(...)`
- `Contains(...)`
- `IsPartOf(...)`
- `CurrentRegion`
- `AllowSpawn(...)`
- `OnMoveInto(...)` / movement-entry rule hooks if present
- parent/child region hierarchy patterns

### Coordinate-to-region behavior
Region support appears sufficient for:
- mapping world coordinate -> current semantic region
- checking whether a point/mobile is inside a region
- inferring parent/child regional relationships

### Semantic navigation value
Useful region semantics for companions later:
- town vs dungeon vs wilderness context
- guarded vs unguarded zone context
- house/interior/public-space context
- landmark grouping by region name/identity
- “return to owner/home region” reasoning layer

### Risk notes
- Region names/structures may not always be player-friendly landmarks
- region semantics should be normalized into a navigation snapshot rather than exposed raw to UMG

## F. Movement execution inventory
### Core movement/AI files found
- `Scripts\Mobile.cs`
- `Scripts\Mobiles\BaseCreature.cs`
- `Scripts\Mobiles\AI\BaseAI.cs`
- `Scripts\Mobiles\NPCs\BaseHire.cs`
- preserved Dev movement/travel/pathing files under `Scripts\Services\Pathing` and `Scripts\Custom\AIGM`

### Safe later adapter candidates
Potentially safe **behind an adapter** later:
- `Map` + `Point3D` validation queries
- `InRange(...)` / LOS helpers
- `MovementPath` / pathing planners for advisory routing
- carefully selected `BaseHire` / `BaseCreature` movement entry surfaces after contract design

### Dangerous / deferred mutation surfaces
These should stay deferred or tightly gated:
- `ControlTarget`
- `ControlOrder`
- `Combatant`
- `CantWalk`
- `Home`
- `RangeHome`
- direct `Location =`
- direct `MoveToWorld(...)` without adapter discipline
- direct `Direction =`
- AI mutation (`AIObject` and broad AI reassignment)

### Direct router mutation prohibitions
The router should **not** directly own:
- BaseHire/creature control field mutation
- stuck recovery teleport/relocation behavior
- combat target mutation
- hidden path-following dictionaries/timers

## G. Pathfinding / route inventory
### Existing path helpers found
Preserved Dev contains meaningful pathing stack:
- `Scripts\Services\Pathing\MovementPath.cs`
- `Scripts\Services\Pathing\PathAlgorithm.cs`
- `Scripts\Services\Pathing\FastAStarAlgorithm.cs`
- `Scripts\Services\Pathing\SlowAStarAlgorithm.cs`
- `Scripts\Services\Pathing\PathFollower.cs`
- `Scripts\Misc\Waypoints.cs`
- `Scripts\Items\Internal\Waypoint.cs`
- `Scripts\Misc\DataPath.cs`

Also relevant navigation-adjacent files:
- `Scripts\Services\AIGM\PlayerAutoFollow.cs`
- `Scripts\Services\Help\StuckMenu.cs`

### Whether target repo already has pathfinding code
- yes, ServUO/NeoUO appears to already include pathing services and waypoint concepts

### Whether preserved Dev has pathfinding code
- yes, preserved Dev explicitly exposes A* and path-follower classes

### Whether built-in pathing is usable for companions
- likely **usable later**, but only through a clean adapter/executor boundary
- not safe to pull directly into the router today

### Whether pathing appears broken/commented/unused
- inventory did not prove breakage, but preserved Dev’s autonomous movement problems likely came from orchestration/authority coupling rather than lack of raw pathing code

### Whether pathing is too heavy for frequent use
- potentially yes if invoked naively per-think/per-message
- path computation should probably be rate-limited and tied to progress monitoring

### Whether a small local probe strategy may be safer first
- yes
- a small read-only local navigation probe + bounded MOVE_TO_POINT adapter slice is safer than importing broad travel systems wholesale

## H. Tracking inventory
### Existing tracking surfaces found
- `Scripts\Skills\Tracking.cs`
- tracking gump/target code in the target/Dev trees
- preserved Dev AIGM tracking/report code via `AIGMCompanionTracking...` surfaces
- `ShipTracking.cs` and other non-companion tracking-adjacent examples

### Tracking capabilities likely available
- detect mobiles: yes
- direction output: likely yes through tracking skill mechanics
- target selection/listing: yes
- distance-ish or directional relationship: likely yes
- server-side tracked target lists/models: present in preserved Dev custom AIGM layer
- custom AIGM report model: partially implied by preserved Dev tracking/report code

### Does tracking currently move or chase?
- vanilla tracking itself appears to be perception-oriented
- preserved Dev companion tracking layer leaked toward pursuit when coupled to router/state surfaces

### Movement leakage risks
- tracking must remain perception-only
- `START_TRACKING` and `TRACKING_REPORT` must never promote themselves into movement authority
- pursuit must stay behind explicit `PURSUE_TRACKED_TARGET`

### Report-model recommendation
Future AIGM should likely introduce a dedicated read-only `AIGMTrackingReport` / tracking snapshot rather than expose raw tracking logic to router execution.

## I. Stuck/backtrack inventory
### Existing logic found
- `Scripts\Services\Help\StuckMenu.cs`
- preserved Dev path follower / travel recovery / stuck-related code patterns
- search results indicate recover/backtrack/stuck concepts in preserved Dev path/travel systems

### Why old approach likely struggled
Most likely reasons:
- travel/progress/recovery logic was too entangled with broader movement authority
- pathing, tracking, and control mutation were not sufficiently isolated
- state storage and execution authority were mixed
- “stuck” may have been treated as part of live movement logic rather than a bounded progress-monitor signal

### What can become progress-monitor input
- last accepted target point
- last movement decision timestamp
- repeated failure to reduce distance to target
- repeated unchanged position/region under active move intent
- repeated blocked/CanFit/path failures

### What should become stuck recovery planner behavior
Later, separate planner/model concepts should cover:
- route retry count
- local reprobe radius
- backtrack candidate selection
- “give up and report blocked” policy
- owner-return fallback

### What should not be copied
- any preserved Dev stuck/recovery code that drags in travel controller or direct live mutation without a clean boundary

## J. Companion / middleware integration points
### Files and abstractions inspected
- `IAIGMCompanionActor.cs`
- `AIGMCompanionIntent.cs`
- `AIGMCompanionIntentParser.cs`
- `AIGMCompanionSkillExecutor.cs`
- movement router/gate/intent/state stack

### Where navigation snapshots could plug in later
Best insertion points:
- router input / pre-routing perception layer
- companion decision middleware before execution request build
- UMG-facing state snapshot returned to cognition layer

### Where UMG cognition could receive navigation facts
A future nav snapshot should probably include:
- current coordinate/map/region
- destination/active intent
- owner position/range if available later
- nearby obstacles / standability summary
- tracking report summary
- progress / stuck state summary

### Where action proposals should be normalized
- parser should normalize natural language into intent/proposal
- router should normalize movement proposals against gates and nav snapshot
- executor should receive only validated execution requests

### Where parser / skill executor must remain non-movement-authority
- parser: request-only, never movement execution
- skill executor: ability/cooldown only, never navigation authority

### Whether companion actor currently exposes enough identity/state for navigation
- enough to continue contract/model work: yes
- not enough yet for live execution identity/adapter guarantees without further explicit navigation actor context

## K. CROSS_REPO_ALIGNMENT_MAP
### NeoUO code repo later updates
Belongs in code repo:
- navigation adapter contracts
- landmark/navigation/progress/stuck models
- read-only map/region/navigation snapshot adapters
- execution adapter/executor implementations
- router/executor integration
- tests/build reports for nav execution

### NeoUO wiki / NL repo later updates
Belongs in wiki/NL repo:
- PRD/phase summaries for autonomous navigation
- architecture diagrams for router vs executor vs adapter
- operator docs for dry-run vs live execution modes
- companion navigation behavior explanation pages
- route/landmark/tracking/stuck recovery summaries in NL

### UO UMG repo later updates
Belongs in UO UMG repo:
- movement cognition blocks
- navigation perception blocks
- tracking report blocks
- landmark resolution blocks
- progress monitor blocks
- stuck recovery blocks
- pursuit permission blocks
- owner-follow and guard behavior blocks
- IR matrix semantics for movement/open/closed gate conditions
- sleeves/stacks for navigation style and companion movement policy

## L. UMG assets needed later
### Likely assets
- `NAVIGATION.PERCEPTION.SNAPSHOT` block
- `LANDMARK.RESOLUTION` block
- `ROUTE.PLANNING` block
- `PROGRESS.MONITOR` block
- `STUCK.RECOVERY` block
- `TRACKING.REPORT` block
- `PURSUIT.PERMISSION` block
- `TACTICAL.RANGE.MAINTENANCE` block
- `OWNER.FOLLOW` block
- `GUARD.BEHAVIOR` block
- IR-matrix symbols for movement gate open/closed/deferred
- companion sleeve fields for movement style / navigation personality

### Classification
- likely already exists in some form: tracking/combat/follow semantics may partially exist
- may exist in UMG repo: route/planning/progress/stuck-related abstractions
- needs retrieval/check: landmark resolution and navigation snapshot normalization
- likely needs new block: explicit `PURSUIT.PERMISSION`, `PROGRESS.MONITOR`, and bounded `STUCK.RECOVERY` blocks tied to NeoUO companion semantics

## M. Recommended next phases
Ranked recommendations:
1. **Phase 56Q-R5-NAV-ADAPTER-CONTRACT — Add read-only navigation adapter contract**
   - reason: best next boundary after this inventory; wraps Map/Region/range/LOS primitives cleanly
2. **Phase 56Q-R5-NAV-SNAPSHOT-MODEL — Add navigation snapshot model only**
   - reason: gives UMG and router a unified read-only nav fact surface
3. **Phase 56Q-R5-LANDMARK-MODEL — Add landmark model only**
   - reason: region/coordinate context needs semantic labels before route planning becomes useful
4. **Phase 56Q-R5-PROGRESS-MODEL — Add progress/stuck model**
   - reason: old backtracking problems likely came from missing clean progress/stuck semantics
5. **Phase 56Q-R5-MOVE-TO-POINT-LIVE-PLAN — Plan first real MOVE_TO_POINT adapter slice**
   - reason: once adapter + snapshot boundaries exist, first live candidate can be planned safely

## N. Explicit no-mutation confirmation
- no C# code changed: **confirmed**
- no existing files changed: **confirmed**
- no preserved Dev files copied: **confirmed**
- no wiki repo changed: **confirmed**
- no UO UMG repo changed: **confirmed**
- no commit occurred: **confirmed**

## Final summary for operators
### Top findings
- ServUO/NeoUO already has strong low-level navigation primitives; the problem is orchestration and authority boundaries, not total capability absence.
- Preserved Dev contains reusable pathing/tracking patterns, but also unsafe authority coupling that must not be blindly imported.
- The most important missing layer now is a **read-only navigation adapter/snapshot boundary**, not more direct movement code.

### Blockers found
- no hard build blockers
- main blocker is architectural: raw engine capability exists, but autonomous navigation should not proceed until adapter/snapshot/progress models are formalized

### Repo alignment risks
- yes, moderate alignment risk if code, NL/wiki docs, and UMG assets drift independently
- future movement/navigation work should be documented intentionally across:
  - code repo (contracts/adapters/models)
  - wiki/NL repo (design + operations)
  - UMG repo (blocks/stacks/sleeves/IR semantics)
