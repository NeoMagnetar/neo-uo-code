# NEOUO FULL INTEGRATION PHASE56Q-R3-SKILL-PLAN REPORT

Date: 2026-06-07 18:51:38 -09:00

## Target state
- branch: neo/staging-aigm
- HEAD: 3ddb64b6c03c65d007ebfacd027fc1d4c6c344bc
- latest commit: 3ddb64b6c feat: add shared companion intent parser
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
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_COMPANION_PLAN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_D_LOCATE_AND_DRYRUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_I_P_COMPANION_INTENT_COMMIT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_MD_BUCKET_C_DEPENDENCY_CLASSIFICATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_M_MANUAL_BUCKET_C_FILE_PORT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_DECOUPLE_P_COMMIT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_DECOUPLING_PLAN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_SURFACE_P_COMMIT_REPORT.md
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
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_COMPANION_PLAN_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_D_LOCATE_AND_DRYRUN_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_I_P_COMPANION_INTENT_COMMIT_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_MD_BUCKET_C_DEPENDENCY_CLASSIFICATION_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_M_MANUAL_BUCKET_C_FILE_PORT_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_DECOUPLE_P_COMMIT_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_DECOUPLING_PLAN_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_SURFACE_P_COMMIT_REPORT.md
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

Time Elapsed 00:00:03.10
`
- result: PASSED

## Skill executor file inspected
- file: C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionSkillExecutor.cs

### Public methods
- TryHealTarget
- TryCureTarget
- TryUseHealingSkill
- TryUseBandages
- TryUseMageryHeal
- TryUseCurePotion
- TryUseMageryCure
- CanUseBandages

### Private methods
- FindBandages
- TryStartNativeSelfBandage
- IsUnderAttack
- GetNextSupportActionUtc
- SetNextSupportActionUtc
- Log
- SafeName

### Cooldown / state behavior summary
- no executor-local fields or dictionaries
- cooldown state is stored externally on companion mobile instances via NextSupportActionUtc
- cooldown reads/writes occur through:
  - GetNextSupportActionUtc(BaseHire companion)
  - SetNextSupportActionUtc(BaseHire companion, DateTime value)
- current implementation obtains state by concrete casts to:
  - AIGMCompanionDakeyras
  - AIGMCompanionDanyal
  - AIGMCompanionDardalion
- this is the primary concrete coupling in the file

### World mutation / skill-use behavior summary
- world mutation behavior exists:
  - target hit-point mutation
  - bandage consumption
  - cooldown updates
- skill-use behavior exists:
  - healing via bandages
  - simplified healing/cure spell effects
  - cure potion self-use behavior
- state persistence behavior:
  - indirect only, through companion runtime state properties
  - no file/database persistence inside executor itself

### Referenced AIGM / system surfaces
- referenced AIGM types:
  - none beyond named companion concrete classes
- referenced movement/router types:
  - none direct
- referenced action-executor types:
  - none direct
- referenced speech/memory/bridge/dialogue types:
  - none direct
- referenced tracking/travel types:
  - none direct
- referenced ServUO core types:
  - BaseHire
  - Mobile
  - Bandage
  - EnhancedBandage
  - BandageContext
  - skill values / backpack / range / hits / combatant APIs

## Concrete companion coupling table
| Source member/method | Hardcoded companion reference | Current purpose | Accessed member/state | Can replace with IAIGMCompanionActor | Can replace with AIGMCompanionProfile | Needs new skill state surface | Needs action executor | Needs movement/router | Risk level | Recommended disposition |
|---|---|---|---|---|---|---|---|---|---|---|
| GetNextSupportActionUtc | AIGMCompanionDakeyras | cooldown read | NextSupportActionUtc | yes | no | maybe | no | no | medium | remove concrete cast; route through shared skill state / actor state |
| GetNextSupportActionUtc | AIGMCompanionDanyal | cooldown read | NextSupportActionUtc | yes | no | maybe | no | no | medium | remove concrete cast; route through shared skill state / actor state |
| GetNextSupportActionUtc | AIGMCompanionDardalion | cooldown read | NextSupportActionUtc | yes | no | maybe | no | no | medium | remove concrete cast; route through shared skill state / actor state |
| SetNextSupportActionUtc | AIGMCompanionDakeyras | cooldown write | NextSupportActionUtc | yes | no | maybe | no | no | medium | remove concrete cast; route through shared skill state / actor state |
| SetNextSupportActionUtc | AIGMCompanionDanyal | cooldown write | NextSupportActionUtc | yes | no | maybe | no | no | medium | remove concrete cast; route through shared skill state / actor state |
| SetNextSupportActionUtc | AIGMCompanionDardalion | cooldown write | NextSupportActionUtc | yes | no | maybe | no | no | medium | remove concrete cast; route through shared skill state / actor state |
| TryUseBandages / TryUseMageryHeal / TryUseCurePotion / TryUseMageryCure | indirect via cooldown helpers | runtime support gating | cooldown checks | yes | partial | yes | no | no | medium | preserve behavior, swap state backing surface |

## Shared surfaces inspected
### IAIGMCompanionActor
- currently supports:
  - companion identity
  - profile key / role strings
  - CanUseAIGMSkills
  - GuardOwnerMode
  - NextSupportActionUtc
  - ExecutionModeKey
- executor support status:
  - already covers the current direct cooldown need via NextSupportActionUtc
- missing cooldown fields:
  - none for the current executor slice
- missing skill capability fields:
  - capability granularity is minimal; only CanUseAIGMSkills exists
- missing state access fields:
  - none required for the current cooldown use case
- conclusion:
  - enough for a first decoupling pass if the executor can cast to IAIGMCompanionActor

### AIGMCompanionProfile
- currently supports:
  - identity / aliases / role / allowed intent kinds / allowed skill families
- executor support status:
  - useful for future capability/routing policy
  - insufficient for runtime cooldown tracking by itself
- conclusion:
  - good for eligibility/profile-driven evolution, not enough for current cooldown state alone

### AIGMCompanionIntent
- currently supports intent kind / target / destination / address flags
- executor support status:
  - not a cooldown surface
- conclusion:
  - not directly relevant for executor state decoupling

### IAIGMActor
- currently supports generic actor identity / shell / inventory
- executor support status:
  - too generic for cooldown state by itself
- conclusion:
  - useful base anchor, not enough for direct skill executor decoupling alone

## Action / movement / speech / travel dependency summary
- action executor dependency: no direct dependency in the inspected skill executor
- movement/router dependency: no direct dependency in the inspected skill executor
- speech/memory/bridge/dialogue dependency: no direct dependency in the inspected skill executor
- travel/tracking dependency: no direct dependency in the inspected skill executor
- conclusion:
  - this lane is materially cleaner than parser/state-access/companion-mobile lanes

## Key answers
- can skill executor be ported before concrete companion mobiles?
  - **yes**, if it targets shared companion cooldown/state surface instead of concrete classes
- can skill executor be ported before movement/router?
  - **yes**
- can skill executor be ported before action executor?
  - **yes**
- should cooldown state live on companion actor, separate skill state interface, or executor-local dictionary?
  - **preferred now: on shared companion actor surface**
  - reason: IAIGMCompanionActor already has NextSupportActionUtc, so adding another interface just for the current one-field need is unnecessary churn
  - future expansion to per-skill cooldown maps may justify a later dedicated skill-state surface
- should Dakeyras / Danyal / Dardalion skill behavior be data/profile-driven?
  - **eligibility yes, runtime cooldown no**
  - profile data should govern allowed skill families later
  - runtime cooldown should remain runtime actor state, not static profile data
- what is the smallest safe next mutation?
  - **port skill executor against IAIGMCompanionActor cooldown/state access without broadening scope**

## Whether new skill state/capability surface is recommended
- immediate recommendation: **not required for the first decoupling pass**
- rationale:
  - current IAIGMCompanionActor already exposes the only runtime field the preserved executor actually uses: NextSupportActionUtc
  - current AIGMCompanionProfile can cover later capability-family policy if needed
- future recommendation:
  - if the executor later grows per-skill cooldown maps, skill-family gating, or role-specific execution routing, then a dedicated IAIGMCompanionSkillState or AIGMCompanionSkillProfile may become worthwhile

## Recommended next mutation phase
- **Recommended next phase: Phase 56Q-R3-SKILL-DECOUPLE — Port skill executor against existing shared companion surface + build**

Reason:
- no direct action/movement/travel dependency blocks this lane
- concrete companion coupling is narrow and mostly artificial
- the required runtime state hook already exists on IAIGMCompanionActor
- this is a cleaner near-term mutation than creating more abstraction files before they are truly needed

## Confirmations
- confirmation no files were copied: confirmed
- confirmation no patch was applied: confirmed
- confirmation no command/action/movement files were changed: confirmed
- confirmation no commit occurred: confirmed
