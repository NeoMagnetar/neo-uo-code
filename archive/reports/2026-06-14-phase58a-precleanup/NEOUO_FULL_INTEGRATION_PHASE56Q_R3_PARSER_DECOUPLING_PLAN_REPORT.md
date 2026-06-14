# NEOUO FULL INTEGRATION PHASE56Q-R3-PARSER-DECOUPLING PLAN REPORT

Date: 2026-06-07 18:08:42 -09:00

## Target state
- branch: neo/staging-aigm
- HEAD: b43a5e80e2adcc0148055d57aa0245e14debf097
- latest commit: b43a5e80e feat: add AIGM companion intent model
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

Time Elapsed 00:00:02.81
`
- result: PASSED

## Parser file inspected
- file: C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionIntentParser.cs
- hardcoded companion references found:
  - AIGMCompanionDakeyras
  - AIGMCompanionDanyal
  - AIGMCompanionDardalion
- current reasons for those references:
  1. identify whether speech is addressed to a different named companion
  2. attach alias sets (dak, waylander, dar)
  3. derive addressed-command stripping rules from concrete class type
- whether used only for identity/name matching:
  - mostly yes
- whether used for type-specific behavior:
  - not really; parser is doing identity routing, not per-class gameplay execution
- whether it could be replaced by BaseHire:
  - partially, but BaseHire alone does not provide companion identity / alias metadata
- whether it could be replaced by IAIGMActor:
  - not by current shape alone; IAIGMActor lacks companion alias/profile fields
- whether it needs a new companion-specific interface/profile:
  - yes

## Skill executor file inspected
- file: C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionSkillExecutor.cs
- hardcoded companion references found:
  - AIGMCompanionDakeyras
  - AIGMCompanionDanyal
  - AIGMCompanionDardalion
- current reasons for those references:
  1. read NextSupportActionUtc
  2. write NextSupportActionUtc
- skill behavior attached to that class:
  - support cooldown storage only
- whether behavior is actually companion-specific:
  - the cooldown state is companion-specific, but not Dak/Danyal/Dardalion-specific
- whether behavior could be profile-driven:
  - yes for role / allowed skill families
  - cooldown state itself should be on a shared companion surface, not a profile row alone
- whether behavior could be routed through IAIGMActor:
  - not with current interface shape
- whether behavior requires concrete mobile methods/properties:
  - only because the current code stores support cooldown on concrete companion types
  - that dependency is artificial, not essential to the skill behavior itself

## BaseHire inspection summary
- target file inspected: Scripts\Mobiles\NPCs\BaseHire.cs
- current role:
  - shared hireling mobile base
- identity fields present:
  - generic mobile Name, owner/control state via ServUO surfaces
- capability fields present:
  - none AIGM-specific
- actor references present:
  - none AIGM-specific
- can represent Dakeyras / Danyal / Dardalion / future companions?
  - only as generic mobiles, not as named AI companion identities with aliases/profiles/cooldowns
- runtime-safe:
  - yes
- avoids movement/action side effects:
  - yes
- conclusion:
  - useful base, but too generic to be the parser/skill decoupling target by itself

## IAIGMActor inspection summary
- target file inspected: Scripts\Custom\AIGM\IAIGMActor.cs
- current fields:
  - Mobile Shell
  - string ActorId
  - string DisplayName
  - IAIGMInventoryCapability Inventory
- can represent Dakeyras / Danyal / Dardalion / future companions?
  - partially
  - enough for general actor identity, not enough for companion alias routing or support-state storage
- runtime-safe:
  - yes
- avoids movement/action side effects:
  - yes
- conclusion:
  - good anchor, but incomplete for companion-specific parser/skill needs

## AIGMCompanionIntent inspection summary
- target file inspected: Scripts\Custom\AIGM\AIGMCompanionIntent.cs
- current fields:
  - Kind
  - TargetSerial
  - RawText
  - DestinationName
  - DestinationPoint
  - DestinationMap
  - AllowRemoteRelay
  - ExplicitlyAddressed
  - AddressedToDifferentCompanion
- current identity limitation:
  - no normalized companion target id / profile key field exists yet
- runtime-safe:
  - yes
- avoids movement/action side effects:
  - yes
- conclusion:
  - already safe, but not yet expressive enough for profile-based companion addressing

## Hardcoded companion reference table
| File | Hardcoded symbol | Current purpose | Used only for identity/name matching | Used for type-specific behavior | Could replace with BaseHire | Could replace with IAIGMActor | Needs new interface/profile |
|---|---|---|---|---|---|---|---|
| AIGMCompanionIntentParser.cs | AIGMCompanionDakeyras | alias routing / addressed-to-different-companion checks | yes | no | no | no | yes |
| AIGMCompanionIntentParser.cs | AIGMCompanionDanyal | alias routing / addressed-to-different-companion checks | yes | no | no | no | yes |
| AIGMCompanionIntentParser.cs | AIGMCompanionDardalion | alias routing / addressed-to-different-companion checks | yes | no | no | no | yes |
| AIGMCompanionSkillExecutor.cs | AIGMCompanionDakeyras | support cooldown storage access | no | yes, but only accidentally | no | no | yes |
| AIGMCompanionSkillExecutor.cs | AIGMCompanionDanyal | support cooldown storage access | no | yes, but only accidentally | no | no | yes |
| AIGMCompanionSkillExecutor.cs | AIGMCompanionDardalion | support cooldown storage access | no | yes, but only accidentally | no | no | yes |

## Decoupling table
| File | Hardcoded symbol | Current purpose | Can replace with BaseHire: yes/no | Can replace with IAIGMActor: yes/no | Needs new interface/profile: yes/no | Risk level | Recommended replacement | Mutation phase to handle it |
|---|---|---|---|---|---|---|---|---|
| AIGMCompanionIntentParser.cs | Dakeyras / Danyal / Dardalion concrete class checks | speech identity / alias routing | no | no | yes | medium | companion identity/profile surface with alias metadata and companion id | parser-surface phase |
| AIGMCompanionIntentParser.cs | hardcoded alias lists in parser | named-address parsing | no | no | yes | medium | registry/profile map from alias -> companion id/profile key | parser-surface or registry phase |
| AIGMCompanionSkillExecutor.cs | concrete class casts for NextSupportActionUtc | support cooldown state | no | no | yes | medium | shared companion state surface (interface or helper access layer) | parser-surface or skill-decouple phase |
| AIGMCompanionSkillExecutor.cs | per-class cooldown getters/setters | support execution | no | no | yes | medium | IAIGMCompanionActor or equivalent support-state accessor | skill-decouple phase |

## Shared surface options evaluated
### Option A — Use BaseHire
- verdict: insufficient alone
- pros:
  - already present
  - compatible with companion mobiles
- cons:
  - too generic
  - does not express aliases, companion id, role/profile, or support cooldown state

### Option B — Use IAIGMActor
- verdict: useful anchor but insufficient alone
- pros:
  - already present in target
  - appropriate AIGM-layer abstraction
- cons:
  - no companion alias/profile fields
  - no companion support-state fields

### Option C — Introduce IAIGMCompanionActor
- verdict: recommended
- likely minimal fields/properties:
  - string CompanionId
  - string CompanionDisplayName
  - string CompanionProfileKey or CompanionRole
  - alias access or profile lookup hook
  - DateTime NextSupportActionUtc { get; set; }
  - ool GuardOwnerMode { get; set; }
  - AIGMExecutionMode ExecutionMode { get; set; }
- note:
  - likely should compose with or inherit from IAIGMActor conceptually

### Option D — Introduce AIGMCompanionProfile / registry
- verdict: also recommended
- likely fields:
  - Id
  - DisplayName
  - alias list
  - Role
  - allowed intent families
  - allowed skill families
  - optional personality/profile key
- note:
  - parser should resolve aliases through profile/registry rather than class-name checks

### Option E — Hybrid
- verdict: preferred overall
- use IAIGMActor as the general actor anchor
- add a small companion-specific surface for runtime companion state/capabilities
- add a companion profile/registry model for alias and identity mapping

## Parser dependency decision
- should parser parse names into concrete class types?
  - **no**
- should parser parse names into companion IDs / profile keys?
  - **yes**
- should parser resolve companion identity through a registry?
  - **yes**
- should parser produce AIGMCompanionIntent without needing concrete companion classes?
  - **yes**

Preferred parser direction:
- parser should depend on companion identity/profile data, not concrete companion class references
- minimal future-safe extension would be adding a companion target id/profile field to intent or a parallel addressing surface that later execution/policy layers can consume

## Skill executor dependency decision
- should skill executor dispatch by concrete class?
  - **no, not long-term**
- should it dispatch by IAIGMActor?
  - **not enough alone in current shape**
- should it dispatch by role/profile/capability?
  - **yes, where possible**
- which current skill behaviors genuinely require concrete companion mobile methods?
  - none of the inspected cooldown logic truly requires Dakeyras/Danyal/Dardalion specifically
  - it requires a shared mutable companion support-state surface

Preferred skill direction:
- skill executor should use a shared companion capability/state surface where possible
- concrete class checks, if any, should only be a temporary compatibility bridge

## Key answers
- whether parser can be ported without concrete companion classes: **yes, after shared identity/profile surface is defined**
- whether skill executor can be ported without concrete companion classes: **yes, after shared companion support-state surface is defined**
- whether new IAIGMCompanionActor is recommended: **yes**
- whether new AIGMCompanionProfile / registry is recommended: **yes**

## Recommended next mutation phase
- **Recommended next phase: Phase 56Q-R3-PARSER-SURFACE — Add IAIGMCompanionActor / companion profile model only + build**

Reason:
- it is the smallest mutation that makes future parser and skill decoupling possible
- it avoids prematurely importing broad concrete companion mobiles
- it keeps the system open for Dakeyras, Danyal, Dardalion, and later companions without rewriting parser logic for each new name

## Confirmations
- confirmation no files were copied: confirmed
- confirmation no patch was applied: confirmed
- confirmation no command/action/movement files were changed: confirmed
- confirmation no commit occurred: confirmed
