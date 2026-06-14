# NEOUO FULL INTEGRATION PHASE56Q AIGM CORE COMMAND RUNTIME DRYRUN REPORT

- branch: $branch
- HEAD: $head

## git status before dry-run
?? NEOUO_FULL_INTEGRATION_AIGM_BASELINE_ABSTRACTIONS_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_AIGM_MISSING_TYPE_SURFACE_REPORT.md
?? NEOUO_FULL_INTEGRATION_COMPILE_GAP_INVENTORY_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_R_PATCH_FORMAT_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_S_PATCH_SCOPE_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_T_SPLIT_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56L_PREREQ_PATCH_DRY_RUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56O_R_BUILD_ERRORS.txt
?? NEOUO_FULL_INTEGRATION_PHASE56P_GREEN_BASELINE_AIGM_RECONCILIATION_COMMIT_REPORT.md
?? NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md


## Patch path
- $patch

## Patch exists
- True

## Patch size
- 135108

## Patch target file list
- diff --git a/Scripts/Commands/AIGMCompanionCommand.cs b/Scripts/Commands/AIGMCompanionCommand.cs
- diff --git a/Scripts/Custom/AIGM/AIGMActionExecutor.cs b/Scripts/Custom/AIGM/AIGMActionExecutor.cs
- diff --git a/Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs b/Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs
- diff --git a/Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs b/Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs
- diff --git a/Scripts/Custom/AIGM/AIGMCompanionIntent.cs b/Scripts/Custom/AIGM/AIGMCompanionIntent.cs
- diff --git a/Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs b/Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs
- diff --git a/Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs b/Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs
- diff --git a/Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs b/Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs
- diff --git a/Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs b/Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs
- diff --git a/Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs b/Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs


## git apply --numstat result
- exit code: 0
274	3	Scripts/Commands/AIGMCompanionCommand.cs 0	260	Scripts/Custom/AIGM/AIGMActionExecutor.cs 594	0	Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs 166	0	Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs 64	0	Scripts/Custom/AIGM/AIGMCompanionIntent.cs 562	0	Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs 390	0	Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs 404	0	Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs 35	0	Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs 236	0	Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs

## git apply --stat result
- exit code: 0
 Scripts/Commands/AIGMCompanionCommand.cs           |  277 +++++++++  Scripts/Custom/AIGM/AIGMActionExecutor.cs          |  260 ---------  Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs |  594 ++++++++++++++++++++  .../Custom/AIGM/AIGMCompanionDirectActionPolicy.cs |  166 ++++++  Scripts/Custom/AIGM/AIGMCompanionIntent.cs         |   64 ++  Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs   |  562 +++++++++++++++++++  Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs  |  390 +++++++++++++  Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs    |  404 ++++++++++++++  Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs  |   35 +  Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs  |  236 ++++++++  10 files changed, 2725 insertions(+), 263 deletions(-)

## git apply --check result
- exit code: 1
error: Scripts/Commands/AIGMCompanionCommand.cs: No such file or directory error: patch failed: Scripts/Custom/AIGM/AIGMActionExecutor.cs:326 error: Scripts/Custom/AIGM/AIGMActionExecutor.cs: patch does not apply

## Exact errors if any
error: Scripts/Commands/AIGMCompanionCommand.cs: No such file or directory error: patch failed: Scripts/Custom/AIGM/AIGMActionExecutor.cs:326 error: Scripts/Custom/AIGM/AIGMActionExecutor.cs: patch does not apply 
  Scripts/Commands/AIGMCompanionCommand.cs           |  277 +++++++++  Scripts/Custom/AIGM/AIGMActionExecutor.cs          |  260 ---------  Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs |  594 ++++++++++++++++++++  .../Custom/AIGM/AIGMCompanionDirectActionPolicy.cs |  166 ++++++  Scripts/Custom/AIGM/AIGMCompanionIntent.cs         |   64 ++  Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs   |  562 +++++++++++++++++++  Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs  |  390 +++++++++++++  Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs    |  404 ++++++++++++++  Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs  |   35 +  Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs  |  236 ++++++++  10 files changed, 2725 insertions(+), 263 deletions(-)

## Scope classification
- patch-readable but not safely applicable

## Patch overlap with Phase 56P committed files
- none

## Whether patch appears too broad
- True

## Confirmation
- no patch was applied
- no build was run for this phase
- no subsystem mutation occurred

## Recommendation for next phase
- Review/split/regenerate aigm-core-command-runtime patch before any apply phase
