# NEOUO FULL INTEGRATION PHASE56Q-R3-MD BUCKET C DEPENDENCY CLASSIFICATION REPORT

Date: 2026-06-07 17:26:44 -09:00

## Target state
- branch: neo/staging-aigm
- HEAD: 5d263c775c9933c4f595ba6e650fcb50dee1d45b
- latest commit: 5d263c775 chore: reconcile baseline AIGM model and support surfaces
- expected target head: 5d263c775c9933c4f595ba6e650fcb50dee1d45b
- git status:
`	ext
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
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_D_LOCATE_AND_DRYRUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_M_MANUAL_BUCKET_C_FILE_PORT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_REG_BUCKET_C_PATCH_REGENERATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R_CORE_COMMAND_RUNTIME_SPLIT_REGENERATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md
`
- known untracked report artifacts:
`	ext
NEOUO_FULL_INTEGRATION_AIGM_BASELINE_ABSTRACTIONS_PATCH_REPORT.md
NEOUO_FULL_INTEGRATION_AIGM_MISSING_TYPE_SURFACE_REPORT.md
NEOUO_FULL_INTEGRATION_COMPILE_GAP_INVENTORY_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56K_V_R_PATCH_FORMAT_REPAIR_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56K_V_S_PATCH_SCOPE_REPAIR_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56K_V_T_SPLIT_PATCH_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56L_PREREQ_PATCH_DRY_RUN_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56P_GREEN_BASELINE_AIGM_RECONCILIATION_COMMIT_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_AIGM_CORE_COMMAND_RUNTIME_DRYRUN_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_D_LOCATE_AND_DRYRUN_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_M_MANUAL_BUCKET_C_FILE_PORT_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_REG_BUCKET_C_PATCH_REGENERATION_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R_CORE_COMMAND_RUNTIME_SPLIT_REGENERATION_REPORT.md
NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md
`

## Baseline build result
`	ext
  Determining projects to restore...
  All projects are up-to-date for restore.
  Ultima -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Ultima.dll
  Server -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\ServUO.exe
  Scripts -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.06
`
- result: PASSED

## Inspected Bucket C files
- Scripts/Custom/AIGM/AIGMCompanionIntent.cs
- Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs
- Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs
- Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs

## Dependency category legend
- A = Already present in target
- B = Safe standalone model/support type
- C = Deferred movement/router dependency
- D = Deferred companion mobile dependency
- E = Deferred command/action-executor dependency
- F = Runtime-loader/UMG integration dependency
- G = Unknown or risky dependency

## Dependency table
| Source file | Referenced symbol | Present in target | Present in preserved Dev | Dependency category A-G | Required for compile if source file is copied | Recommended disposition |
|---|---|---|---|---|---|---|
| Scripts/Custom/AIGM/AIGMCompanionIntent.cs | Point3D | yes | yes | A | yes | safe core type already present |
| Scripts/Custom/AIGM/AIGMCompanionIntent.cs | Map | yes | yes | A | yes | safe core type already present |
| Scripts/Custom/AIGM/AIGMCompanionIntent.cs | AIGMCompanionIntentKind | no | yes | B | yes | defined in same file; safe with intent-only port |
| Scripts/Custom/AIGM/AIGMCompanionIntent.cs | AIGMCompanionIntent | no | yes | B | yes | defined in same file; safe with intent-only port |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | BaseHire | yes | yes | A | yes | ServUO/base target type already present |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | Mobile | yes | yes | A | yes | ServUO/base target type already present |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | AIGMCompanionIntent | no | yes | B | yes | safe if intent-only file ports first |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | AIGMCompanionIntentKind | no | yes | B | yes | safe if intent-only file ports first |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | AIGMCompanionDakeyras | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | AIGMCompanionDanyal | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | AIGMCompanionDardalion | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs | IPooledEnumerable | yes | yes | A | yes | ServUO/core type already present |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | BaseHire | yes | yes | A | yes | ServUO/base target type already present |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | Mobile | yes | yes | A | yes | ServUO/base target type already present |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | Bandage | yes | yes | A | yes | ServUO/item type already present |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | EnhancedBandage | yes | yes | A | yes | ServUO/item type already present |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | BandageContext | yes | yes | A | yes | ServUO/support type already present |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | AIGMCompanionDakeyras | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | AIGMCompanionDanyal | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs | AIGMCompanionDardalion | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | BaseHire | yes | yes | A | yes | ServUO/base target type already present |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMCompanionTravelObjective | no | yes | B | yes | possible model-only support candidate; inspect separately before port |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMTrackingCyclePhase | no | yes | B | yes | possible model-only support candidate; inspect separately before port |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | UMGMovementState | no | yes | C | yes | defer until movement/router phase |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMCompanionEngagementState | no | yes | B | yes | possible model-only support candidate; inspect separately before port |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMCompanionRoleProfile | no | yes | B | yes | possible model-only support candidate; inspect separately before port |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | UMGMovementIntentKind | no | yes | C | yes | defer until movement/router phase |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMExecutionMode | no | yes | B | yes | possible model-only support candidate; inspect separately before port |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMExecutionLog | yes | yes | A | yes | already present in target |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMCompanionDakeyras | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMCompanionDanyal | no | yes | D | yes | defer until companion mobile strategy is ported |
| Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs | AIGMCompanionDardalion | no | yes | D | yes | defer until companion mobile strategy is ported |

## Classification of each missing symbol
- AIGMCompanionIntent => category B
- AIGMCompanionIntentKind => category B
- AIGMCompanionTravelObjective => category B
- AIGMTrackingCyclePhase => category B
- UMGMovementState => category C
- AIGMCompanionEngagementState => category B
- AIGMCompanionRoleProfile => category B
- UMGMovementIntentKind => category C
- AIGMExecutionMode => category B
- AIGMCompanionDakeyras => category D
- AIGMCompanionDanyal => category D
- AIGMCompanionDardalion => category D

## Slice evaluation
### Slice 1 — AIGMCompanionIntent.cs only
- safe alone: yes
- reason: only depends on ServUO core types (Point3D, Map) and its own in-file intent kind vocabulary.

### Slice 2 — AIGMCompanionIntent.cs plus safe standalone enum/model dependencies only
- viable: yes, but not required yet
- reason: current intent file already contains its own vocabulary and does not require the missing Bucket C state/movement/companion surfaces.

### Slice 3 — Parser only, without companion mobile coupling
- viable: no
- reason: parser directly references AIGMCompanionDakeyras, AIGMCompanionDanyal, and AIGMCompanionDardalion.

### Slice 4 — StateAccess only
- viable: no
- reason: AIGMCompanionStateAccess.cs must remain deferred because it directly references movement-layer types UMGMovementState and UMGMovementIntentKind, plus multiple missing state/role/execution symbols.

### Slice 5 — SkillExecutor only
- viable: no
- reason: skill executor requires concrete companion mobile classes first.

## Key determinations
- whether AIGMCompanionIntent.cs is safe alone: yes
- whether AIGMCompanionStateAccess.cs must remain deferred: yes
- whether parser / skill executor require companion mobile port first: yes

## Sequencing recommendation
- Recommended option: **Option E**
- Split Bucket C into:
  - R3-SAFE: intent/model-only
  - R3-COMPANION: parser/skill executor after companion mobile surfaces exist
  - R3-MOVEMENT: state access after movement/router phase
- Immediate next phase: **Phase 56Q-R3-I — Port AIGMCompanionIntent Only + Build**

Secondary planning note:
- A later model-only support phase may be reasonable for AIGMCompanionTravelObjective, AIGMTrackingCyclePhase, AIGMCompanionEngagementState, AIGMCompanionRoleProfile, and AIGMExecutionMode, but only after those files are individually inspected to confirm they are vocabulary/state-only and do not themselves pull movement behavior or concrete companion logic.

## Confirmations
- confirmation no files were copied: confirmed
- confirmation no patch was applied: confirmed
- confirmation no command/action/movement files were changed: confirmed
- confirmation no commit occurred: confirmed
