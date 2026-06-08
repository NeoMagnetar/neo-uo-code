# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER-GATE-MODEL REPORT

Date: 2026-06-08 09:21 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `b613fe7cd22e5b5d035aac2607790614a6571b32`
- latest commit: `b613fe7 feat: add non-executing UMG movement router skeleton`

## Baseline build result before gate model
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

## Router-gates plan report inspected
Inspected:
- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\NEOUO_FULL_INTEGRATION_PHASE56Q_R4_ROUTER_GATES_PLAN_REPORT.md`

Extracted from the plan:
- gate names:
  - `MovementExecution`
  - `Pursuit`
  - `TrackingScan`
  - `Travel`
  - `FollowGuard`
  - `Hold`
  - `Stop`
  - `Resume`
  - `CombatInterruption`
  - `StateAccessBoundary`
  - `ParserBoundary`
  - `SkillExecutorBoundary`
- gate result needs:
  - allow/deny result
  - reason text
  - whether state updates are allowed
  - whether live movement is allowed
  - whether explicit pursuit intent is required
  - scan/report-only flags
  - suspend/clear semantics
  - destination preservation semantics

## Movement model files inspected
Inspected:
- `Scripts/Custom/AIGM/Movement/UMGMovementIntentKind.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

Confirmed:
- namespace pattern is `Server.Custom.AIGM`
- movement model style is small enum/model classes in `Scripts/Custom/AIGM/Movement`
- no existing file required modification for this phase

## Files added
Added:
- `Scripts/Custom/AIGM/Movement/UMGMovementGateKind.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateDecision.cs`

Optional file added?
- `UMGMovementAuthorityPolicy.cs`: **no**

Reason not added yet:
- the plan supports starting with the smallest possible compile-safe model set
- gate kind + gate decision are sufficient for the next non-executing gate-method phase

## Exact enum values added
In `UMGMovementGateKind.cs`:
- `MovementExecution = 0`
- `Pursuit`
- `TrackingScan`
- `Travel`
- `FollowGuard`
- `Hold`
- `Stop`
- `Resume`
- `CombatInterruption`
- `StateAccessBoundary`
- `ParserBoundary`
- `SkillExecutorBoundary`

## Exact decision properties added
In `UMGMovementGateDecision.cs`:
- `UMGMovementGateKind GateKind`
- `bool IsAllowed`
- `string Reason`
- `bool AllowsStateUpdate`
- `bool AllowsLiveMovement`
- `bool RequiresExplicitIntent`
- `bool IsScanOnly`
- `bool IsReportOnly`
- `bool SuspendsActiveIntent`
- `bool ClearsActiveIntent`
- `bool PreservesDestination`

Static helpers added:
- `Allow(...)`
- `Deny(...)`

These helpers are data-only object factories and do not mutate world state.

## Gate model data-only confirmation
- gate model is data-only: **confirmed**
- no router code was modified: **confirmed**
- no live movement authority was introduced: **confirmed**
- `StateAccess` was not copied or referenced: **confirmed**
- parser / skill / companion / action / command files were not changed: **confirmed**

## Forbidden-string scan result
Scanned new files for forbidden strings:
- `Mobile`
- `BaseHire`
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
- **no forbidden strings found**

## Scope verification
Changed files in movement folder:
- `Scripts/Custom/AIGM/Movement/UMGMovementGateKind.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementGateDecision.cs`

Confirmed unchanged:
- `UMGMovementRouter.cs`
- `UMGMovementState.cs`
- `UMGMovementIntent.cs`
- `UMGMovementIntentKind.cs`
- `AIGMCompanionStateAccess.cs`
- parser files
- skill executor files
- companion mobile files
- command/action files
- bridge/speech/gump files

## Build result after gate model
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
Warning count after gate model:
- **15**

Warning files observed:
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
- `Scripts\Gumps\AIGMResponseGump.cs`
- `Scripts\Gumps\AIGMQuestionGump.cs`
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

Important warning note:
- warnings do **not** touch the new gate model files
- warnings remain the known unrelated set
- no warning repair was attempted in this phase

## Error classification
- errors after gate model: **none**
- no enum/model implementation error observed
- no namespace/path mismatch observed
- no accidental dependency on router execution observed
- no accidental dependency on `StateAccess` observed

## Recommendation for next phase
Recommended next phase:
- **Phase 56Q-R4-ROUTER-GATE-MODEL-P — Commit Movement Gate Model**

Suggested commit message:
- `feat: add UMG movement gate model`

## Likely next phase after commit
- **Phase 56Q-R4-ROUTER-GATE-SKELETON — Add non-executing gate methods to router + build**

Reason:
- the minimal gate vocabulary/result surfaces now exist
- the next safe step is wiring non-executing gate methods into the router skeleton without introducing live movement
