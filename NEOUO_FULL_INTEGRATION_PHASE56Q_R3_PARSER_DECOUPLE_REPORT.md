# NEOUO FULL INTEGRATION PHASE56Q-R3-PARSER-DECOUPLE REPORT

Date: 2026-06-07 18:36:45 -09:00

## Target state
- branch: neo/staging-aigm
- HEAD: 7c72e7bcb613c53c7deb426b246184e017cb7cb6
- latest commit: 7c72e7bcb feat: add shared AIGM companion actor surface
- git status before parser port:
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
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_DECOUPLING_PLAN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_SURFACE_P_COMMIT_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R3_REG_BUCKET_C_PATCH_REGENERATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56Q_R_CORE_COMMAND_RUNTIME_SPLIT_REGENERATION_REPORT.md
?? NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md
?? Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs
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
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_DECOUPLING_PLAN_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_PARSER_SURFACE_P_COMMIT_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R3_REG_BUCKET_C_PATCH_REGENERATION_REPORT.md
NEOUO_FULL_INTEGRATION_PHASE56Q_R_CORE_COMMAND_RUNTIME_SPLIT_REGENERATION_REPORT.md
NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md
`

## Baseline build result before parser port
`	ext
  Determining projects to restore...
  All projects are up-to-date for restore.
  Ultima -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Ultima.dll
  Server -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\ServUO.exe
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(34,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(45,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(48,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(107,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(119,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(192,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(55,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(64,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(101,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(138,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(142,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(151,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(165,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(175,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(276,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
  Scripts -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts.dll

Build succeeded.

C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(34,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(45,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(48,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(107,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(119,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(192,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(55,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(64,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(101,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(138,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(142,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(151,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(165,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(175,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(276,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
    15 Warning(s)
    0 Error(s)

Time Elapsed 00:00:38.38
`
- result: PASSED

## Source parser inspected
- source file: C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionIntentParser.cs
- concrete companion class references found in source:
  - AIGMCompanionDakeyras
  - AIGMCompanionDanyal
  - AIGMCompanionDardalion
- parser methods reviewed:
  - IsClearlyAddressedToDifferentCompanion
  - TryParse
  - TryExtractAddressedCommand
  - TryStripAnyKnownCompanionPrefix
  - TryBuildTravelIntent
  - ResolveAttackTarget
  - normalization / canonicalization helpers
- command phrases handled:
  - location / scan / tracking / travel / follow / greet / guard / heal / cure / bandage / attack / stay / come / stop-combat intents
- hidden dependency scan result:
  - no direct skill executor, state access, action executor, or movement/router dependency inside parser logic itself
  - only concrete companion identity coupling was removed

## Target shared surfaces inspected
- AIGMCompanionIntent.cs
- IAIGMCompanionActor.cs
- AIGMCompanionProfile.cs
- IAIGMActor.cs
- compile target conclusion:
  - parser can compile against target shared surfaces plus existing ServUO BaseHire / Mobile types only

## Parser adaptation strategy
- adapted preserved Dev parser into a parser-only target file
- removed all concrete companion mobile class references
- replaced class-based identity checks with profile/string alias resolution
- added optional profile-list overloads while preserving a simple TryParse(BaseHire, Mobile, string, out AIGMCompanionIntent) entrypoint
- used default data-only profile identities for:
  - Dakeyras
  - Danyal
  - Dardalion
- kept parser intent-only:
  - string matching
  - alias matching
  - profile lookup
  - intent construction
  - target serial resolution for attack target only via existing world reads

## Concrete companion references removed
- removed references to:
  - AIGMCompanionDakeyras
  - AIGMCompanionDanyal
  - AIGMCompanionDardalion
- Dakeyras / Danyal / Dardalion are now represented only as profile/string identity data: yes

## Files changed
- Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs

## Scope verification
- git diff --stat:
`	ext

`
- git diff -- Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs:
`	ext

`

## Confirmations
- confirmation skill executor was not copied: confirmed
- confirmation state access was not copied: confirmed
- confirmation companion mobiles were not copied: confirmed
- confirmation no command/action/movement files changed: confirmed
- forbidden changed path hits:
`	ext
none
`

## Build result
`	ext
  Determining projects to restore...
  All projects are up-to-date for restore.
  Ultima -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Ultima.dll
  Server -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\ServUO.exe
  Scripts -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.83
`
- exit code: 0
- classification: success

## Exact errors if any
none

## Recommendation for next phase
- Recommended next phase: Phase 56Q-R3-PARSER-DECOUPLE-P — Commit Decoupled Companion Intent Parser
