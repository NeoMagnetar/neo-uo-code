# NEOUO FULL INTEGRATION PHASE56Q-R4 MOVEMENT BOUNDARY PLAN REPORT

Date: 2026-06-08 08:20 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `0955a665b9d63882a945bc4e219c9f5523162a15`
- latest commit: `0955a665 feat: add AIGM companion state vocabulary`

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
- known warning files from prior phase were not reproduced in this run
- no warning cleanup was attempted

## Current target repo state
- committed vocabulary prerequisite slice remains present:
  - `Scripts/Custom/AIGM/AIGMExecutionMode.cs`
  - `Scripts/Custom/AIGM/AIGMTrackingCyclePhase.cs`
  - `Scripts/Custom/AIGM/Movement/UMGMovementIntentKind.cs`
- many untracked narrative/report artifacts remain in repo root
- no movement files were copied in this phase
- no patch was applied in this phase
- no commit occurred in this phase

## Movement files inspected in preserved Dev
Primary files inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- `Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs`

Secondary surfaces inspected:
- `Scripts/Custom/AIGM/AIGMCompanionTravelObjective.cs`
- `Scripts/Custom/AIGM/Movement/AIGMCompanionRoleProfile.cs`
- already-ported vocabulary:
  - `AIGMTrackingCyclePhase`
  - `AIGMExecutionMode`
  - `UMGMovementIntentKind`

`AIGMCompanionEngagementState` was referenced by `AIGMCompanionStateAccess.cs` but its direct definition was not cleanly isolated in this pass and should remain conservative/unsafe-unknown.

---

## 1) UMGMovementIntent classification
Source file:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementIntent.cs`

Namespace:
- `Server.Custom.AIGM`

Top-level types:
- `UMGMovementIntentKind` (already ported)
- `UMGMovementIntent`

Public API / shape:
- properties:
  - `Kind`
  - `Requester`
  - `TargetMobile`
  - `DestinationName`
  - `DestinationPoint`
  - `DestinationMap`
  - `TargetSerial`
  - `TargetName`
  - `Reason`
- factory method:
  - `Create(UMGMovementIntentKind kind)`

Constructors:
- implicit default constructor only

Referenced AIGM types:
- `UMGMovementIntentKind`

Referenced ServUO types:
- `Mobile`
- `Point3D`
- `Map`

Behavior / authority assessment:
- vocabulary/model-only?: **no**, because it is more than an enum; it is a DTO/model surface
- state holder?: **yes**
- mutates world state?: **no**
- moves mobiles?: **no**
- changes Combatant / ControlTarget / ControlOrder / Home / RangeHome / CantWalk?: **no**
- starts/stops timers?: **no**
- issues pathfinding/movement commands?: **no**
- depends on tracking/travel/combat surfaces?: only as payload fields, not execution

Classification:
- **state / intent DTO**
- safe as a standalone model surface if imported without router behavior

Conclusion:
- `UMGMovementIntent` can be ported before router **if treated strictly as a passive intent DTO**.

---

## 2) UMGMovementRouter classification
Source file:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementRouter.cs`

Namespace:
- `Server.Custom.AIGM`

Top-level types:
- `UMGMovementRouter`

Public API:
- `RouteIntent(BaseHire companion, UMGMovementIntent intent, out string response)`
- `RecordIntent(BaseHire companion, UMGMovementIntent intent)`
- `SetTrackedPursuitState(BaseHire companion, AIGMCompanionTrackingEntry entry)`
- `ClearTrackedPursuitState(BaseHire companion, string reason)`
- `NoteRecoverFromStuck(BaseHire companion, string reason)`

Private routing methods:
- `RouteFollowPlayer`
- `RouteHoldPosition`
- `RouteReturnToPlayer`
- `RouteTravelToNamedDestination`
- `RouteMoveToPoint`
- `RoutePursueTrackedTarget`
- `RouteGuardTarget`

Referenced AIGM types:
- `UMGMovementIntent`
- `UMGMovementIntentKind`
- `UMGMovementState`
- `AIGMCompanionStateAccess`
- `AIGMCompanionTrackingCycle`
- `AIGMCompanionTravelController`
- `AIGMCompanionTrackingEntry`

Referenced ServUO types:
- `BaseHire`
- `Mobile`
- `World`
- `OrderType`

Behavior / authority assessment:
- vocabulary/model-only?: **no**
- mutates world state?: **yes**
- moves mobiles?: **yes, indirectly through live ServUO control surfaces and travel controller**
- changes Combatant / ControlTarget / ControlOrder / Home / RangeHome / CantWalk?: **yes**
  - sets `Combatant`
  - sets `ControlTarget`
  - sets `ControlOrder`
  - sets `CantWalk`
- starts/stops timers?: indirectly via travel/tracking/state access timing surfaces
- issues pathfinding/movement commands?: **yes**, via travel controller and direct control-order changes
- depends on tracking/travel/combat surfaces?: **yes**

Classification:
- **router / movement-authority-bearing**

Conclusion:
- `UMGMovementRouter` must **not** be ported before movement authority ownership and gates are explicitly designed.

---

## 3) UMGMovementState classification
Source file:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementState.cs`

Namespace:
- `Server.Custom.AIGM`

Top-level types:
- `UMGMovementState`

Public API / properties:
- `ActiveIntent`
- `SuspendedIntent`
- `UpdatedUtc`
- `DestinationName`
- `DestinationPoint`
- `DestinationMap`
- `TargetSerial`
- `TargetName`
- `Reason`
- `InterruptReason`
- `TrackingMode`
- `LastMovementDecision`
- `LastMovementDecisionUtc`
- `RoleProfile`
- constructor initializes defaults
- method `Apply(UMGMovementIntent intent)`

Referenced AIGM types:
- `UMGMovementIntentKind`
- `UMGMovementIntent`
- `AIGMCompanionRoleProfile`

Referenced ServUO types:
- `Point3D`
- `Map`

Behavior / authority assessment:
- vocabulary/model-only?: **no**
- state-only?: **yes, mostly**
- mutates world state?: **no**
- moves mobiles?: **no**
- changes Combatant / ControlTarget / ControlOrder / Home / RangeHome / CantWalk?: **no**
- starts/stops timers?: **no**
- issues pathfinding/movement commands?: **no**
- depends on tracking/travel/combat surfaces?: **yes, conceptually**, through movement intent, role profile, and tracking-mode strings

Classification:
- **B / state-only but depends on movement vocabulary**
- mixed state surface, but not authority-bearing by itself

Conclusion:
- `UMGMovementState` may be portable before router **only if its dependent model surfaces are explicitly split and kept passive**.
- It should not be imported bundled with router or StateAccess behavior.

---

## 4) AIGMCompanionStateAccess boundary classification
Source file:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionStateAccess.cs`

Namespace:
- `Server.Custom.AIGM`

Observed read-like methods:
- `GetGuardOwnerMode`
- `GetNextSupportActionUtc`
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
- `GetRoleProfile`
- `GetExecutionMode`

Observed mutation methods / authority leakage:
- `SetGuardOwnerMode`
- `SetNextSupportActionUtc`
- `SetTravelObjective`
- `SetNextTravelPulseUtc`
- `SetNextTravelThreatScanUtc`
- `SetTrackingEnabled`
- `SetTrackingPhase`
- `SetTrackingNextSweepUtc`
- `SetTrackingMonsterLock`
- `SetHoldPositionUntilUtc`
- `LatchHoldPosition`
- `ClearHoldPosition`
- `SetMovementState`
- `SetEngagementState`
- `SetRoleProfile`
- `SuspendMovementIntent`
- `ResumeSuspendedMovementIntent`
- `SetTrackingMode`
- `NoteLastMovementDecision`
- `ResetAllCompanionIntentState`

State dictionaries / ownership:
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

ServUO/live control mutation confirmed:
- `ResetAllCompanionIntentState` mutates:
  - `Combatant = null`
  - `ControlTarget = null`
  - `CantWalk = true`
  - `Home = companion.Location`
  - `RangeHome = 0`
  - `ControlOrder = OrderType.Stay`

Concrete companion coupling confirmed:
- `AIGMCompanionDakeyras`
- `AIGMCompanionDanyal`
- `AIGMCompanionDardalion`

Boundary classification:
- read-only?: **partially**, but mixed with authority and concrete-mobile coupling
- movement authority?: **yes, effectively** through movement/travel/tracking ownership and reset/control methods
- safe to port as-is?: **no**
- safe reduced read-only extraction possible?: **maybe later**, but requires explicit design and interface-backed data access

Conclusion:
- `AIGMCompanionStateAccess` **must remain deferred**.
- It must not become the router.
- Any future read-only adapter must exclude live control mutation, movement suspension/resume, and travel/tracking dictionary ownership unless router boundaries already exist.

---

## Missing / non-clean model surfaces inspected

### AIGMCompanionRoleProfile
Source file:
- `Scripts\Custom\AIGM\Movement\AIGMCompanionRoleProfile.cs`

Shape:
- enum values:
  - `Unknown`
  - `Melee`
  - `Ranged`
  - `Mage`
  - `Hybrid`

Assessment:
- vocabulary/model-only: **yes**
- world mutation: **no**
- movement authority: **no**
- companion-mobile-dependent: **no direct dependency**
- action-executor-dependent: **not in the enum itself**

Classification:
- **A / safe vocabulary-model-only**

Caution:
- lives under Movement and is used by `UMGMovementState`, so it should be ported only as part of an explicitly bounded model slice.

### AIGMCompanionTravelObjective
Source file:
- `Scripts\Custom\AIGM\AIGMCompanionTravelObjective.cs`

Assessment:
- vocabulary/model-only: **no**
- state-only: **not cleanly**
- contains behavior-bearing dependencies:
  - `AIGMTravelPathStrategy`
  - `AIGMTravelMemory`
  - `PathFollower`
  - `AIGMTravelRecoveryMode`
  - `AIGMStuckZone`
  - `AIGMCompanionTravelStatus`
  - `AIGMTrackingCategory`
- world mutation in class itself: **no direct mutation code**, but the model carries active controller/runtime objects and travel execution state
- movement-authority-bearing: **indirectly yes**, because it stores runtime path follower and live recovery/path state
- companion-mobile-dependent: not directly mobile-bound, but deeply travel-system-bound

Classification:
- **D / movement-authority-bearing mixed travel state**

Conclusion:
- not safe to port as a mere DTO in current form

### AIGMCompanionEngagementState
Direct definition:
- not isolated cleanly in this pass

Assessment:
- referenced by `AIGMCompanionStateAccess`
- exact structure unresolved here
- should remain conservative

Classification:
- **G / unsafe-unknown**

Conclusion:
- do not port until directly inspected and classified in a narrower future phase

### UMGMovementState
Already detailed above

Classification summary:
- **B / state-only but depends on movement vocabulary**

---

## Already-ported vocabulary re-check

### AIGMTrackingCyclePhase
- remains vocabulary-only: **yes**
- introduces movement authority: **no**
- suitable prerequisite for later movement planning: **yes**

### AIGMExecutionMode
- remains vocabulary-only: **yes**
- introduces movement authority: **no**
- suitable prerequisite for later movement planning: **yes**

### UMGMovementIntentKind
- remains vocabulary-only: **yes**
- introduces movement authority: **no**
- suitable prerequisite for later movement planning: **yes**

---

## Movement boundary table

| File / type | Category | Present in target | Present in preserved Dev | Mutates world state | Moves mobiles | Owns movement intent | Owns tracking/travel dictionaries | Touches combat/control fields | Safe to port before router | Recommended disposition |
|---|---|---|---|---|---|---|---|---|---|---|
| `AIGMExecutionMode` | vocabulary | yes | yes | no | no | no | no | no | yes | already ported prerequisite |
| `AIGMTrackingCyclePhase` | vocabulary | yes | yes | no | no | no | no | no | yes | already ported prerequisite |
| `UMGMovementIntentKind` | vocabulary | yes | yes | no | no | no | no | no | yes | already ported prerequisite |
| `UMGMovementIntent` | state / intent DTO | no | yes | no | no | yes (descriptive) | no | no | yes | best candidate for next safe mutation |
| `UMGMovementState` | state / mixed model | no | yes | no | no | yes (stores active/suspended intent) | no | no | maybe | only after companion role model decision |
| `AIGMCompanionRoleProfile` | vocabulary/model | no | yes | no | no | no | no | no | yes | likely safe companion model prerequisite |
| `AIGMCompanionTravelObjective` | mixed travel runtime state | no | yes | no direct | indirect travel system | yes | yes (via StateAccess ownership) | no direct | no | defer until router/travel authority plan |
| `AIGMCompanionEngagementState` | unknown / mixed | no | yes (referenced) | unknown | unknown | unknown | maybe | maybe | no | direct inspect later |
| `UMGMovementRouter` | router / authority | no | yes | yes | yes | yes | no direct, but uses StateAccess/travel | yes | no | do not port before router authority plan |
| `AIGMCompanionStateAccess` | mixed / authority leak | no | yes | yes | yes indirectly | yes | yes | yes | no | must remain deferred |

---

## Explicit answers

### Can UMGMovementIntent be ported before UMGMovementRouter?
- **yes**
- reason: it is a passive intent DTO and does not execute movement by itself

### Can UMGMovementState be ported without router authority?
- **maybe, but only carefully**
- reason: it is passive state by itself, but depends on movement vocabulary and `AIGMCompanionRoleProfile`; importing it safely should be a bounded model-only slice, not bundled with StateAccess/router

### Can AIGMCompanionTravelObjective be ported as a DTO?
- **no, not in its current form**
- reason: it carries active travel-runtime/path/recovery/controller state and is not a clean DTO

### Can AIGMCompanionEngagementState be ported as vocabulary/state?
- **unknown / not yet safe to say yes**
- reason: direct definition was not isolated cleanly in this pass

### Can AIGMCompanionRoleProfile be ported without companion mobile coupling?
- **yes, likely**
- reason: the enum itself is pure vocabulary/model and has no direct mobile coupling

### Must AIGMCompanionStateAccess remain deferred?
- **yes**
- reason: it crosses into movement authority, tracking/travel ownership, and live ServUO control mutation

### What is the smallest safe next mutation?
- **port `UMGMovementIntent` as a passive DTO, likely alongside `AIGMCompanionRoleProfile` if needed by the next model slice**
- do **not** port router or StateAccess with it

---

## Next safe movement sequence decision

### Option analysis
- **Option A — Phase 56Q-R4-MOVEMENT-MODEL**
  - plausible if the slice is limited to passive movement/state model surfaces
  - could include `UMGMovementIntent` and possibly `AIGMCompanionRoleProfile`
  - `UMGMovementState` is possible only if kept clearly passive

- **Option B — Phase 56Q-R4-MOVEMENT-INTENT**
  - strongest immediate candidate
  - `UMGMovementIntent` is the cleanest next DTO surface
  - minimal risk, clear boundary, no authority import

- **Option C — Phase 56Q-R4-ROUTER-PLAN**
  - still necessary before router or StateAccess work
  - but not required before importing the passive intent DTO itself

- **Option D — Phase 56Q-R4-STATEACCESS-READONLY-PLAN**
  - premature right now; StateAccess remains too entangled

- **Option E — Phase 56Q-R2-M — Return to action executor manual reconciliation**
  - not indicated by this planning pass; movement intent DTO can proceed independently first

## Recommended next phase
- **Recommended next phase: Phase 56Q-R4-MOVEMENT-INTENT — Port UMGMovementIntent only + build**

Reason:
- `UMGMovementIntent` is a passive DTO
- it does not move mobiles
- it does not mutate world state
- it does not import router authority
- it respects the rule that movement router remains the sole movement authority

Follow-on after that:
- likely a bounded model phase for `AIGMCompanionRoleProfile` and possibly `UMGMovementState` if still needed
- then a dedicated router-authority design phase before any router/StateAccess import

## Confirmations
- confirmation no files were copied: **confirmed**
- confirmation no patch was applied: **confirmed**
- confirmation no command/action/parser/skill/companion mobile files were changed: **confirmed**
- confirmation no commit occurred: **confirmed**

## Final boundary statement
Movement authority must stay inside an explicit router layer.
`AIGMCompanionStateAccess.cs` must not be used as a shortcut router.
Tracking remains scan/report by default, and pursuit remains an explicit movement intent routed through a future authority boundary.
