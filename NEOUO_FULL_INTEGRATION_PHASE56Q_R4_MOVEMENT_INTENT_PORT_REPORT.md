# NEOUO FULL INTEGRATION PHASE56Q-R4 MOVEMENT-INTENT PORT REPORT

Date: 2026-06-08 08:25 -09:00

## Target state
- branch: `neo/staging-aigm`
- HEAD: `0955a665b9d63882a945bc4e219c9f5523162a15`
- latest commit: `0955a665 feat: add AIGM companion state vocabulary`

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

## Source UMGMovementIntent inspected
Source file:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\Movement\UMGMovementIntent.cs`

Namespace:
- `Server.Custom.AIGM`

Top-level type(s) found in source file:
- `UMGMovementIntentKind` enum
- `UMGMovementIntent` sealed class

Important adaptation note:
- `UMGMovementIntentKind` was already ported in an earlier phase
- this phase copied only the `UMGMovementIntent` DTO class
- the enum was **not** recopied

Constructors:
- implicit default constructor only

Properties / fields:
- `Kind`
- `Requester`
- `TargetMobile`
- `DestinationName`
- `DestinationPoint`
- `DestinationMap`
- `TargetSerial`
- `TargetName`
- `Reason`

Methods:
- `Create(UMGMovementIntentKind kind)` static factory helper

Referenced AIGM types:
- `UMGMovementIntentKind`

Referenced ServUO types:
- `Mobile`
- `Point3D`
- `Map`

Dependency inspection summary:
- references `UMGMovementRouter`: **no**
- references `UMGMovementState`: **no**
- references `AIGMCompanionStateAccess`: **no**
- mutates world state: **no**
- moves mobiles: **no**
- touches `Combatant`: **no**
- touches `ControlTarget`: **no**
- touches `ControlOrder`: **no**
- touches `Home`: **no**
- touches `RangeHome`: **no**
- touches `CantWalk`: **no**

## DTO / model-only confirmation
- `UMGMovementIntent` remains a passive DTO/model surface: **confirmed**
- no movement execution was ported: **confirmed**
- no travel/tracking/control authority was ported: **confirmed**

## Target file existence / tracked status before port
Target file:
- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\Movement\UMGMovementIntent.cs`

Before port:
- exists: **no**
- tracked: **no**
- safe to add: **yes**

## Copied / adapted file
Created:
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`

Adaptation applied:
- copied only the sealed DTO class
- omitted the enum already present in target (`UMGMovementIntentKind`)

## Scope verification
Expected changed file:
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`

Forbidden file checks:
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`: unchanged
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`: unchanged
- `Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs`: unchanged
- `Scripts/Custom/AIGM/AIGMCompanionTravelObjective.cs`: unchanged
- `Scripts/Custom/AIGM/AIGMCompanionEngagementState.cs`: unchanged
- `Scripts/Custom/AIGM/AIGMCompanionRoleProfile.cs`: unchanged
- parser files: unchanged
- skill executor files: unchanged
- companion mobile files: unchanged
- command/action files: unchanged
- bridge/speech/gump warning files: unchanged

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
Warning count after port:
- **15**

Warning files observed:
- `Scripts\Gumps\AIGMResponseGump.cs`
- `Scripts\Gumps\AIGMQuestionGump.cs`
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs`
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs`

Important warning note:
- warnings do **not** touch `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`
- warnings match the previously known unrelated non-movement warning set
- no warning repair was attempted in this phase

## Error classification
- errors after port: **none**
- no namespace/path mismatch observed
- no missing approved vocabulary dependency observed
- no accidental `UMGMovementState` dependency observed
- no accidental router dependency observed
- no accidental `StateAccess` dependency observed

## Confirmations
- confirmation `UMGMovementIntent` is DTO/model-only: **confirmed**
- confirmation no movement authority was ported: **confirmed**
- confirmation router was not copied: **confirmed**
- confirmation `StateAccess` was not copied: **confirmed**
- confirmation `UMGMovementState` was not copied: **confirmed**
- confirmation command/action/parser/skill/companion mobile files were not changed: **confirmed**

## Recommendation for next phase
Recommended next phase:
- **Phase 56Q-R4-MOVEMENT-INTENT-P — Commit UMGMovementIntent DTO**

Suggested commit message:
- `feat: add UMG movement intent model`

Reason:
- the DTO slice ports cleanly
- build remains error-free
- warnings are known unrelated files, not the new movement intent model
- movement authority remains deferred behind future router-boundary work
