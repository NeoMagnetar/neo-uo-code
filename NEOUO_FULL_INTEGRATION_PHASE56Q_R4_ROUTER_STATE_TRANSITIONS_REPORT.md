# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER-STATE-TRANSITIONS REPORT

Date: 2026-06-08 09:44 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `a4cac3a741c31875b763a4ff9911d320fc78875d`
- latest commit: `a4cac3a feat: add non-executing UMG movement router gates`

## Baseline build result before transition refinement
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

## Router inspected
Inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

Transition methods before refinement:
- `Hold(...)`
- `Stop(...)`
- `Suspend(...)`
- `Resume(...)`
- `CombatInterruption(...)`
- `Clear()`

Router already had gate evaluation, but transition semantics were still fairly loose.

## Movement state inspected
Inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`

Available state support used by this phase:
- `ActiveIntent`
- `SuspendedIntent`
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
- `UpdatedUtc`
- `Clear()`
- `Reset()` via `Clear()`

## Gate model inspected
Inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementGateDecision.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateKind.cs`

Helpers used:
- `Allow(...)`
- `Deny(...)`
- `SuspendsActiveIntent`
- `ClearsActiveIntent`
- `PreservesDestination`

## Transition behavior refined
Modified only:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

Refinements made:
- standardized state-only reason strings for transition paths
- prevented redundant/invalid suspend behavior when active intent is already `Idle` or `HoldPosition`
- introduced no-op/ignored transition outcomes where current bounded state does not support meaningful action
- tightened combat-interruption behavior to suspend only interruptible intents
- prevented resume while hold is still active
- ensured clear operation records bounded-state-only clear metadata

### HOLD refinement
- now uses helper logic to suspend only suspendable active intents
- sets explicit bounded-state reason fallback: `hold_requested`
- remains state-only

### STOP refinement
- now suspends only if current intent is suspendable
- sets explicit bounded-state reason fallback: `stop_requested`
- clears active intent to `Idle` without any live reset
- remains state-only

### SUSPEND refinement
- now no-ops cleanly when no suspendable active intent exists
- records `SuspendIgnored` when applicable
- otherwise moves active intent into suspended intent and sets active to `Idle`
- preserves destination/target metadata because bounded state is not cleared

### RESUME refinement
- now records resumed intent more explicitly
- denies resume when no suspended intent exists through gate logic
- also denies resume while HOLD remains active
- records `ResumeNoOp` if nothing was actually restored
- remains state-only

### COMBAT_INTERRUPTION refinement
- now suspends only interruptible intents using helper classification
- records `CombatInterruptionIgnored` when current active intent is not interruptible
- preserves destination/target metadata
- remains state-only

### CLEAR refinement
- still clears only bounded model state
- now records `clear_requested` reason plus updated timestamps after clear
- remains state-only

## Missing state fields / methods
No hard blocker prevented refinement, but the bounded model still lacks richer state for future work, including:
- explicit hold-latched vs temporary hold field
- explicit stop-policy mode (clear vs suspend) field
- explicit source/caller attribution for transitions
- explicit combat-interruption provenance field
- explicit gate-audit trail field

These are future model improvements, not blockers for this phase.

## Confirmations
- confirmation HOLD remains state-only: **confirmed**
- confirmation STOP remains state-only: **confirmed**
- confirmation SUSPEND remains state-only: **confirmed**
- confirmation RESUME remains state-only: **confirmed**
- confirmation COMBAT_INTERRUPTION remains state-only: **confirmed**
- confirmation CLEAR remains state-only: **confirmed**
- confirmation router remains non-executing: **confirmed**
- confirmation router does not move mobiles: **confirmed**
- confirmation router does not mutate ServUO control/combat fields: **confirmed**
- confirmation `StateAccess` was not copied or referenced: **confirmed**
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

## Build result after transition refinement
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
Warning count after transition refinement:
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
- **Phase 56Q-R4-ROUTER-STATE-TRANSITIONS-P — Commit Non-Executing Router State Transitions**

Suggested commit message:
- `feat: refine non-executing UMG router state transitions`

## Likely next phase after commit
- **Phase 56Q-R4-ROUTER-LIVE-EXECUTION-PLAN — Live Movement Execution Boundary Plan**

Alternative if deeper model gaps matter first:
- **Phase 56Q-R4-MOVEMENT-STATE-GAPS — Bounded Movement State Gap Plan**

Reason:
- state-only router transitions are now more deliberate and guarded
- the next big decision is whether to plan live execution boundaries or close bounded-state modeling gaps first
