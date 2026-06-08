# NEOUO FULL INTEGRATION PHASE56Q-R4 MOVEMENT-STATE-REDUCED REPORT

Date: 2026-06-08 08:41 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `b003826012bc47d1bb66540bb72a35fde50e5dd2`
- latest commit: `b003826 feat: add UMG movement intent model`

## Baseline build result before port
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

## Source UMGMovementState inspected
Source file:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementState.cs`

Source characteristics confirmed:
- namespace: `Server.Custom.AIGM`
- stores active/suspended intent
- stores destination/target metadata
- stores tracking/movement-decision metadata
- uses `Apply(UMGMovementIntent intent)` as data-copy only
- does not call router
- does not call `StateAccess`
- does not reference `Mobile`
- does not mutate control/combat fields
- does not move mobiles
- blocker in source form: depends on `AIGMCompanionRoleProfile`

## Reduced adaptation strategy
Strategy used:
- create a bounded passive `UMGMovementState` model in target
- preserve the passive state/data shape
- avoid importing `AIGMCompanionRoleProfile`
- replace the enum dependency with a passive string key:
  - `RoleProfileKey`
- add data-reset helpers only:
  - `Reset()`
  - `Clear()`

## Whether AIGMCompanionRoleProfile dependency was removed / avoided
- **yes**
- `AIGMCompanionRoleProfile` was not imported
- no fake enum or stub was introduced
- no role-profile file was copied

## Exact files changed
Created:
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`

No other code files were changed.

## Bounded passive model confirmation
The reduced `UMGMovementState` in target:
- stores passive movement/state data only
- does not call router
- does not call `StateAccess`
- does not reference `Mobile`
- does not own timers
- does not own dictionaries
- does not move mobiles
- does not call pathfinding
- does not mutate `Combatant`
- does not mutate `ControlTarget`
- does not mutate `ControlOrder`
- does not mutate `CantWalk`
- does not mutate `Home`
- does not mutate `RangeHome`

## Forbidden file confirmation
Unchanged:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- `Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTravelObjective.cs`
- `Scripts/Custom/AIGM/AIGMCompanionEngagementState.cs`
- `Scripts/Custom/AIGM/AIGMCompanionRoleProfile.cs`
- parser files
- skill executor files
- companion mobile files
- command/action files
- bridge/speech/gump files

## Build result after port
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
Warning count after reduced model port:
- **15**

Warning files observed:
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
- `Scripts\Gumps\AIGMResponseGump.cs`
- `Scripts\Gumps\AIGMQuestionGump.cs`
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

Important warning note:
- warnings do **not** touch `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- warnings match the known unrelated warning set
- no warning repair was attempted in this phase

## Error classification
- errors after reduced model port: **none**
- no reduced model implementation error observed
- no namespace/path mismatch observed
- no missing already-approved vocabulary dependency observed
- no accidental `AIGMCompanionRoleProfile` dependency observed
- no accidental router dependency observed
- no accidental `StateAccess` dependency observed

## Confirmations
- confirmation `UMGMovementState` is bounded passive state/model only: **confirmed**
- confirmation router was not copied: **confirmed**
- confirmation `StateAccess` was not copied: **confirmed**
- confirmation role profile was not copied: **confirmed**
- confirmation command/action/parser/skill/companion mobile files were not changed: **confirmed**

## Recommendation for next phase
Recommended next phase:
- **Phase 56Q-R4-MOVEMENT-STATE-REDUCED-P — Commit Bounded UMGMovementState Model**

Suggested commit message:
- `feat: add bounded UMG movement state model`

Reason:
- reduced passive state model compiles cleanly on errors
- no movement authority was imported
- role-profile dependency was avoided safely
- warnings remain in the known unrelated files only
