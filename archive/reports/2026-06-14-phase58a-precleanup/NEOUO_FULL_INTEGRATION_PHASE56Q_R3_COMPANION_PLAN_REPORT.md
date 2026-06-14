# NEOUO FULL INTEGRATION PHASE56Q-R3-COMPANION-PLAN REPORT

Date: 2026-06-07 17:58:00 -09:00

## Target state
- branch: neo/staging-aigm
- HEAD: b43a5e80e2adcc0148055d57aa0245e14debf097
- latest commit: b43a5e80e feat: add AIGM companion intent model
- git status:
```text
?? NEOUO_FULL_INTEGRATION_AIGM_BASELINE_ABSTRACTIONS_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_AIGM_MISSING_TYPE_SURFACE_REPORT.md
?? NEOUO_FULL_INTEGRATION_COMPILE_GAP_INVENTORY_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_R_PATCH_FORMAT_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_S_PATCH_SCOPE_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_T_SPLIT_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56L_PREREQ_PATCH_DRY_RUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56O_R_BUILD_ERRORS.txt
?? NEOUO_FULL_INTEGRATION_PHASE56P_GREEN_BASELINE_AIGM_RECONCILIATION_COMMIT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_AIGM_CORE_COMMAND_RUNTIME_DRYRUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_COMPANION_PLAN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_D_LOCATE_AND_DRYRUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_I_P_COMPANION_INTENT_COMMIT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_MD_BUCKET_C_DEPENDENCY_CLASSIFICATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_M_MANUAL_BUCKET_C_FILE_PORT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_REG_BUCKET_C_PATCH_REGENERATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R_CORE_COMMAND_RUNTIME_SPLIT_REGENERATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md
```

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

Time Elapsed 00:00:02.72
```
- result: PASSED

## Parser file inspected
- file: `Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs`
- referenced companion classes:
  - `AIGMCompanionDakeyras`
  - `AIGMCompanionDanyal`
  - `AIGMCompanionDardalion`
- whether concrete class references are truly required:
  - **not fundamentally required by the mechanic**
  - current use is for name/alias routing in `IsClearlyAddressedToDifferentCompanion` and alias extraction logic
- whether references could be generalized:
  - yes
  - likely through shared companion identity/profile metadata rather than hardcoded class checks
- referenced AIGM types:
  - `AIGMCompanionIntent`
  - `AIGMCompanionIntentKind`
- referenced movement/router types:
  - none direct
- referenced action-executor types:
  - none direct
- referenced speech/memory/bridge types:
  - none direct
- referenced ServUO core types:
  - `BaseHire`
  - `Mobile`
  - `IPooledEnumerable`
  - `Point3D`
  - `Map`

## Skill executor file inspected
- file: `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs`
- referenced companion classes:
  - `AIGMCompanionDakeyras`
  - `AIGMCompanionDanyal`
  - `AIGMCompanionDardalion`
- whether concrete class references are truly required:
  - partially avoidable
  - current hard dependency is mainly for per-class property access (`NextSupportActionUtc`)
- whether references could be generalized:
  - yes, if cooldown / support-state surfaces move onto a shared companion interface/base/profile
- referenced AIGM types:
  - none beyond companion class references
- referenced movement/router types:
  - none direct
- referenced action-executor types:
  - none direct
- referenced speech/memory/bridge types:
  - none direct
- referenced ServUO core types:
  - `BaseHire`
  - `Mobile`
  - `Bandage`
  - `EnhancedBandage`
  - `BandageContext`
  - skills / backpack / item APIs

## Companion files inspected

### BaseHire
- file: `Scripts/Mobiles/NPCs/BaseHire.cs`
- namespace: `Server.Mobiles`
- base class: `BaseCreature`
- constructable constructors:
  - `BaseHire(AIType AI)`
  - `BaseHire()`
  - `BaseHire(Serial serial)`
- serialization/deserialization:
  - yes
  - standard hireling persistence
- referenced AIGM types:
  - none
- referenced movement/router types:
  - none
- referenced speech/bridge/memory types:
  - none
- referenced services:
  - none special
- referenced experimental or optional systems:
  - none observed
- compile-safe as standalone mobile surface:
  - yes
- requires BaseHire first:
  - already present in target
- requires action-executor first:
  - no
- requires movement/router first:
  - no
- requires speech/bridge/memory first:
  - no

### AIGMCompanionDakeyras
- namespace: `Server.Mobiles`
- base class: `BaseHire`
- constructable constructor:
  - yes
- serialization/deserialization:
  - yes
- referenced AIGM types:
  - `AIGMExecutionMode`
  - `AIGMCompanionIntent`
  - `AIGMCompanionIntentKind`
  - `AIGMCompanionIntentParser`
  - `AIGMCompanionDirectActionPolicy`
  - `AIGMCompanionActionPolicyResult`
  - `AIGMCompanionActionDecision`
  - `AIGMCompanionActionExecutor`
  - `AIGMCompanionTrackingCycle`
  - `AIGMCompanionTravelController`
  - `AIGMCompanionSpeechQueue`
  - `AIGMCompanionSpeechBus`
  - `AIGMCompanionDialogueEvent`
- referenced movement/router types:
  - indirect via `AIGMCompanionTravelController` and tracking/travel behaviors
- referenced speech/bridge/memory types:
  - speech queue / speech bus / dialogue event
- referenced services:
  - none runtime-loader-specific observed
- referenced experimental or optional systems:
  - none explicit, but companion speech/action stack is broad
- compile-safe as standalone mobile surface:
  - no
- requires BaseHire first:
  - yes, but BaseHire already exists in target
- requires action-executor first:
  - yes
- requires movement/router first:
  - likely yes or near-yes because travel/tracking pulse hooks are embedded
- requires speech/bridge/memory first:
  - yes for full behavior surface

### AIGMCompanionDanyal
- namespace: `Server.Mobiles`
- base class: `BaseHire`
- constructable constructor:
  - yes
- serialization/deserialization:
  - yes
- referenced AIGM types:
  - largely same pattern as Dakeyras
  - parser
  - direct action policy
  - action executor
  - speech bus / speech queue
  - tracking cycle
  - travel controller
  - execution mode
- referenced movement/router types:
  - indirect via travel/tracking hooks
- referenced speech/bridge/memory types:
  - speech queue / speech bus / dialogue event
- referenced services:
  - none runtime-loader-specific observed
- compile-safe as standalone mobile surface:
  - no
- requires BaseHire first:
  - yes, but already satisfied in target
- requires action-executor first:
  - yes
- requires movement/router first:
  - likely yes or near-yes
- requires speech/bridge/memory first:
  - yes

### AIGMCompanionDardalion
- namespace: `Server.Mobiles`
- base class: `BaseHire`
- constructable constructor:
  - yes
- serialization/deserialization:
  - yes
- referenced AIGM types:
  - same broad family as Dakeyras / Danyal
  - parser
  - direct action policy
  - action executor
  - speech bus / speech queue
  - tracking cycle
  - execution mode
- referenced movement/router types:
  - less travel coupling than Dakeyras/Danyal in `OnThink` (tracking pulse present; travel pulse absent in the inspected file)
- referenced speech/bridge/memory types:
  - speech queue / speech bus / dialogue event
- referenced services:
  - none runtime-loader-specific observed
- compile-safe as standalone mobile surface:
  - no
- requires BaseHire first:
  - yes, but already satisfied in target
- requires action-executor first:
  - yes
- requires movement/router first:
  - somewhat less than Dakeyras/Danyal, but still not cleanly independent
- requires speech/bridge/memory first:
  - yes

## Target existence table
| Symbol / file | Present in target | Present in preserved Dev | Notes |
|---|---|---|---|
| `BaseHire` | yes | yes | already exists in target at `Scripts/Mobiles/NPCs/BaseHire.cs` |
| `AIGMCompanionDakeyras` | no | yes | not present in target |
| `AIGMCompanionDanyal` | no | yes | not present in target |
| `AIGMCompanionDardalion` | no | yes | present in source; not present in target |
| `IAIGMActor` | yes | yes | target has generic actor abstraction already |
| `IAIGMCompanionActor` | no | no observed | no dedicated companion interface found |
| companion profile / registry abstraction | no | no observed | no reusable registry/profile surface found |

## Dependency table
| Candidate file | Symbol/type provided | Intended companion status | Required by parser | Required by skill executor | Present in target | Present in preserved Dev | Pulls movement/router | Pulls action-executor | Pulls speech/bridge/memory | Pulls service/runtime-loader | Serialization risk | Generalizable to shared interface/profile | Recommended disposition |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `Scripts/Mobiles/NPCs/BaseHire.cs` | `BaseHire` | shared base | yes | yes | yes | yes | no | no | no | no | low | yes | already present; do not re-port |
| `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs` | `AIGMCompanionDakeyras` | intended companion | yes | yes | no | yes | yes | yes | yes | no direct loader tie seen | medium | partially | broad; do not port before dependency plan |
| `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs` | `AIGMCompanionDanyal` | intended companion | yes | yes | no | yes | yes | yes | yes | no direct loader tie seen | medium | partially | broad; do not port before dependency plan |
| `Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs` | `AIGMCompanionDardalion` | intended third companion, not yet validated in target | yes | yes | no | yes | some | yes | yes | no direct loader tie seen | medium | partially | intended, but still broad; validate separately if concrete route chosen |
| target abstraction | `IAIGMActor` | shared actor interface | no direct current use | no direct current use | yes | yes | no | no | no | no | low | yes | useful anchor, but insufficient alone for companion-specific identity/cooldown needs |

## Classification
- **Dakeyras:** H / broad for current phase, with D/E/F characteristics
  - requires action executor
  - likely requires movement/travel hooks
  - requires speech/bridge surfaces
- **Danyal:** H / broad for current phase, with D/E/F characteristics
  - same class of dependency as Dakeyras
- **Dardalion:** C + H
  - intended companion, not yet validated in target
  - not unwanted
  - somewhat narrower travel coupling than Dakeyras/Danyal in the inspected file, but still depends on action/speech surfaces and is not cleanly standalone
- **BaseHire:** A
  - safe standalone mobile surface
  - already present in target

## Dardalion-specific determination
- whether Dardalion is present in source: **yes**
- whether Dardalion has extra dependencies versus Dakeyras / Danyal: **not obviously extra-broad**
- observed difference:
  - Dardalion appears to have slightly less embedded travel controller coupling in `OnThink` than Dakeyras / Danyal
  - but it still depends on parser, action policy, action executor, speech queue/bus, tracking cycle, execution mode, and companion dialogue surfaces
- conclusion:
  - Dardalion should be treated as **intended but not yet validated in target**, not rejected

## Architecture check
- would the current parser / skill executor need edits every time a new AI companion is added?
  - **yes, as currently written**
- is companion identity hardcoded by class name?
  - **yes**
  - parser explicitly checks `AIGMCompanionDakeyras`, `AIGMCompanionDanyal`, `AIGMCompanionDardalion`
  - skill executor explicitly casts to those classes for support cooldown state
- is there already a reusable companion profile / registry pattern?
  - **no clear pattern found**
- should the next safe model be?
  - **hybrid approach**
  - use existing `BaseHire` and `IAIGMActor` as anchors
  - introduce a companion-specific shared identity/state surface before widening concrete named companion imports if possible

## Can parser be ported before companions?
- **no** in current form
- reason: hardcoded named companion class checks

## Can skill executor be ported before companions?
- **no** in current form
- reason: hardcoded per-class cooldown/property access

## Shared abstraction recommendation
A shared companion surface is recommended before more companions are added.

Minimum missing idea:
- a companion identity/profile surface for alias/name routing
- a shared companion state surface for `GuardOwnerMode`, `NextSupportActionUtc`, and `ExecutionMode`

Likely future-safe forms:
- `IAIGMCompanionActor`
- or companion profile/registry metadata keyed by actor/mobile identity
- or a hybrid: concrete companions implement shared companion properties while parser resolves aliases through profile metadata instead of class-name checks

## Recommended next phase
- **Recommended next phase: Phase 56Q-R3-PARSER-PLAN — Parser Decoupling / Shared Companion Surface Plan**

Reason:
- `BaseHire` is already present in target, so a BaseHire-first port is unnecessary.
- Concrete companion files are broad and pull action/speech/tracking surfaces with them.
- The current blocker for parser/skill executor is not just “missing companion files”; it is the lack of a scalable shared companion identity/state surface.
- Decoupling parser/skill executor from hardcoded named companion classes is the cleaner path if future companions are expected.

## Confirmations
- confirmation no files were copied: confirmed
- confirmation no patch was applied: confirmed
- confirmation no command/action/movement files were changed: confirmed
- confirmation no commit occurred: confirmed
