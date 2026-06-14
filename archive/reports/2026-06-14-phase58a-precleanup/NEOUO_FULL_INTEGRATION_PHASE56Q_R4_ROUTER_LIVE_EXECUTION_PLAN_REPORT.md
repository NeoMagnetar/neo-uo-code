# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER-LIVE-EXECUTION PLAN REPORT

Date: 2026-06-08 10:07 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `464605f0dbea20d174111b92482b1bcfe26a5f40`
- latest commit: `464605f feat: refine non-executing UMG router state transitions`

## Build result
Command:
```text
dotnet build .\ServUO.sln -v:minimal
```
Result observed in this planning phase:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
- warning count: **0**
- error count: **0**
- known warning set did not reproduce in this run
- no warning cleanup was attempted

## Current router inspected
Inspected target files:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateDecision.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateKind.cs`

### Current public router methods
- `TrySubmitIntent(...)`
- `ApplyIntentToState(...)`
- `Hold(...)`
- `Stop(...)`
- `Suspend(...)`
- `Resume(...)`
- `CombatInterruption(...)`
- `Clear()`
- `GetState()`
- `EvaluateGate(...)`
- `EvaluateIntentGate(...)`
- `EvaluateTrackingGate(...)`
- `EvaluatePursuitGate(...)`
- `EvaluateHoldGate(...)`
- `EvaluateStopGate(...)`
- `EvaluateResumeGate(...)`
- `EvaluateCombatInterruptionGate(...)`

### Current state transition capability
- active intent storage: yes
- suspended intent storage: yes
- destination metadata preservation: yes
- reason / interruption metadata: yes
- transition audit strings: yes
- live movement authorization: **no**
- live execution path: **none**

### Current intent fields/properties
From `UMGMovementIntent`:
- `Kind`
- `Requester`
- `TargetMobile`
- `DestinationName`
- `DestinationPoint`
- `DestinationMap`
- `TargetSerial`
- `TargetName`
- `Reason`

### Current gate decision properties
From `UMGMovementGateDecision`:
- `GateKind`
- `IsAllowed`
- `Reason`
- `AllowsStateUpdate`
- `AllowsLiveMovement`
- `RequiresExplicitIntent`
- `IsScanOnly`
- `IsReportOnly`
- `SuspendsActiveIntent`
- `ClearsActiveIntent`
- `PreservesDestination`

### What exists to authorize live movement
- explicit gate categories exist
- explicit allow/deny model exists
- explicit pursuit requirement exists
- bounded state transitions exist
- separation between state update and live movement exists (`AllowsStateUpdate` vs `AllowsLiveMovement`)

### What is missing for live movement
- actor/executor contract
- execution request/result model
- target validation model
- map/range/target/deleted/alive checks as reusable execution preconditions
- no-op/dry-run execution path
- adapter boundary between router decisions and ServUO mutation
- explicit source attribution / caller identity for requests
- explicit live-execution audit path

## Target BaseHire inspected
File inspected:
- `Scripts/Mobiles/NPCs/BaseHire.cs`

### Namespace
- `Server.Mobiles`

### Base class
- `BaseCreature`

### Available movement/control properties visible in class/useful via inheritance
- `ControlMaster`
- `Controlled`
- `ControlSlots`
- `Combatant`
- `ControlTarget`
- `ControlOrder`
- `Home`
- `RangeHome`
- `Location`
- `Map`
- `GetOwner()` helper
- `InRange(...)`
- standard `Mobile`/`BaseCreature` behavior through inheritance

### Safe helper methods already present
- `GetOwner()` is the most obvious safe helper already available
- no dedicated safe movement adapter/helper methods were found in `BaseHire`

### AIGM / companion interfaces
- `BaseHire` itself does **not** implement `IAIGMCompanionActor` in this target file
- specific companion subclasses likely provide the companion-specific surface instead

### Serialization concerns relevant to movement state
- `BaseHire` already serializes hire-specific state and restores timers
- touching `BaseHire` for first live execution would increase risk because it is broad infrastructure and has existing lifecycle/timer behavior

### Conclusion on BaseHire
- first live execution should **not** directly expand `BaseHire` if avoidable
- use an execution adapter/contract first

## Preserved router inspected for live execution lessons
Preserved Dev file:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

### Live execution / authority usage classified

| Usage / symbol | Observed in preserved router | Classification | Notes |
|---|---|---|---|
| `Combatant` | yes | C / unsafe for first live execution | combat mutation should not be in first movement slice |
| `ControlTarget` | yes | B/D | likely later; should live behind adapter and explicit gate |
| `ControlOrder` | yes | B/D | likely later for follow/guard/come semantics |
| `CantWalk` | yes | C | not for first live execution slice |
| `Home` | not in router directly | B later / E elsewhere | reset logic lives in StateAccess leak |
| `RangeHome` | not in router directly | B later / E elsewhere | defer |
| `MoveToWorld` | not directly observed | F | deeper review if chosen as execution primitive |
| `SetLocation` | not directly observed | C | should not be used for first live movement |
| `Location =` | not directly observed | C | should not be used |
| `Direction =` | not directly observed | C | should not be used |
| `AIObject` | not observed | F | deeper review if needed later |
| `DelayCall` / `Timer` | not observed directly in router | B later | timing concerns likely belong in executor/adapter later |
| `pathfinding` | indirect via travel controller | D | must sit behind executor/adapter, not router directly |
| `GetDirectionTo` | not observed | F | defer |
| `InRange` | indirect through broader system | A/B | validation likely required early |
| `Map` | yes | A | needed for validation/request payload |
| `Point3D` | yes | A | needed for move-to-point payload |
| `Mobile` | yes | A | needed for target/request payload and validation |
| `BaseHire` | yes | A | actor surface required for eventual execution |
| `StateAccess` | heavy | C/E | must not be reintroduced as authority |
| travel controller | yes | D | should move behind executor/adapter |
| tracking controller | yes | D/E | should stay separate from execution and remain gated |

### Key lesson
Preserved router mixes decision-making and live control mutation too early. The target should keep:
- router = decision + gating authority
- executor/adapter = ServUO mutation authority

## StateAccess inspected for bypass risks
Preserved Dev file:
- `Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs`

### Methods mutating live control fields
- `ResetAllCompanionIntentState`
  - mutates `Combatant`
  - mutates `ControlTarget`
  - mutates `CantWalk`
  - mutates `Home`
  - mutates `RangeHome`
  - mutates `ControlOrder`

### Methods duplicating router authority
- `SetMovementState`
- `SuspendMovementIntent`
- `ResumeSuspendedMovementIntent`
- `SetTrackingMode`
- `LatchHoldPosition`
- `ClearHoldPosition`
- travel/tracking setters and objective ownership

### Methods tracking active movement/travel dictionaries
- `TravelObjectives`
- `NextTravelPulseUtc`
- `NextTravelThreatScanUtc`
- `TrackingEnabled`
- `TrackingPhase`
- `TrackingNextSweepUtc`
- `TrackingMonsterLock`
- `HoldPositionUntilUtc`
- `MovementStates`
- `EngagementStates`

### Methods that could become read-only later
- `GetTravelObjective`
- `GetNextTravelPulseUtc`
- `GetNextTravelThreatScanUtc`
- `GetTrackingEnabled`
- `GetTrackingPhase`
- `GetTrackingNextSweepUtc`
- `GetTrackingMonsterLock`
- `GetHoldPositionUntilUtc`
- `IsHoldPositionLatched`
- `GetMovementState`
- `GetEngagementState`
- `GetExecutionMode`

### Methods that must never be ported as-is
- `ResetAllCompanionIntentState`
- concrete companion mobile mutations via guard/support execution mode accessors in current concrete form

### StateAccess conclusion
- StateAccess can remain fully deferred: **yes**
- it must not be used for first live movement execution

## Live execution boundary recommendation
### Recommended owner of live movement calls
- **not** `UMGMovementRouter` directly
- live ServUO mutation should be owned by a separate executor/adapter surface

### Recommended boundary
- router decides and gates
- executor/adapter performs validated ServUO mutation
- StateAccess remains out of the live path

### Recommended architectural shape
- router remains authority over **permission**
- executor/adapter becomes authority over **mechanics of ServUO mutation**

Preferred interface direction:
- `IUMGMovementExecutor`
- or `IUMGServUOMovementAdapter`

Recommendation between names:
- prefer `IUMGMovementExecutor` first because it is narrower and intent-focused
- adapter naming can come later if implementation splits further

## Live execution prerequisites table

| Prerequisite | Present/Missing | Notes |
|---|---|---|
| explicit `MovementExecutionGate` decision | present | modeled, but no executor uses it yet |
| explicit `PursuitGate` decision | present | requires explicit pursuit intent |
| actor type / companion actor reference | partial | `BaseHire` exists; companion interface integration is indirect |
| safe live target `Mobile` or `Point3D` reference | partial | intent has fields, but validation pipeline not formalized |
| map validation | missing | needed before any real movement |
| range validation | missing | needed for follow/guard/pursuit decisions |
| target validity / deleted checks | missing as reusable execution layer | present ad hoc in old systems only |
| alive checks | missing as reusable execution layer | same |
| hold state check | present partial | router state can represent it |
| stop state check | present partial | router state can represent it |
| combat interruption policy | present partial | state-only policy exists |
| no StateAccess bypass | missing as enforced contract | planned by boundary only |
| no parser/skill bypass | missing as enforced contract | planned by boundary only |
| null checks | partial | router has some, executor contract missing |
| debug/reporting path | missing | no execution result model yet |
| dry-run/no-op mode | missing | should exist before real executor |

## First executable intent candidate table

| Intent | should be live-executable first | required gate | required target data | required ServUO mutation | risk | recommended timing |
|---|---|---|---|---|---|---|
| `MOVE_TO_POINT` | **yes** | `MovementExecutionGate` + `TravelGate` | `DestinationPoint`, `DestinationMap`, owner/request context | likely movement adapter call only | lowest of live candidates | **first** |
| `TRAVEL_TO_DESTINATION` | maybe later | `MovementExecutionGate` + `TravelGate` | destination name -> resolved target/point | path/travel resolution | medium | after move-to-point |
| `FOLLOW` | later | `MovementExecutionGate` + `FollowGuardGate` | target mobile/owner | `ControlTarget` / `ControlOrder`-style behavior likely | medium/high | after executor contract and validation |
| `GUARD` | later | `MovementExecutionGate` + `FollowGuardGate` | guard target | control-order/combat-proximity semantics | high | later |
| `PURSUE_TRACKED_TARGET` | later | `MovementExecutionGate` + `PursuitGate` | explicit target mobile/serial | target follow/pursuit control | high | after explicit target validation layer |
| `HOLD` | no first live mutation needed | `HoldGate` | reason only | none for first slice | low | keep state-only first |
| `STOP` | no first live mutation needed | `StopGate` | reason only | none for first slice | low | keep state-only first |
| `RESUME` | no first live mutation needed | `ResumeGate` | suspended intent | none directly until execution path exists | medium | after movement execution contract |
| `COMBAT_INTERRUPTION` | no first live mutation needed | `CombatInterruptionGate` | interrupt reason/current state | none directly for first slice | low | keep state-only first |
| `START_TRACKING` | no | `TrackingScanGate` | tracking request | none | low | remain non-executing |
| `TRACKING_REPORT` | no | `TrackingScanGate` | report request | none | low | remain non-executing |

## Permitted/deferred ServUO mutation table

| Field / surface | may be used in first live execution | must be deferred | must require explicit gate | requires companion mobile validation | Notes |
|---|---|---|---|---|---|
| `Location` | no | yes | yes | yes | do not direct-set |
| `Direction` | no | yes | yes | yes | defer |
| `MoveToWorld` | maybe later | yes for first slice unless strongly justified | yes | yes | not first move slice unless adapter specifically uses it |
| `ControlTarget` | no for first slice | yes | yes | yes | likely follow/guard/pursuit later |
| `ControlOrder` | no for first slice | yes | yes | yes | later behavioral layer |
| `Combatant` | no | yes | yes | yes | combat and movement should stay separate first |
| `CantWalk` | no | yes | yes | yes | do not use for first slice |
| `Home` | no | yes | yes | yes | defer |
| `RangeHome` | no | yes | yes | yes | defer |
| `AIObject` | no | yes | yes | yes | defer |
| `Map` | yes as validation/input only | no | yes | yes | safe as request/validation context |
| `Point3D` | yes as validation/input only | no | yes | yes | safe as request/validation context |

## Executor / adapter design options

### Option A — Direct router execution
- router mutates `BaseHire` / `Mobile` directly
- risk: **high**
- recommendation: **reject for first live slice**

### Option B — Movement executor interface
- add interface such as `IUMGMovementExecutor`
- no live implementation yet
- risk: **low**
- recommendation: **preferred next step**

### Option C — Null/no-op executor
- add no-op executor implementation that records decisions but does not move
- risk: **low**
- recommendation: **good immediately after interface exists**

### Option D — ServUO movement adapter
- add real adapter later, behind gates and validation
- risk: **medium/high**
- recommendation: later, after contract + no-op path

### Option E — Keep router non-executing longer
- continue model/gate work only
- risk: **low**
- recommendation: acceptable fallback, but current stack is ready for an execution contract

## Recommended next mutation phase
### Primary recommendation
- **Phase 56Q-R4-LIVE-EXECUTION-CONTRACT — Add movement execution interface/result model only + build**

Recommended surfaces:
- `IUMGMovementExecutor`
- `UMGMovementExecutionRequest`
- `UMGMovementExecutionResult`

No live movement implementation yet.

### Secondary follow-up
- **Phase 56Q-R4-LIVE-EXECUTION-NOOP — Add no-op movement executor + build**

### Tertiary follow-up
- **Phase 56Q-R4-ROUTER-EXECUTOR-WIRING — Wire router to optional no-op executor only + build**

## Explicit answers

### Should router directly mutate BaseHire/Mobile?
- **no**

### Should there be a movement executor interface first?
- **yes**

### Should there be a no-op executor before real executor?
- **yes**

### What is the safest first live movement intent candidate?
- **`MOVE_TO_POINT`**

### What intent types must remain non-executing?
- `START_TRACKING`
- `TRACKING_REPORT`
- initially also `HOLD`, `STOP`, `RESUME`, and `COMBAT_INTERRUPTION` should remain state-only

### What mutation fields are forbidden for first live execution?
- `Combatant`
- `CantWalk`
- `Home`
- `RangeHome`
- direct `Location =`
- `Direction =`
- likely also `ControlTarget` and `ControlOrder` for the very first live slice

### What validation must exist before any movement?
- non-null actor
- non-null request
- map validation
- point/target validation
- alive/deleted checks
- hold/stop gating
- explicit movement execution gate approval
- no StateAccess / parser / skill bypass
- debug/result reporting path
- dry-run/no-op path

### Can StateAccess remain fully deferred?
- **yes**

### What is the smallest safe next mutation?
- **Phase 56Q-R4-LIVE-EXECUTION-CONTRACT — Add movement execution interface/result model only + build**

## Confirmations
- confirmation no files were copied: **confirmed**
- confirmation no patch was applied: **confirmed**
- confirmation no code files changed: **confirmed**
- confirmation no commit occurred: **confirmed**

## Final live-boundary statement
Router authority should stay about permission and policy. Real ServUO mutation should be isolated behind an executor contract that can first be implemented as a no-op path. The first live candidate should be `MOVE_TO_POINT`, and only after explicit execution contracts, validation, and dry-run behavior exist. StateAccess should stay fully deferred out of the live path.
