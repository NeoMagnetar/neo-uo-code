# NEOUO FULL INTEGRATION PHASE56Q-R3-STATEACCESS-PLAN REPORT

Date: 2026-06-07 19:24:00 -09:00

## Target state
- branch: neo/staging-aigm
- HEAD: 5b00ae8bc227317f7d119e4ac180537fc035a154
- latest commit: 5b00ae8bc feat: add shared companion skill executor

## Baseline build result
```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  Ultima -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Ultima.dll
  Server -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\ServUO.exe
  Scripts -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```
- result: PASSED

## State access file inspected
- file: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionStateAccess.cs`

### Public methods / responsibilities observed
- companion runtime state access:
  - `GetGuardOwnerMode` / `SetGuardOwnerMode`
  - `GetNextSupportActionUtc` / `SetNextSupportActionUtc`
- travel state access:
  - `GetTravelObjective` / `SetTravelObjective`
  - `GetNextTravelPulseUtc` / `SetNextTravelPulseUtc`
  - `GetNextTravelThreatScanUtc` / `SetNextTravelThreatScanUtc`
- tracking cycle state access:
  - `GetTrackingEnabled` / `SetTrackingEnabled`
  - `GetTrackingPhase` / `SetTrackingPhase`
  - `GetTrackingNextSweepUtc` / `SetTrackingNextSweepUtc`
  - `GetTrackingMonsterLock` / `SetTrackingMonsterLock`
- position / hold state access:
  - `GetHoldPositionUntilUtc` / `SetHoldPositionUntilUtc`
  - `LatchHoldPosition`
  - `ClearHoldPosition`
  - `IsHoldPositionLatched`
- movement state access:
  - `GetMovementState` / `SetMovementState`
  - `SetRoleProfile` / `GetRoleProfile`
  - `SuspendMovementIntent`
  - `ResumeSuspendedMovementIntent`
  - `SetTrackingMode`
  - `NoteLastMovementDecision`
- engagement state access:
  - `GetEngagementState` / `SetEngagementState`
- broad reset / behavior mutation:
  - `ResetAllCompanionIntentState`
- execution mode access:
  - `GetExecutionMode`

### Concrete companion coupling still present in source
- `AIGMCompanionDakeyras`
- `AIGMCompanionDanyal`
- `AIGMCompanionDardalion`
- used for:
  - `GuardOwnerMode`
  - `NextSupportActionUtc`
  - `ExecutionMode`

### Movement / behavior leakage observed
This file is **not** just passive state vocabulary.

It directly:
- owns movement-state dictionaries
- suspends/resumes movement intent
- latches tracking mode / movement decision text
- resets control state on live companions:
  - `Combatant = null`
  - `ControlTarget = null`
  - `CantWalk = true`
  - `Home = companion.Location`
  - `RangeHome = 0`
  - `ControlOrder = OrderType.Stay`

That crosses the movement-authority boundary.

### World mutation / behavior summary
- yes, world/live mobile mutation exists
- yes, movement-state mutation exists
- yes, tracking/travel state mutation exists
- no direct action-executor calls observed in this file itself

## Missing dependency definitions inspected

### UMGMovementState
- source file: `Scripts\Custom\AIGM\Movement\UMGMovementState.cs`
- kind: state class
- state-only?: mostly data-holding, but **not pure vocabulary**
- world mutation?: no direct world mutation inside the class itself
- movement/router execution dependency?: **yes, indirectly** via `Apply(UMGMovementIntent)` and role-profile coupling
- companion mobile dependency?: no direct concrete companion dependency inside the type
- action executor dependency?: no direct dependency observed
- safely portable as model-only surface?: **maybe, but only with care**
- classification: **H / risky mixed state surface**

### UMGMovementIntentKind
- source file: `Scripts\Custom\AIGM\Movement\UMGMovementIntent.cs`
- kind: enum
- state-only?: yes, pure vocabulary enum
- world mutation?: no
- movement/router execution dependency?: no direct execution code in enum itself
- companion mobile dependency?: no
- action executor dependency?: no
- safely portable as model-only surface?: **yes**
- classification: **B / safe model-vocabulary**

### AIGMCompanionTravelObjective
- source surface found in:
  - `Scripts\Custom\AIGM\AIGMCompanionAutoPathNavigator.cs`
  - `Scripts\Custom\AIGM\AIGMCompanionMapNavigator.cs`
  - `Scripts\Custom\AIGM\AIGMCompanionStateAccess.cs`
- kind: travel state / navigation objective surface
- state-only?: no
- world mutation?: participates in movement/travel orchestration indirectly
- movement/router execution dependency?: **yes**
- companion mobile dependency?: yes, navigators operate on `BaseHire`
- action executor dependency?: no direct dependency established here
- safely portable as model-only surface?: **no**
- classification: **D / travel-tracking behavior dependency**

### AIGMTrackingCyclePhase
- source file: `Scripts\Custom\AIGM\AIGMCompanionTrackingCycle.cs`
- kind: enum
- state-only?: yes, pure cycle vocabulary
- world mutation?: no in the enum itself
- movement/router execution dependency?: no in the enum itself
- companion mobile dependency?: no in the enum itself
- action executor dependency?: no
- safely portable as model-only surface?: **yes**
- classification: **B / safe model-vocabulary**

### AIGMCompanionEngagementState
- exact definition file not isolated in this phase, but target/dev symbol scan shows it is absent from target and used by `AIGMCompanionStateAccess`
- state-only?: likely state object, but unresolved enough to stay conservative
- world mutation?: unknown from direct source read here
- movement/router execution dependency?: unknown
- companion mobile dependency?: unknown
- action executor dependency?: unknown
- safely portable as model-only surface?: **uncertain**
- classification: **H / risky-unknown**

### AIGMCompanionRoleProfile
- source location: `Scripts\Custom\AIGM\Movement\AIGMCompanionRoleProfile.cs`
- kind: role vocabulary surface
- state-only?: likely enum/model-like, but importantly it lives inside the Movement surface tree
- world mutation?: none proven directly here
- movement/router execution dependency?: **coupled by location and usage in UMGMovementState / action executor**
- companion mobile dependency?: none proven directly here
- action executor dependency?: referenced by action executor
- safely portable as model-only surface?: **possible, but not yet clean enough to recommend blindly**
- classification: **H / risky-coupled vocabulary**

### AIGMExecutionMode
- source file: `Scripts\Custom\AIGM\AIGMExecutionMode.cs`
- kind: enum
- state-only?: yes
- world mutation?: no
- movement/router execution dependency?: no direct dependency
- companion mobile dependency?: no
- action executor dependency?: no direct dependency in the enum itself
- safely portable as model-only surface?: **yes**
- classification: **B / safe model-vocabulary**

## Target existence table
| Symbol | Present in target | Present in preserved Dev | Notes |
|---|---|---|---|
| `UMGMovementState` | no | yes | absent from target |
| `UMGMovementIntentKind` | no | yes | enum lives in movement intent file |
| `AIGMCompanionTravelObjective` | no | yes | tied to travel/navigation surfaces |
| `AIGMTrackingCyclePhase` | no | yes | enum inside tracking-cycle file |
| `AIGMCompanionEngagementState` | no | yes | missing and not cleanly classified yet |
| `AIGMCompanionRoleProfile` | no | yes | located under Movement |
| `AIGMExecutionMode` | no | yes | pure enum |

## Dependency table
| Source member/method | Referenced symbol | Present in target | Present in preserved Dev | Category | Required for compile if StateAccess is ported | Safe to port before movement/router | Recommended disposition |
|---|---|---|---|---|---|---|---|
| `GetGuardOwnerMode` / `SetGuardOwnerMode` | concrete companion classes | no | yes | F | yes | no | already superseded conceptually by shared actor surface; do not port source form |
| `GetNextSupportActionUtc` / `SetNextSupportActionUtc` | concrete companion classes | no | yes | F | yes | no | already solved in shared actor / skill lane; do not re-import through StateAccess |
| `GetTravelObjective` / `SetTravelObjective` | `AIGMCompanionTravelObjective` | no | yes | D | yes | no | defer until travel/movement plan |
| tracking phase methods | `AIGMTrackingCyclePhase` | no | yes | B | yes | yes | candidate for model-only patch |
| movement state methods | `UMGMovementState` | no | yes | C | yes | no / uncertain | defer until movement vocabulary/state lane is explicit |
| engagement state methods | `AIGMCompanionEngagementState` | no | yes | H | yes | uncertain | inspect separately before any patch |
| `SetRoleProfile` / `GetRoleProfile` | `AIGMCompanionRoleProfile` | no | yes | H | yes | uncertain | inspect separately; not first safe slice |
| `SuspendMovementIntent` / `ResumeSuspendedMovementIntent` | `UMGMovementIntentKind` + `UMGMovementState` | no | yes | C | yes | no | movement-authority behavior; defer |
| `SetTrackingMode` / `NoteLastMovementDecision` | `UMGMovementState` | no | yes | C | yes | no / uncertain | defer |
| `ResetAllCompanionIntentState` | live companion control state | yes | yes | C/D/E mix | yes | no | do not port before movement boundary is explicit |
| `GetExecutionMode` | `AIGMExecutionMode` + concrete companion classes | no | yes | B + F mix | yes | enum yes, source form no | enum may be ported later as vocabulary; source method should not be ported as-is |

## Classification summary
- **UMGMovementState:** mixed state surface, not safe as an immediate blind model-only port
- **UMGMovementIntentKind:** pure vocabulary enum, safest movement-related slice found
- **AIGMCompanionTravelObjective:** behavior-coupled travel/navigation surface, not safe before movement/router
- **AIGMTrackingCyclePhase:** pure vocabulary enum, safe candidate
- **AIGMCompanionEngagementState:** unresolved/risky until definition is inspected directly
- **AIGMCompanionRoleProfile:** likely vocabulary-ish, but coupled enough to Movement/Action surfaces that it should not be blindly imported next
- **AIGMExecutionMode:** pure enum, safe candidate

## Slice evaluation
### Slice 1 — Model vocabulary only
Candidate members:
- `AIGMTrackingCyclePhase`
- `AIGMExecutionMode`
- possibly later: `AIGMCompanionEngagementState`, `AIGMCompanionRoleProfile` after direct inspection

Recommendation:
- **yes, partially recommended**
- but keep it narrow at first; not all listed vocabulary candidates are equally clean yet

### Slice 2 — Movement state vocabulary only
Candidate members:
- `UMGMovementIntentKind`
- maybe `UMGMovementState`

Recommendation:
- **only `UMGMovementIntentKind` is clean now**
- `UMGMovementState` should not be treated as pure vocabulary without a more deliberate movement/state pass

### Slice 3 — StateAccess read-only subset
Recommendation:
- **not recommended yet**
- reason: even a reduced adaptation would still need a careful replacement for role/movement/engagement/travel state surfaces, and the current source form is too entangled

### Slice 4 — Full StateAccess
Recommendation:
- **no**
- it is not state-only and would violate the movement-authority boundary

### Slice 5 — Defer StateAccess until movement/router phase
Recommendation:
- **yes**
- this is the safest default for the file itself

## Explicit answers
- Can `AIGMCompanionStateAccess.cs` be ported before movement/router?
  - **no, not in its current form**
- Can its model dependencies be ported before movement/router?
  - **some of them, yes**
  - strongest candidates: `AIGMTrackingCyclePhase`, `AIGMExecutionMode`, `UMGMovementIntentKind`
- Is `UMGMovementState` pure state or does it imply router behavior?
  - **mixed state; it implies router/movement behavior context and should not be treated as pure vocabulary**
- Is `UMGMovementIntentKind` pure vocabulary or does it imply movement execution?
  - **pure vocabulary enum**
- Is `AIGMExecutionMode` safe to introduce as enum/model vocabulary now?
  - **yes**
- Would porting StateAccess violate the movement-authority boundary?
  - **yes**
- What is the smallest safe next mutation?
  - **a narrow model-vocabulary patch, not StateAccess itself**

## Whether a model-only patch is recommended
- **yes**
- but only for the cleanest vocabulary/state surfaces, not the whole missing set

## Whether a read-only StateAccess port is recommended
- **no, not yet**
- the coupling map is still too messy to claim a safe read-only cut without more design work

## Whether movement/router should come first
- **for StateAccess itself: yes**
- for a small vocabulary patch: not necessarily

## Recommended next phase
- **Recommended next phase: Phase 56Q-R3-STATE-MODEL — Port companion state model vocabulary only + build**

Suggested first safe candidates for that phase:
- `AIGMTrackingCyclePhase`
- `AIGMExecutionMode`
- `UMGMovementIntentKind`

Reason:
- these appear to be the cleanest vocabulary surfaces identified in this planning pass
- they advance the remaining state lane without importing movement authority, router execution, or travel-navigation behavior

## Confirmations
- confirmation no files were copied: confirmed
- confirmation no patch was applied: confirmed
- confirmation no command/action/movement files were changed: confirmed
- confirmation no commit occurred: confirmed
