# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER-EXECUTOR-WIRING REPORT

Date: 2026-06-08 10:52 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `741e9ce203a934020bf460af36ce4d41ad77d8c5`
- latest commit: `741e9ce feat: add no-op UMG movement executor`

## Baseline build result before wiring
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

Current router constructors before this phase:
- `UMGMovementRouter(UMGMovementState existingState = null)`

Current `TrySubmitIntent(...)` behavior before this phase:
- evaluates `EvaluateIntentGate(...)`
- denies if no decision or gate blocks state update
- applies bounded state only for allowed intents
- did not call any executor yet

Current router gate methods before this phase:
- `EvaluateGate(...)`
- `EvaluateIntentGate(...)`
- `EvaluateTrackingGate(...)`
- `EvaluatePursuitGate(...)`
- `EvaluateHoldGate(...)`
- `EvaluateStopGate(...)`
- `EvaluateResumeGate(...)`
- `EvaluateCombatInterruptionGate(...)`

## Execution contract files inspected
Inspected:
- `IUMGMovementExecutor.cs`
- `UMGMovementExecutionRequest.cs`
- `UMGMovementExecutionResult.cs`

Confirmed:
- execute signature:
  - `UMGMovementExecutionResult Execute(UMGMovementExecutionRequest request)`
- request supports dry-run, gate decision, state snapshot/reference, reason, actor identity/profile
- result supports dry-run/denied/failure helpers and result capture

## No-op executor inspected
Inspected:
- `UMGMovementNoOpExecutor.cs`

Confirmed behavior:
- null-safe
- denies missing gate decision
- denies denied gates
- returns dry-run when `IsDryRun == true`
- never moves anything
- never mutates request state or world state

## Exact router changes made
Modified only:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

Added/changed:
- private field:
  - `IUMGMovementExecutor _executor`
- constructor overload:
  - `UMGMovementRouter(UMGMovementState existingState, IUMGMovementExecutor executor)`
- preserved parameterless/default-compatible constructor behavior via delegation
- new property:
  - `UMGMovementExecutionResult LastExecutionResult { get; private set; }`
- new helpers:
  - `TryExecuteNoOp(...)`
  - `ShouldSubmitToExecutor(...)`
  - `BuildExecutionRequest(...)`
- `TrySubmitIntent(...)` now records `LastExecutionResult` for movement-execution candidate intents only
- state-control methods remain bounded-state only and do not submit execution requests

## Constructor / injection behavior
- original construction style preserved:
  - `new UMGMovementRouter()` still works through the existing optional-state constructor path
- constructor injection added safely:
  - caller may optionally pass an `IUMGMovementExecutor`
- if no executor is provided:
  - router defaults to `new UMGMovementNoOpExecutor()`

## Default executor behavior
- default executor is the no-op executor
- therefore router can safely build dry-run execution requests without introducing world mutation

## Null executor behavior
- constructor normalizes null executor to `new UMGMovementNoOpExecutor()`
- helper `TryExecuteNoOp(...)` also defensively falls back to a new no-op executor if needed
- no exception path introduced for missing executor

## Which intents submit dry-run execution requests
These intent kinds now submit a dry-run execution request after bounded state application:
- `MoveToPoint`
- `TravelToNamedDestination`
- `FollowPlayer`
- `ReturnToPlayer`
- `GuardTarget`
- `PursueTrackedTarget`

## Which intents do not submit execution requests
These do **not** submit execution requests in this phase:
- `HoldPosition`
- `Idle`
- `RecoverFromStuck`
- all state-control methods:
  - `Hold(...)`
  - `Stop(...)`
  - `Suspend(...)`
  - `Resume(...)`
  - `CombatInterruption(...)`
  - `Clear()`

Reason:
- these remain bounded-state transitions only
- tracking/reporting remains non-executing
- no implicit promotion into movement occurs

## Confirmation every execution request is dry-run
- every request built by `BuildExecutionRequest(...)` sets:
  - `IsDryRun = true`
- router does not create any non-dry-run execution request in this phase

## LastExecutionResult behavior
- `LastExecutionResult` now captures the most recent no-op executor result for eligible movement-candidate intents
- it is cleared to `null` when gate denial prevents submission or when bounded-state-only methods like `Clear()` intentionally reset execution context

## Non-execution confirmations
- confirmation router remains non-executing: **confirmed**
- confirmation router does not move mobiles: **confirmed**
- confirmation no live movement authority was introduced: **confirmed**
- confirmation no `BaseHire` / `Mobile` dependency was introduced: **confirmed**
- confirmation `StateAccess` was not copied or referenced: **confirmed**
- confirmation parser / skill / companion / action / command files were not changed: **confirmed**
- confirmation only `UMGMovementRouter.cs` changed: **confirmed**

## Forbidden-string scan result
Scanned `UMGMovementRouter.cs` for:
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
- **all clear; no forbidden strings found**

## Build result after wiring
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
Warning count after wiring:
- **15**

Warning files observed:
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
- `Scripts\Gumps\AIGMResponseGump.cs`
- `Scripts\Gumps\AIGMQuestionGump.cs`
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

Important warning note:
- warnings do **not** touch `UMGMovementRouter.cs`
- warnings do **not** touch movement/execution files
- warnings remain the known unrelated set

## Recommendation for next phase
Recommended next phase:
- **Phase 56Q-R4-ROUTER-EXECUTOR-WIRING-P — Commit Router-to-No-Op Executor Wiring**

Suggested commit message:
- `feat: wire UMG router to no-op movement executor`

## Likely next phase after commit
- **Phase 56Q-R4-MOVE-TO-POINT-LIVE-PLAN — Plan first real MOVE_TO_POINT execution adapter slice**

Alternative:
- **Phase 56Q-R4-SERVUO-MOVEMENT-ADAPTER-CONTRACT — Add ServUO adapter boundary contract only + build**

Reason:
- the router can now exercise an execution-shaped path safely without mutating the world
- the next safe decision is whether to plan the first real movement candidate directly or formalize a ServUO adapter boundary first
