# NEOUO FULL INTEGRATION PHASE56Q-R4 ROUTER-SKELETON REPORT

Date: 2026-06-08 09:02 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `a1eb50290ecd59cd4c098e17ae44e9b7ddf0e12d`
- latest commit: `a1eb502 feat: add bounded UMG movement state model`

## Baseline build result before skeleton
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

## Preserved Dev router inspected
Source file inspected:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementRouter.cs`

Preserved router findings carried into adaptation:
- namespace: `Server.Custom.AIGM`
- static authority router in preserved Dev
- depends on:
  - `AIGMCompanionStateAccess`
  - `AIGMCompanionTrackingCycle`
  - `AIGMCompanionTravelController`
  - `BaseHire`
  - `Mobile`
  - live ServUO control mutation
- writes live control fields in preserved Dev:
  - `CantWalk`
  - `Combatant`
  - `ControlTarget`
  - `ControlOrder`
- therefore preserved file was **not** copied as-is

## Target movement surfaces inspected
Confirmed compile-safe target surfaces available:
- `UMGMovementIntentKind`
- `UMGMovementIntent`
- bounded `UMGMovementState`
- `AIGMExecutionMode`
- `AIGMTrackingCyclePhase`
- `IAIGMCompanionActor`

Confirmed target model availability for skeleton:
- `UMGMovementState.Apply(...)`: available
- `UMGMovementState.Reset()`: available
- `UMGMovementState.Clear()`: available
- no `StateAccess` requirement for skeleton compile
- no live mobile requirement for skeleton compile

## Skeleton adaptation strategy
Strategy used:
- create a brand-new non-executing target `UMGMovementRouter`
- make it instance-based around a bounded `UMGMovementState`
- allow intent submission and state-policy transitions only
- preserve movement semantics at the state/policy level without live execution
- do **not** import any preserved Dev live control mutation

Implemented non-executing methods:
- `TrySubmitIntent(UMGMovementIntent intent, out string reason)`
- `ApplyIntentToState(UMGMovementIntent intent)`
- `Hold(string reason = null)`
- `Stop(string reason = null)`
- `Suspend(string reason = null)`
- `Resume(string reason = null)`
- `CombatInterruption(string reason = null)`
- `Clear()`
- `GetState()`

## Exact files changed
Created:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

No other code files were changed.

## Non-executing skeleton confirmation
The new router skeleton:
- accepts `UMGMovementIntent`
- updates bounded `UMGMovementState`
- records policy/state transitions only
- does not execute movement
- does not move mobiles
- does not use live ServUO control mutation
- does not use timers
- does not use pathfinding
- does not use tracking/travel dictionaries

## Forbidden-string verification
Confirmed absent from the new router skeleton:
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

## Scope verification
Expected changed file:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`

Forbidden code files confirmed unchanged:
- `AIGMCompanionStateAccess.cs`
- `UMGMovementIntent.cs`
- `UMGMovementState.cs`
- `UMGMovementIntentKind.cs`
- `AIGMCompanionTravelObjective.cs`
- `AIGMCompanionEngagementState.cs`
- `AIGMCompanionRoleProfile.cs`
- parser files
- skill executor files
- companion mobile files
- command/action files
- bridge/speech/gump files

## Build result after skeleton
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
Warning count after router skeleton:
- **15**

Warning files observed:
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs`
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
- `Scripts\Gumps\AIGMResponseGump.cs`
- `Scripts\Gumps\AIGMQuestionGump.cs`

Important warning note:
- warnings do **not** touch `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- warnings are the known unrelated set
- no movement/router warning file appeared

## Error classification
- errors after skeleton: **none**
- no skeleton implementation error observed
- no namespace/path mismatch observed
- no mismatch with `UMGMovementIntent` observed
- no mismatch with bounded `UMGMovementState` observed
- no accidental `StateAccess` dependency observed
- no accidental live ServUO movement/control dependency observed

## Confirmations
- confirmation skeleton is non-executing: **confirmed**
- confirmation router does not move mobiles: **confirmed**
- confirmation router does not mutate ServUO control/combat fields: **confirmed**
- confirmation `StateAccess` was not copied or referenced: **confirmed**
- confirmation full preserved router was not copied as-is: **confirmed**
- confirmation command/action/parser/skill/companion mobile files were not changed: **confirmed**

## Recommendation for next phase
Recommended next phase:
- **Phase 56Q-R4-ROUTER-SKELETON-P — Commit Non-Executing Movement Router Skeleton**

Suggested commit message:
- `feat: add non-executing UMG movement router skeleton`

## Likely next phase after commit
- **Phase 56Q-R4-ROUTER-GATES-PLAN — Movement Gate / Arbiter Design Plan**

Reason:
- once the skeleton exists, the next safe question is not full movement execution
- the next safe question is which explicit gate allows live movement execution, pursuit, hold, stop, resume, and combat interruption
