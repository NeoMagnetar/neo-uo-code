# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER AUTHORITY PLAN REPORT

Date: 2026-06-08 08:58 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `a1eb50290ecd59cd4c098e17ae44e9b7ddf0e12d`
- latest commit: `a1eb502 feat: add bounded UMG movement state model`

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

## UMGMovementRouter inspected
Source file inspected:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementRouter.cs`

### Namespace
- `Server.Custom.AIGM`

### Top-level types
- `UMGMovementRouter` (static class)

### Constructors
- none

### Static/global state
- no static dictionaries found
- no static timers found
- no fields/properties found

### Public methods
- `RouteIntent(BaseHire companion, UMGMovementIntent intent, out string response)`
- `RecordIntent(BaseHire companion, UMGMovementIntent intent)`
- `SetTrackedPursuitState(BaseHire companion, AIGMCompanionTrackingEntry entry)`
- `ClearTrackedPursuitState(BaseHire companion, string reason)`
- `NoteRecoverFromStuck(BaseHire companion, string reason)`

### Private methods
- `RouteFollowPlayer`
- `RouteHoldPosition`
- `RouteReturnToPlayer`
- `RouteTravelToNamedDestination`
- `RouteMoveToPoint`
- `RoutePursueTrackedTarget`
- `RouteGuardTarget`

### Referenced AIGM types
- `UMGMovementIntent`
- `UMGMovementState`
- `UMGMovementIntentKind`
- `AIGMCompanionStateAccess`
- `AIGMCompanionTrackingEntry`
- `AIGMCompanionTrackingCycle`
- `AIGMCompanionTravelController`

### Referenced ServUO types
- `BaseHire`
- `Mobile`
- `World`
- `OrderType`
- `Map`
- `Point3D`

### Referenced tracking/travel types
- `AIGMCompanionTrackingEntry`
- `AIGMCompanionTrackingCycle`
- `AIGMCompanionTravelController`

### Referenced companion mobile types
- none directly in router file, but `BaseHire` control mutation assumes companion mobile execution layer

### Referenced action executor types
- none directly in router file

### Referenced StateAccess types
- `AIGMCompanionStateAccess` heavily

## Router method classification

| Method | Authority class | Notes |
|---|---|---|
| `RouteIntent` | H. Unsafe/mixed | dispatch + state writes + routes to authority methods |
| `RecordIntent` | B. Intent/state write only | safe conceptually, but currently routed through `StateAccess` |
| `SetTrackedPursuitState` | F. Tracking/travel behavior | creates pursuit intent and flips tracking mode to Pursue |
| `ClearTrackedPursuitState` | B/H mixed | state reset only in effect, but uses `StateAccess` and directly rewrites movement state |
| `NoteRecoverFromStuck` | B. Intent/state write only | records recover intent only |
| `RouteFollowPlayer` | D/G | direct control mutation + follow behavior |
| `RouteHoldPosition` | D/H | resets all companion state, latches hold, control mutation via `StateAccess` |
| `RouteReturnToPlayer` | D/G | direct control mutation + return/follow behavior |
| `RouteTravelToNamedDestination` | C/F | router-controlled travel entry via travel controller |
| `RouteMoveToPoint` | C/F | router-controlled coordinate travel via travel controller |
| `RoutePursueTrackedTarget` | D/F | explicit pursuit with direct control mutation |
| `RouteGuardTarget` | D/E/G | direct control mutation + guard mode + guard behavior |

## Live mutation search results in router
Observed direct live mutation usage:
- `companion.CantWalk = false`
- `companion.Combatant = null`
- `companion.ControlTarget = target/requester`
- `companion.ControlOrder = OrderType.Follow/Come/Guard`

Observed world/query usage:
- `World.FindMobile(...)`
- `intent.DestinationMap ?? companion.Map`
- `Point3D` via `intent.DestinationPoint`
- `Mobile` throughout routing decisions

Not observed directly in router file:
- `Home`
- `RangeHome`
- `MoveToWorld`
- `SetLocation`
- `Location =`
- `Direction =`
- `Frozen`
- `Blessed`
- `Warmode`
- `AIObject`
- explicit `Timer` or `DelayCall`
- explicit pathfinding call names

Important nuance:
- even without direct pathfinding calls, `RouteTravelToNamedDestination` and `RouteMoveToPoint` delegate into `AIGMCompanionTravelController`, which is still movement authority.

## Target reduced-model compatibility check
Already present in target:
- `UMGMovementIntentKind`
- `UMGMovementIntent`
- bounded `UMGMovementState`
- `AIGMExecutionMode`
- `AIGMTrackingCyclePhase`
- `IAIGMCompanionActor`
- `AIGMCompanionIntent`
- `AIGMCompanionIntentParser`
- `AIGMCompanionSkillExecutor`

Mismatch vs preserved Dev:
- preserved Dev `UMGMovementState` uses `RoleProfile`
- target bounded `UMGMovementState` uses `RoleProfileKey`
- router file inspected does **not** currently rely on `RoleProfile`, so this mismatch is not the primary blocker for router work

## AIGMCompanionStateAccess boundary inspection
Router-boundary duplication / authority leakage found in StateAccess:
- `SetMovementState`
- `SetTrackingMode`
- `NoteLastMovementDecision`
- `SuspendMovementIntent`
- `ResumeSuspendedMovementIntent`
- `LatchHoldPosition`
- `ClearHoldPosition`
- `SetGuardOwnerMode`
- `ResetAllCompanionIntentState`
- travel/tracking setters and dictionary ownership

Methods that should move behind router or router-owned services:
- `SuspendMovementIntent`
- `ResumeSuspendedMovementIntent`
- `LatchHoldPosition`
- `ClearHoldPosition`
- `SetTrackingMode`
- `NoteLastMovementDecision`
- all active-travel/tracking mutation setters
- `ResetAllCompanionIntentState`

Methods that should never be ported in current form:
- `ResetAllCompanionIntentState` (mutates `Combatant`, `ControlTarget`, `CantWalk`, `Home`, `RangeHome`, `ControlOrder`)
- concrete companion-mobile property accessors tied to Dakeyras/Danyal/Dardalion (`GetGuardOwnerMode`, `SetGuardOwnerMode` in current form)

Methods that could become read-only later:
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

Does StateAccess call router or bypass router?
- current evidence: it **bypasses** router; it directly owns state and directly mutates control state in reset logic
- therefore StateAccess must not be the movement arbiter

## Authority map

| File | Method/member | Current purpose | Reads state | Writes intent/state | Moves mobile | Mutates ServUO control/combat | Tracking/travel effect | Should belong to router | Should remain model/state | Should be deferred | Risk | Recommended disposition |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `UMGMovementRouter` | `RouteIntent` | central dispatch | yes | yes | indirect | indirect/direct via subroutes | yes | yes | no | no | high | split before import |
| `UMGMovementRouter` | `RecordIntent` | persist movement state | no | yes | no | no | no | maybe service under router | no | no | medium | candidate for skeleton/state-transition slice |
| `UMGMovementRouter` | `SetTrackedPursuitState` | convert tracking target to pursuit state | yes | yes | no direct | no | yes | yes | no | no | high | later gated authority slice |
| `UMGMovementRouter` | `ClearTrackedPursuitState` | clear pursuit metadata | yes | yes | no | no | yes | maybe | maybe | no | medium | state-transition candidate after router contract |
| `UMGMovementRouter` | `NoteRecoverFromStuck` | record stuck recovery intent | no | yes | no | no | no | maybe | maybe | no | low | state-transition candidate |
| `UMGMovementRouter` | `RouteFollowPlayer` | execute follow | yes | yes | yes | yes | suspends pursuit/travel | yes | no | no | high | exclude from early port |
| `UMGMovementRouter` | `RouteHoldPosition` | execute hold policy | yes | yes | indirect suppress | yes via reset path | yes | yes | no | no | high | exclude from early port or reduce to state-only HOLD transition |
| `UMGMovementRouter` | `RouteReturnToPlayer` | execute come/return | yes | yes | yes | yes | suspends pursuit/travel | yes | no | no | high | exclude from early port |
| `UMGMovementRouter` | `RouteTravelToNamedDestination` | start destination travel | yes | some | yes indirect | indirect | yes | yes | no | no | high | exclude from early port |
| `UMGMovementRouter` | `RouteMoveToPoint` | start coordinate travel | yes | some | yes indirect | indirect | yes | yes | no | no | high | exclude from early port |
| `UMGMovementRouter` | `RoutePursueTrackedTarget` | explicit pursuit | yes | yes | yes | yes | yes | yes | no | no | high | exclude from early port |
| `UMGMovementRouter` | `RouteGuardTarget` | execute guard | yes | yes | yes | yes | suspends pursuit/travel | yes | no | no | high | exclude from early port |
| `AIGMCompanionStateAccess` | `SetMovementState` | persistence of movement state | yes | yes | no | no | no | no | maybe read/write store | yes in current form | medium | split later |
| `AIGMCompanionStateAccess` | `SuspendMovementIntent` | transition state | yes | yes | no | no | no | yes | maybe after refactor | yes in current form | medium | move behind router policy |
| `AIGMCompanionStateAccess` | `ResumeSuspendedMovementIntent` | transition state | yes | yes | no | no | no | yes | maybe after refactor | yes in current form | medium | move behind router policy |
| `AIGMCompanionStateAccess` | `ResetAllCompanionIntentState` | hard reset | yes | yes | yes indirect | yes | yes | yes | no | yes | critical | never port as-is |

## Router dependency table

| Referenced symbol/file | Present in target | Present in preserved Dev | Category | Required for router compile | Required for router behavior | Safe before router | Recommended disposition |
|---|---|---|---|---|---|---|---|
| `UMGMovementIntentKind` | yes | yes | A. already present safe model/vocab | yes | yes | yes | satisfied |
| `UMGMovementIntent` | yes | yes | A. already present safe model/vocab | yes | yes | yes | satisfied |
| `UMGMovementState` | yes | yes | C. state model | yes | yes | yes | satisfied with reduced-model mismatch acceptable for early skeleton only |
| `AIGMExecutionMode` | yes | yes | A. already present safe model/vocab | no direct | maybe policy later | yes | optional future gate input |
| `AIGMTrackingCyclePhase` | yes | yes | A. already present safe model/vocab | indirect via StateAccess | policy context only | yes | satisfied |
| `IAIGMCompanionActor` | yes | yes | A. already present safe model/vocab | no direct in router file | no direct | yes | not blocker |
| `AIGMCompanionIntent` | yes | yes | A. already present safe model/vocab | no direct | indirect upstream parser surface | yes | upstream only |
| `AIGMCompanionStateAccess` | no | yes | F. StateAccess dependency | yes in current router file | yes | no | must be abstracted/split before router import |
| `AIGMCompanionTrackingCycle` | no | yes | G. tracking/travel dependency | yes for current file | yes | no | defer; tracking gate required |
| `AIGMCompanionTravelController` | no | yes | G/H travel/live-control dependency | yes for current file | yes | no | defer; travel gate required |
| `AIGMCompanionTrackingEntry` | no | yes | G. tracking/travel dependency | yes for current file | yes | no | defer or contract separately |
| `BaseHire` control surfaces | yes | yes | H. ServUO live-control dependency | yes | yes | no | defer live execution |
| companion mobiles (`Dakeyras/Danyal/Dardalion`) via StateAccess | no | yes | E/F | not direct in router file | indirect | no | defer |
| action executor surfaces | target partial/varied | preserved Dev yes | D. action executor dependency | no direct | maybe later orchestration | no for router authority | not required for earliest skeleton |

## Can router be split?

### Slice 1 — Router contract only
- **yes**
- viable contents:
  - method signatures
  - authority policy surface
  - no movement mutation
  - no timers
  - no control-field writes

### Slice 2 — Router skeleton
- **yes**
- viable contents:
  - accepts `UMGMovementIntent`
  - updates `UMGMovementState`
  - no live control mutation
  - no pathfinding
  - no travel controller calls
  - no StateAccess

### Slice 3 — Router state transition only
- **yes, but only in reduced form**
- safe candidates:
  - HOLD / STOP / SUSPEND / RESUME intent transitions
  - clear or suspend state only
  - no live movement execution
  - no `BaseHire` control writes

### Slice 4 — Full router
- **no, not yet**
- current dependencies are too broad and authority-bearing

### Slice 5 — Defer router
- also valid, but not necessary if a skeleton/contract helps stabilize boundaries first

## Required movement gates

1. **Execution gate**
- only the router may turn a movement intent into live ServUO control mutation
- parser / skill executor / StateAccess may propose or describe, never execute

2. **Pursuit gate**
- only explicit `PURSUE_TRACKED_TARGET` may authorize pursuit movement
- scan/report tracking may not upgrade itself into pursuit

3. **Autonomy suppression gate**
- HOLD must latch suppression state so travel/follow/pursuit cannot resume implicitly

4. **Intent clear gate**
- STOP must explicitly clear active intent or convert it to suspended/idle according to policy, not by side effect

5. **Resume gate**
- only explicit RESUME or defined router policy may restore suspended travel
- state storage alone must not resume behavior

6. **Combat interruption gate**
- combat may suspend travel, but must not erase destination state unless policy explicitly says so

7. **Tracking isolation gate**
- `START_TRACKING` and `TRACKING_REPORT` are report/scan only
- no movement side effects allowed

8. **StateAccess isolation gate**
- StateAccess cannot write control/combat fields or become authority
- at most it can become a read-only backing store later

9. **Parser/skill isolation gate**
- parser and skill executor may emit intent/policy decisions only
- they may not write `ControlTarget`, `ControlOrder`, `Combatant`, `CantWalk`, `Home`, `RangeHome`

## Legal command-to-router mapping

| Input / Intent | allowed to move | router required | state-only update allowed | report-only | clears active intent | suspends active intent | resumes suspended intent | notes |
|---|---|---|---|---|---|---|---|---|
| `START_TRACKING` | no | no for pure state flag | yes | yes | no | no | no | scan-only enable |
| `TRACKING_REPORT` | no | no | no | yes | no | no | no | report-only |
| `PURSUE_TRACKED_TARGET` | yes | yes | yes pre-router skeleton can record | no | no | maybe replaces active intent | no | explicit pursuit only |
| `MOVE_TO_POINT` | yes | yes | yes pre-router skeleton can record | no | no | maybe | no | coordinate travel |
| `TRAVEL_TO_DESTINATION` | yes | yes | yes pre-router skeleton can record | no | no | maybe | no | named destination travel |
| `FOLLOW` | yes | yes | yes pre-router skeleton can record | no | no | maybe | no | live control write only via router |
| `GUARD` | yes | yes | yes pre-router skeleton can record | no | no | maybe | no | guard mode is authority-bearing |
| `HOLD` | no live move | yes for policy | yes | no | maybe sets idle/hold | yes | no | suppresses autonomy |
| `STOP` | no live move | yes for policy | yes | no | yes | maybe | no | explicit clear/suspend policy |
| `RESUME` | maybe | yes | yes | no | no | no | yes | must be explicit or policy-gated |
| `COMBAT_INTERRUPTION` | no direct by itself | yes | yes | no | no | yes | later maybe | may suspend travel without erasing it |

## Explicit answers

### Can UMGMovementRouter be ported as-is?
- **no**
- reasons:
  - direct `BaseHire` control mutation
  - heavy `StateAccess` dependency
  - travel/tracking controller coupling
  - authority-bearing follow/guard/pursuit/travel behavior

### Can UMGMovementRouter be ported before action executor?
- **not as-is**
- a non-executing skeleton could be ported before action executor, because current router file does not directly require action executor compile surfaces

### Can UMGMovementRouter be ported before companion mobiles?
- **not as-is**
- live execution assumes companion mobile/control semantics even if not naming concrete classes directly

### Can UMGMovementRouter be ported before StateAccess?
- **not as-is**
- current router file is tightly coupled to `AIGMCompanionStateAccess`
- a decoupled skeleton/contract could be ported before StateAccess

### Can a non-executing router skeleton be ported safely?
- **yes**
- this is the strongest safe next mutation

### Which methods must be excluded from early router port?
- exclude all live execution methods:
  - `RouteFollowPlayer`
  - `RouteHoldPosition` (current form)
  - `RouteReturnToPlayer`
  - `RouteTravelToNamedDestination`
  - `RouteMoveToPoint`
  - `RoutePursueTrackedTarget`
  - `RouteGuardTarget`
- also exclude direct StateAccess-bound writes unless replaced with target-safe state model handling

### Which methods are authority-bearing?
- `RouteFollowPlayer`
- `RouteHoldPosition`
- `RouteReturnToPlayer`
- `RouteTravelToNamedDestination`
- `RouteMoveToPoint`
- `RoutePursueTrackedTarget`
- `RouteGuardTarget`
- and `RouteIntent` as dispatcher over them

### Which methods are state-only?
- conceptually state-only or reducible:
  - `RecordIntent`
  - `NoteRecoverFromStuck`
  - reduced versions of `ClearTrackedPursuitState`
  - future reduced HOLD/STOP/SUSPEND/RESUME state transitions

### What is the smallest safe next mutation?
- **Phase 56Q-R4-ROUTER-SKELETON — Add non-executing router skeleton + build**

## Recommended next phase
- **Primary recommendation: Phase 56Q-R4-ROUTER-SKELETON — Add non-executing router skeleton + build**

Why:
- lets target gain a single movement authority surface without importing live movement execution
- can accept `UMGMovementIntent`
- can update bounded `UMGMovementState`
- can encode HOLD / STOP / SUSPEND / RESUME state policy safely
- avoids StateAccess, travel controller, tracking controller, and ServUO control writes

## Alternate next phases
- **Phase 56Q-R4-ROUTER-CONTRACT — Add movement router contract / policy surface only + build**
  - use if you want an even thinner interface-first slice
- **Phase 56Q-R4-ROLEPROFILE-PLAN — Companion Role Profile Model Plan**
  - use if role semantics are needed before further state modeling
- **Phase 56Q-R4-STATEACCESS-READONLY-PLAN — Read-only StateAccess plan**
  - use if you want to split read-only inspection from authority before any router execution work

## Confirmations
- confirmation no files were copied: **confirmed**
- confirmation no patch was applied: **confirmed**
- confirmation no command/action/parser/skill/companion mobile files were changed: **confirmed**
- confirmation no commit occurred: **confirmed**

## Final authority statement
The router must become the sole movement authority, but only after being introduced in a non-executing form first. Current preserved Dev logic mixes authority with StateAccess and travel/tracking helpers. The safe path is:
1. contract or skeleton router,
2. state transitions,
3. explicit authority gates,
4. only then live movement execution.

Anything else risks letting StateAccess, tracking, or companion-control side effects quietly become the real movement router.
