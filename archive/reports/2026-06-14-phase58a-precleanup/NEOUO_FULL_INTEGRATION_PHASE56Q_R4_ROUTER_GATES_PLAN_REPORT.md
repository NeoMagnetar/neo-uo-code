# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER-GATES PLAN REPORT

Date: 2026-06-08 09:14 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `b613fe7cd22e5b5d035aac2607790614a6571b32`
- latest commit: `b613fe7 feat: add non-executing UMG movement router skeleton`

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

## Current router skeleton inspected
Target file inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

### Current public methods
- `TrySubmitIntent(UMGMovementIntent intent, out string reason)`
- `ApplyIntentToState(UMGMovementIntent intent)`
- `Hold(string reason = null)`
- `Stop(string reason = null)`
- `Suspend(string reason = null)`
- `Resume(string reason = null)`
- `CombatInterruption(string reason = null)`
- `Clear()`
- `GetState()`
- property `State`

### Current state mutation behavior
- mutates only bounded `UMGMovementState`
- uses `Apply(...)` for intent copy
- sets `ActiveIntent`, `SuspendedIntent`, `InterruptReason`, `TrackingMode`, `UpdatedUtc`, `LastMovementDecision`, `LastMovementDecisionUtc`
- preserves destination metadata during `Suspend` / `CombatInterruption` because `Clear()` is not invoked

### Current validation behavior
- `TrySubmitIntent` rejects null intent
- unsupported movement kinds return `false` with reason text
- no live control or movement validation exists yet

### Current intent kinds handled
- direct submit/update:
  - `Idle`
  - `FollowPlayer`
  - `ReturnToPlayer`
  - `TravelToNamedDestination`
  - `MoveToPoint`
  - `PursueTrackedTarget`
  - `GuardTarget`
  - `RecoverFromStuck`
- special policy path:
  - `HoldPosition`

### Current Hold / Stop / Suspend / Resume / CombatInterruption / Clear behavior
- `Hold`: suspends prior active intent if needed, sets active intent to `HoldPosition`, forces `TrackingMode=Idle`
- `Stop`: suspends prior active intent if needed, clears active intent to `Idle`, forces `TrackingMode=Idle`
- `Suspend`: moves active intent to suspended if appropriate, sets active intent to `Idle`
- `Resume`: restores `SuspendedIntent` into `ActiveIntent` if present
- `CombatInterruption`: suspends travel/follow/guard/pursuit-like intents without clearing destination metadata
- `Clear`: hard-clears bounded model only via `UMGMovementState.Clear()`

### Live mutation audit
- still contains no live movement/control mutation: **confirmed**

## Movement model surfaces inspected
Inspected target surfaces:
- `UMGMovementIntent.cs`
- `UMGMovementState.cs`
- `UMGMovementIntentKind.cs`
- `AIGMExecutionMode.cs`
- `AIGMTrackingCyclePhase.cs`

### Gate-supporting properties and fields
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

From `UMGMovementState`:
- active intent: `ActiveIntent`
- suspended intent: `SuspendedIntent`
- destination/travel state:
  - `DestinationName`
  - `DestinationPoint`
  - `DestinationMap`
  - `TargetSerial`
  - `TargetName`
  - `Reason`
- interruption/hold-like state:
  - `InterruptReason`
  - `TrackingMode`
  - `LastMovementDecision`
  - `LastMovementDecisionUtc`
- metadata:
  - `UpdatedUtc`
  - `RoleProfileKey`

From vocab/policy enums:
- `UMGMovementIntentKind`
- `AIGMExecutionMode`
- `AIGMTrackingCyclePhase`

### Missing fields for stronger future gate decisions
Current target models do not explicitly track:
- whether live movement execution is currently authorized
- whether pursuit permission was explicit vs inferred
- whether HOLD is latched vs temporary
- whether STOP means clear vs suspend under current policy
- whether combat interruption is resumable by policy vs manual resume only
- who/what subsystem requested the intent (parser/router/tracking/travel/manual)
- gate decision result metadata / audit trail

These gaps suggest a future gate-model phase is useful before live execution.

## Parser and skill executor boundary inspection

### Parser (`AIGMCompanionIntentParser`)
- parser can generate movement-like requests as intent text/data:
  - follow / come
  - travel to destination
  - stop travel
  - return home
  - tracking start/stop/report
- parser does **not** execute movement directly
- parser currently still has combat-target resolution logic for attack intents, but not movement execution
- parser can bypass router today only if some downstream caller directly mutates movement fields instead of routing through approved router path
- future rule: parser output must be treated as request/intent only

### Skill executor (`AIGMCompanionSkillExecutor`)
- no path to movement execution found
- touches support cooldowns, range checks, hits, bandage/spell actions
- uses `IAIGMCompanionActor.NextSupportActionUtc`
- may inspect combat context, but does not route movement
- future rule: skill executor must remain forbidden from movement calls and control-field mutation

## Preserved Dev router inspected for gate lessons
From preserved Dev `UMGMovementRouter.cs`, live mutation / authority lessons:
- live control mutation occurs in:
  - `RouteFollowPlayer`
  - `RouteReturnToPlayer`
  - `RoutePursueTrackedTarget`
  - `RouteGuardTarget`
- these methods directly write:
  - `CantWalk`
  - `Combatant`
  - `ControlTarget`
  - `ControlOrder`
- tracking/travel/pursuit routing occurs in:
  - `SetTrackedPursuitState`
  - `RouteTravelToNamedDestination`
  - `RouteMoveToPoint`
  - `RoutePursueTrackedTarget`
  - follow/guard routes suspend pursuit/travel implicitly
- StateAccess is used in nearly every route for:
  - movement state persistence
  - hold/clear behavior
  - tracking mode mutation
  - movement decision logging
  - hard reset
- every method that would need explicit gates before porting:
  - `RouteIntent`
  - `RecordIntent` (only if tied to external state store)
  - `SetTrackedPursuitState`
  - `ClearTrackedPursuitState`
  - `RouteFollowPlayer`
  - `RouteHoldPosition`
  - `RouteReturnToPlayer`
  - `RouteTravelToNamedDestination`
  - `RouteMoveToPoint`
  - `RoutePursueTrackedTarget`
  - `RouteGuardTarget`

## StateAccess inspected for boundary leaks
From preserved Dev `AIGMCompanionStateAccess.cs`:

### Must never bypass router
- `ResetAllCompanionIntentState`
- `SetMovementState` if used as authority instead of store
- `SuspendMovementIntent`
- `ResumeSuspendedMovementIntent`
- `LatchHoldPosition`
- `ClearHoldPosition`
- travel/tracking mutation setters
- `SetGuardOwnerMode` in current concrete-mobile form

### Duplicate or shadow router authority
- movement-state mutation methods
- hold/suspend/resume methods
- tracking mode mutation
- hard reset of movement/travel/tracking/combat/control state

### Should remain deferred
- all concrete companion-mobile accessors
- all travel/tracking dictionary writers
- hard reset method

### Could become read-only later
- getters for movement/tracking/travel state
- execution-mode inspection
- hold-position inspection

## Gate definitions

### A. MovementExecutionGate
- purpose: master gate for any live ServUO movement/control mutation
- allowed callers: router only
- forbidden callers: parser, skill executor, StateAccess, tracking, travel, companion mobiles, action executor direct paths
- input data required: intent kind, current movement state, execution mode/policy, caller identity, future actor capability state
- output decision: allow / deny / defer
- allowed state update: decision audit metadata only
- forbidden side effects: direct movement unless explicitly allowed by gate result
- live ServUO mutation allowed: **not now**, eventually yes only through approved router path
- currently implementable with existing surfaces: **partially**
- missing prerequisites: gate-result model, caller/source metadata, authority policy model

### B. PursuitGate
- purpose: control when a tracked target becomes active pursuit
- allowed callers: router evaluating explicit `PursueTrackedTarget`
- forbidden callers: tracking scan/report loops, StateAccess, parser direct side effects
- input data required: explicit pursuit intent, target identity, current hold/stop/combat state
- output decision: allow pursuit state / deny pursuit
- allowed state update: active intent to `PursueTrackedTarget`, tracking mode metadata
- forbidden side effects: implicit `ControlTarget` mutation outside execution gate
- live ServUO mutation allowed: not now
- currently implementable: **yes, state-only**
- missing prerequisites: explicit source flag for pursuit approval

### C. TrackingScanGate
- purpose: guarantee scan/report tracking never issues movement
- allowed callers: tracking request parser/router policy layer
- forbidden callers: any execution path using scan/report as movement trigger
- input data required: tracking command kind, current intent, tracking mode
- output decision: report-only / scan-only / deny
- allowed state update: tracking/report metadata only
- forbidden side effects: pursuit, travel, follow, guard activation
- live ServUO mutation allowed: no
- currently implementable: **yes**
- missing prerequisites: clearer tracking-report model fields

### D. TravelGate
- purpose: control destination/coordinate travel execution
- allowed callers: router evaluating `TravelToNamedDestination` or `MoveToPoint`
- forbidden callers: parser direct, StateAccess, travel dictionary side effects
- input data required: destination data, hold/stop/combat status, future authority policy
- output decision: allow travel execution later / record only now / deny
- allowed state update: destination intent/state
- forbidden side effects: pathfinding, movement execution outside router
- live ServUO mutation allowed: not now
- currently implementable: **yes as record-only**
- missing prerequisites: live execution gate integration

### E. FollowGuardGate
- purpose: control follow/guard-style movement behavior
- allowed callers: router only
- forbidden callers: parser, StateAccess, companion mobile direct writes
- input data required: follow/guard intent, owner/target identity, hold/stop status
- output decision: allow later / deny / record only
- allowed state update: active intent, target metadata
- forbidden side effects: direct `ControlTarget` / `ControlOrder` mutation outside execution gate
- live ServUO mutation allowed: not now
- currently implementable: **yes as record-only**
- missing prerequisites: target identity normalization, actor authority policy

### F. HoldGate
- purpose: suppress autonomy safely
- allowed callers: router policy methods only
- forbidden callers: StateAccess reset paths, parser direct movement mutation
- input data required: current active intent, optional reason
- output decision: hold applied / already held / deny
- allowed state update: suspend active intent, set hold state metadata, force non-executing idle/hold policy
- forbidden side effects: `CantWalk`, `ControlOrder`, `ControlTarget`, `Home`, `RangeHome` changes
- live ServUO mutation allowed: no
- currently implementable: **yes**
- missing prerequisites: explicit hold-latched model field if stronger semantics needed

### G. StopGate
- purpose: clear or suspend active intent under explicit stop policy
- allowed callers: router only
- forbidden callers: parser direct reset, StateAccess hard reset
- input data required: current active/suspended intent, optional reason
- output decision: stopped-cleared / stopped-suspended / deny
- allowed state update: set active to idle, optionally preserve suspended intent and destination metadata
- forbidden side effects: hard reset of live control/combat fields
- live ServUO mutation allowed: no
- currently implementable: **yes**
- missing prerequisites: explicit stop-policy enum if needed

### H. ResumeGate
- purpose: restore suspended movement intent only when policy allows
- allowed callers: router only
- forbidden callers: StateAccess direct resume, parser direct resume, tracking loop side effects
- input data required: suspended intent, current hold/combat/stop state, optional reason
- output decision: resume / deny / defer
- allowed state update: restore active intent from suspended intent
- forbidden side effects: direct movement execution unless execution gate also passes
- live ServUO mutation allowed: not now
- currently implementable: **yes as state-only**
- missing prerequisites: explicit resume-policy metadata and source attribution

### I. CombatInterruptionGate
- purpose: suspend travel/follow/guard/pursuit without erasing destination state
- allowed callers: router/combat arbiter only
- forbidden callers: raw combat side effects directly mutating movement fields
- input data required: current active intent, combat event, interruption reason
- output decision: suspend / ignore / deny
- allowed state update: move active intent to suspended, preserve destination/target metadata
- forbidden side effects: direct combat targeting mutation as a substitute for movement policy
- live ServUO mutation allowed: no
- currently implementable: **yes as state-only**
- missing prerequisites: formal combat event source model

### J. StateAccessBoundaryGate
- purpose: prevent StateAccess from bypassing router authority
- allowed callers: router-approved read-only adapters only
- forbidden callers: any StateAccess write path trying to change movement authority state or live controls
- input data required: caller identity, attempted operation kind
- output decision: allow read / deny write / defer
- allowed state update: none, or audit only
- forbidden side effects: all live movement/control mutation from StateAccess
- live ServUO mutation allowed: no
- currently implementable: **as policy only**
- missing prerequisites: read-only adapter design / operation categorization

### K. ParserBoundaryGate
- purpose: prevent parser from movement execution
- allowed callers: parser may emit intent only
- forbidden callers: parser may not execute movement or mutate control fields
- input data required: parsed intent, addressed target, command type
- output decision: accepted-as-intent / rejected
- allowed state update: none directly; router may later record
- forbidden side effects: any live movement or direct control-field mutation
- live ServUO mutation allowed: no
- currently implementable: **yes as policy**
- missing prerequisites: standardized intent-to-router handoff surface

### L. SkillExecutorBoundaryGate
- purpose: prevent skill executor from movement execution
- allowed callers: skill executor may heal/cure/use skills only
- forbidden callers: skill executor may not request or perform movement mutation directly
- input data required: skill request, target/range/cooldown context
- output decision: allowed skill use / denied skill use
- allowed state update: support cooldowns only
- forbidden side effects: movement routing, control-field mutation
- live ServUO mutation allowed: no movement; skill effects only
- currently implementable: **yes as policy**
- missing prerequisites: none for boundary declaration

## Command-to-gate mapping

| intent/input | required gate | allowed to update UMGMovementState | allowed to execute live movement now | allowed to execute live movement later | report-only | scan-only | explicit pursuit required | clears active intent | suspends active intent | preserves destination | notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `START_TRACKING` | `TrackingScanGate` | yes | no | no | no | yes | no | no | no | yes | enable scan/report mode only |
| `TRACKING_REPORT` | `TrackingScanGate` | optional metadata only | no | no | yes | no | no | no | no | yes | pure report |
| `PURSUE_TRACKED_TARGET` | `PursuitGate` + `MovementExecutionGate` later | yes | no | yes | no | no | yes | no | maybe | yes | explicit signal required |
| `MOVE_TO_POINT` | `TravelGate` + `MovementExecutionGate` later | yes | no | yes | no | no | no | no | maybe | yes | coordinate travel intent |
| `TRAVEL_TO_DESTINATION` | `TravelGate` + `MovementExecutionGate` later | yes | no | yes | no | no | no | no | maybe | yes | named destination travel |
| `FOLLOW` | `FollowGuardGate` + `MovementExecutionGate` later | yes | no | yes | no | no | no | no | maybe | n/a | target-follow semantics |
| `GUARD` | `FollowGuardGate` + `MovementExecutionGate` later | yes | no | yes | no | no | no | no | maybe | n/a | guard target semantics |
| `HOLD` | `HoldGate` | yes | no | maybe later only via explicit release | no | no | no | no | yes | yes | suppress autonomy |
| `STOP` | `StopGate` | yes | no | later only after new intent/resume policy | no | no | no | yes | maybe | yes | explicit stop policy |
| `RESUME` | `ResumeGate` | yes | no | yes | no | no | no | no | no | yes | only if suspended intent allowed |
| `COMBAT_INTERRUPTION` | `CombatInterruptionGate` | yes | no | later via resume/exec gate | no | no | no | no | yes | yes | suspend without erasing destination |
| `CLEAR` | `StopGate` or dedicated clear policy | yes | no | no | no | no | no | yes | yes | no | bounded-model clear only |
| `IDLE / NO_OP` | none or lightweight policy check | yes | no | no | maybe | no | no | no | no | yes | descriptive state only |

## Authority ownership map

| subsystem/file | may request movement | may update intent/state | may execute movement | may mutate ServUO control fields | must call router | must never call movement directly | notes |
|---|---|---|---|---|---|---|---|
| `AIGMCompanionIntentParser` | yes | no direct state write | no | no | yes | yes | emits intent only |
| `AIGMCompanionSkillExecutor` | no for movement | no movement state | no | no movement control | n/a | yes | heals/cures/cooldowns only |
| `AIGMCompanionStateAccess` | no | future read-only maybe | no | no | yes if ever involved | yes | must not remain authority |
| `UMGMovementRouter` | yes (as arbiter) | yes | eventually yes | eventually yes through gates only | n/a | no | sole approved movement path |
| `UMGMovementState` | no | yes as data container | no | no | n/a | yes | passive model only |
| `UMGMovementIntent` | no | yes as request DTO | no | no | n/a | yes | passive DTO only |
| companion mobiles | maybe request contextually | no direct | no direct | no direct bypass | yes | yes | must not bypass gates |
| action executor | maybe future orchestrator | no direct movement state unless routed | no direct | no direct | yes | yes | not currently required for skeleton |
| tracking system | yes for scan/report and explicit pursuit request | metadata only | no | no | yes | yes | scan/report must not move |
| travel system | yes as downstream executor later | maybe status reporting | not until gate-approved | not until gate-approved | yes | yes | must not self-authorize |
| combat interruption handling | yes as interrupt signal | yes via router gate | no direct | no direct | yes | yes | may suspend, not execute |

## Recommended next sequence
Recommended sequence:
1. **gate model**
2. router gate methods
3. stronger state transitions
4. only then consider live execution gates

### Primary recommendation
- **Phase 56Q-R4-ROUTER-GATE-MODEL — Add movement gate result / policy model only + build**

Why:
- current target lacks durable gate-decision metadata
- explicit gate models reduce ambiguity before adding more router behavior
- avoids premature execution logic

### Secondary recommendation
- after gate models, proceed to:
  - **Phase 56Q-R4-ROUTER-GATE-SKELETON — Add non-executing gate methods to router + build**

### Tertiary recommendation
- then, if needed:
  - **Phase 56Q-R4-ROUTER-STATE-TRANSITIONS — Implement HOLD / STOP / SUSPEND / RESUME state transitions only + build**
- note: current skeleton already has primitive versions, so that phase would be refinement, not first introduction

## Explicit answers

### What gate must exist before any live movement execution?
- **MovementExecutionGate**

### What gate prevents tracking scan/report from causing movement?
- **TrackingScanGate**

### What gate permits pursuit, and what explicit signal is required?
- **PursuitGate**
- required explicit signal: a deliberate `PursueTrackedTarget` movement intent, not scan/report output alone

### What gate handles HOLD?
- **HoldGate**

### What gate handles STOP?
- **StopGate**

### What gate handles RESUME?
- **ResumeGate**

### What gate handles combat interruption?
- **CombatInterruptionGate**

### What gate prevents StateAccess bypass?
- **StateAccessBoundaryGate**

### What gate prevents parser bypass?
- **ParserBoundaryGate**

### What gate prevents skill executor bypass?
- **SkillExecutorBoundaryGate**

### Is the next safe mutation a gate model, router gate methods, or state transition methods?
- **gate model first**

### What is the smallest safe next mutation?
- **Phase 56Q-R4-ROUTER-GATE-MODEL — Add movement gate result / policy model only + build**

## Confirmations
- confirmation no files were copied: **confirmed**
- confirmation no patch was applied: **confirmed**
- confirmation no command/action/parser/skill/companion mobile files were changed: **confirmed**
- confirmation no commit occurred: **confirmed**

## Final gate statement
No subsystem except the approved router path may ever mutate live movement/control fields. But even the router must remain non-executing until gate results and authority policy are modeled explicitly. The safe path is to make permission itself first-class before any live movement returns.
