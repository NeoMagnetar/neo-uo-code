# NEOUO FULL INTEGRATION PHASE56Q-R4 MOVEMENT-STATE PLAN REPORT

Date: 2026-06-08 08:39 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `b003826012bc47d1bb66540bb72a35fde50e5dd2`
- latest commit: `b003826 feat: add UMG movement intent model`

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
- known prior warning set did not reproduce in this run
- no warning cleanup was attempted

## UMGMovementState file inspected
Source file inspected:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementState.cs`

Target equivalent:
- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\Movement\UMGMovementState.cs`
- present in target now: **no**

### Namespace
- `Server.Custom.AIGM`

### Top-level types
- `UMGMovementState`

### Constructors
- public default constructor:
  - initializes `ActiveIntent = Idle`
  - initializes `SuspendedIntent = Idle`
  - initializes `UpdatedUtc = DateTime.UtcNow`
  - initializes `DestinationPoint = Point3D.Zero`
  - initializes `TrackingMode = "Idle"`
  - initializes `RoleProfile = AIGMCompanionRoleProfile.Unknown`

### Fields / properties
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

### Public methods
- `Apply(UMGMovementIntent intent)`

### Private methods
- none

### Referenced AIGM types
- `UMGMovementIntentKind`
- `UMGMovementIntent`
- `AIGMCompanionRoleProfile`

### Referenced movement types
- `UMGMovementIntentKind`
- `UMGMovementIntent`

### Referenced ServUO types
- `Point3D`
- `Map`

### Direct dependency / safety findings
- references `UMGMovementRouter`: **no**
- references `AIGMCompanionStateAccess`: **no**
- references `Mobile` directly: **no**
- references `ControlOrder`: **no**
- references `Combatant`: **no**
- references `ControlTarget`: **no**
- references `CantWalk`: **no**
- references `Home`: **no**
- references `RangeHome`: **no**
- owns timers: **no**
- owns dictionaries: **no**
- mutates world state: **no**
- moves mobiles: **no**
- calls pathfinding: **no**
- starts / stops / suspends / resumes movement intent directly: **no**
  - note: it stores active/suspended intent values, but does not itself command or resume movement behavior

## Target dependency search result
Search requested:
- `UMGMovementState|UMGMovementIntent|UMGMovementIntentKind|AIGMExecutionMode|AIGMTrackingCyclePhase`

Practical result for current planning decision:
- already present in target:
  - `UMGMovementIntentKind`
  - `UMGMovementIntent`
  - `AIGMExecutionMode`
  - `AIGMTrackingCyclePhase`
- not present in target:
  - `UMGMovementState`
  - `AIGMCompanionRoleProfile`

## Dependency table

| Referenced symbol | Present in target | Present in preserved Dev | Category | Required for UMGMovementState compile | Safe before router | Recommended disposition |
|---|---|---|---|---|---|---|
| `UMGMovementIntentKind` | yes | yes | A. already present | yes | yes | already satisfied |
| `UMGMovementIntent` | yes | yes | A. already present | yes | yes | already satisfied |
| `AIGMCompanionRoleProfile` | no | yes | B. safe vocabulary/model | yes | yes | port separately or adapt away in bounded state phase |
| `Point3D` | yes | yes | A. already present | yes | yes | already satisfied |
| `Map` | yes | yes | A. already present | yes | yes | already satisfied |
| `DateTime` | yes | yes | A. already present | yes | yes | already satisfied |
| `UMGMovementRouter` | no | yes | D. movement/router authority | no | no | not needed for state-model compile |
| `AIGMCompanionStateAccess` | no | yes | E. StateAccess dependency | no | no | not needed for state-model compile |
| `Mobile` | yes | yes | F. ServUO live-control dependency | no | n/a | not referenced by UMGMovementState |

## Classification of UMGMovementState
UMGMovementState best fits:
- **B. Safe only after minor adaptation**

Why not A / safe as-is?
- The file itself is passive and clean, but it depends on `AIGMCompanionRoleProfile`, which is not yet present in target.
- That dependency is likely safe, but it means an as-is port of only `UMGMovementState.cs` would not compile unless the role enum is also added.

Why not C / too broad before router?
- It does **not** call router logic
- It does **not** call StateAccess
- It does **not** mutate live ServUO control state
- It does **not** move mobiles

Why not D / unsafe unknown?
- The dependency graph is clear enough in this pass
- The only missing dependency is the passive enum `AIGMCompanionRoleProfile`

## Reduced bounded model evaluation
### Can a reduced UMGMovementState preserve only active/suspended intent and metadata?
- **yes**

### Can it exclude router hooks?
- **yes**
- there are no router hooks in the file now

### Can it exclude live Mobile references?
- **yes**
- it already has no direct `Mobile` dependency

### Can it exclude travel/tracking dictionaries?
- **yes**
- it owns no dictionaries

### Can it compile using already-ported UMGMovementIntent and UMGMovementIntentKind?
- **almost**
- blocker: `AIGMCompanionRoleProfile` is still missing in target

### Reduced-model recommendation
A reduced bounded model port is safer than an as-is multi-file import if the goal is to avoid broadening scope. Two clean options exist:
1. add a reduced `UMGMovementState` that removes `RoleProfile` for now, or
2. do a tightly bounded two-file passive model slice: `UMGMovementState` + `AIGMCompanionRoleProfile`

Given the current instruction focuses on planning only, the report favors the reduced bounded model option as the safer next mutation.

## Explicit answers

### Can UMGMovementState be ported before UMGMovementRouter?
- **yes, conditionally**
- only if treated as a passive state model and not bundled with router behavior

### Can UMGMovementState be ported before AIGMCompanionStateAccess?
- **yes, conditionally**
- it does not depend on StateAccess directly

### Does UMGMovementState mutate world/control state?
- **no**

### Does UMGMovementState depend on live Mobile references?
- **no**

### Does UMGMovementState own active movement authority?
- **no**
- it stores intent/state description only

### Is as-is port safe?
- **not as a single-file port right now**
- reason: missing `AIGMCompanionRoleProfile` dependency in target

### Is a reduced bounded model port safer?
- **yes**
- either remove the role-profile field temporarily or plan a very small paired passive-model import

### What is the smallest safe next mutation?
- **Phase 56Q-R4-MOVEMENT-STATE-REDUCED — Add bounded UMGMovementState model only + build**
- recommended shape:
  - port a reduced passive `UMGMovementState` that keeps intent + metadata fields
  - omit or defer `RoleProfile` until explicitly approved

## Recommended next phase
- **Phase 56Q-R4-MOVEMENT-STATE-REDUCED — Add bounded UMGMovementState model only + build**

Reason:
- UMGMovementState is passive enough for model slicing
- as-is compile would require `AIGMCompanionRoleProfile`
- the reduced model approach preserves the movement boundary while minimizing scope creep
- router authority and StateAccess remain deferred

## Confirmations
- confirmation no files were copied: **confirmed**
- confirmation no patch was applied: **confirmed**
- confirmation no command/action/parser/skill/companion mobile files were changed: **confirmed**
- confirmation no commit occurred: **confirmed**

## Final boundary statement
`UMGMovementState` may describe movement state, but it must not be allowed to smuggle in authority through adjacent dependencies. Keep it passive, keep router deferred, and treat missing role-profile support as a separate bounded decision rather than a reason to broaden into movement control.
