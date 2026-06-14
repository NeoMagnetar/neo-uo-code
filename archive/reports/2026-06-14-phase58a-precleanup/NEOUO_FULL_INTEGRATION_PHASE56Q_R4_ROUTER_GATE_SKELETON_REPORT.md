# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER-GATE-SKELETON REPORT

Date: 2026-06-08 09:30 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `a66a4b35ad884c32df52cc9e59fde34ee5d65713`
- latest commit: `a66a4b3 feat: add UMG movement gate model`

## Baseline build result before router gate methods
Command:
```text
dotnet build .\ServUO.sln -v:minimal
```
Result:
```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
- baseline result: **PASSED / clean**

## Target router inspected
Inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

Router methods before this phase:
- `TrySubmitIntent(...)`
- `ApplyIntentToState(...)`
- `Hold(...)`
- `Stop(...)`
- `Suspend(...)`
- `Resume(...)`
- `CombatInterruption(...)`
- `Clear()`
- `GetState()`

Router remained:
- non-executing
- state-only
- instance-based around bounded `UMGMovementState`

## Gate model inspected
Inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementGateKind.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateDecision.cs`

Available gate model helpers used:
- `UMGMovementGateKind`
- `UMGMovementGateDecision.Allow(...)`
- `UMGMovementGateDecision.Deny(...)`

## Movement state / intent surfaces inspected
Inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementIntentKind.cs`

Available state/intent features used by router gate methods:
- `UMGMovementState.Apply(...)`
- `UMGMovementState.Clear()`
- active/suspended intent fields
- destination/target metadata preservation via bounded state

No mismatch blocked gate integration.

## Gate methods added
Added to `UMGMovementRouter.cs`:
- `EvaluateGate(UMGMovementGateKind gateKind, UMGMovementIntent intent = null, string reason = null)`
- `EvaluateIntentGate(UMGMovementIntent intent)`
- `EvaluateTrackingGate(UMGMovementIntent intent)`
- `EvaluatePursuitGate(UMGMovementIntent intent)`
- `EvaluateHoldGate(string reason = null)`
- `EvaluateStopGate(string reason = null)`
- `EvaluateResumeGate(string reason = null)`
- `EvaluateCombatInterruptionGate(string reason = null)`

## How TrySubmitIntent uses gates
`TrySubmitIntent(...)` now:
- calls `EvaluateIntentGate(intent)` first
- reads the returned `UMGMovementGateDecision`
- denies if:
  - decision is null
  - decision is not allowed
  - state update is not allowed
- only then applies bounded state behavior
- still does **not** execute movement

## How Hold / Stop / Suspend / Resume / CombatInterruption use gates
### Hold
- calls `EvaluateHoldGate(...)`
- only applies state-only hold if gate allows state update
- may suspend active intent in bounded state only

### Stop
- calls `EvaluateStopGate(...)`
- only clears/suspends bounded state if gate allows state update
- does not reset live control state

### Suspend
- remains bounded-state-only helper
- used by allowed higher-level gate-approved flows
- does not execute movement

### Resume
- calls `EvaluateResumeGate(...)`
- denies resume when there is no suspended intent
- restores suspended intent in bounded state only

### CombatInterruption
- calls `EvaluateCombatInterruptionGate(...)`
- only suspends applicable active intents in bounded state
- preserves destination/target metadata
- does not mutate combat fields

## Gate behavior implemented in this phase
### MovementExecutionGate
- live movement execution remains denied
- state updates may still be allowed where appropriate

### TrackingScanGate
- returns non-movement scan/report-only decisions
- cannot cause movement

### PursuitGate
- requires explicit `PursueTrackedTarget` intent
- may record pursuit intent in bounded state only
- cannot move or mutate control target

### TravelGate
- allows travel intent recording only
- does not pathfind or move

### FollowGuardGate
- allows follow/guard intent recording only
- does not issue live follow/guard behavior

### HoldGate
- allows state-only autonomy suppression
- does not mutate `CantWalk` or other live control fields

### StopGate
- allows state-only clear/suspend behavior
- does not reset live companion control state

### ResumeGate
- allows state-only resume only when suspended intent exists
- does not move

### CombatInterruptionGate
- allows state-only suspension without erasing destination state
- does not mutate `Combatant`

### Boundary gates
Implemented as deny decisions:
- `StateAccessBoundary`
- `ParserBoundary`
- `SkillExecutorBoundary`

## Non-execution confirmations
- confirmation live movement is still denied: **confirmed**
- confirmation tracking scan/report cannot cause movement: **confirmed**
- confirmation pursuit requires explicit pursuit intent: **confirmed**
- confirmation router remains non-executing: **confirmed**
- confirmation `StateAccess` was not copied or referenced: **confirmed**
- confirmation no live ServUO control/combat mutation was introduced: **confirmed**
- confirmation only `UMGMovementRouter.cs` changed: **confirmed**

## Forbidden-string scan result
Scanned `UMGMovementRouter.cs` for:
- `Combatant`
- `ControlTarget`
- `ControlOrder`
- `CantWalk`
- `Home`
- `RangeHome`
- `MoveToWorld`
- `SetLocation`
- `Location =`
- `Direction =`
- `AIObject`
- `DelayCall`
- `Timer`
- `AIGMCompanionStateAccess`
- `AIGMCompanionDakeyras`
- `AIGMCompanionDanyal`
- `AIGMCompanionDardalion`

Result:
- **all clear; no forbidden strings found**

## Build result after router gate methods
Command:
```text
dotnet build .\ServUO.sln -v:minimal
```
Result:
```text
Build succeeded.
    15 Warning(s)
    0 Error(s)
```

## Warning count / files
Warning count after router gate methods:
- **15**

Warning files observed:
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
- `Scripts\Gumps\AIGMResponseGump.cs`
- `Scripts\Gumps\AIGMQuestionGump.cs`
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

Important warning note:
- warnings do **not** touch `UMGMovementRouter.cs`
- warnings do **not** touch movement/gate files
- warnings remain the known unrelated set

## Recommendation for next phase
Recommended next phase:
- **Phase 56Q-R4-ROUTER-GATE-SKELETON-P — Commit Non-Executing Router Gate Methods**

Suggested commit message:
- `feat: add non-executing UMG movement router gates`

## Likely next phase after commit
- **Phase 56Q-R4-ROUTER-STATE-TRANSITIONS — Refine HOLD / STOP / SUSPEND / RESUME state transitions only + build**

Reason:
- gate decisions now exist and are wired into the non-executing router
- the next safe refinement is state-policy quality, not live movement execution
