# NEOUO FULL INTEGRATION PHASE56Q-R CORE COMMAND RUNTIME SPLIT REGENERATION REPORT

- branch: $branch
- HEAD: $head

## Baseline build result
- exit code: 0
- result: passed

## Old patch path
- $oldPatch

## Old patch failure summary
- old patch was git-readable but not applicable
- failures observed previously:
  - Scripts/Commands/AIGMCompanionCommand.cs: No such file or directory
  - patch failed: Scripts/Custom/AIGM/AIGMActionExecutor.cs:326
  - Scripts/Custom/AIGM/AIGMActionExecutor.cs: patch does not apply

## Current target file existence table

File                                                   Exists Tracked
----                                                   ------ -------
Scripts/Commands/AIGMCompanionCommand.cs                False   False
Scripts/Custom/AIGM/AIGMActionExecutor.cs                True    True
Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs      False   False
Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs  False   False
Scripts/Custom/AIGM/AIGMCompanionIntent.cs              False   False
Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs        False   False
Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs       False   False
Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs         False   False
Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs       False   False
Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs       False   False




## Preserved Dev source file existence table

File                                                   Exists
----                                                   ------
Scripts/Commands/AIGMCompanionCommand.cs                 True
Scripts/Custom/AIGM/AIGMActionExecutor.cs                True
Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs       True
Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs   True
Scripts/Custom/AIGM/AIGMCompanionIntent.cs               True
Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs         True
Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs        True
Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs          True
Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs        True
Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs        True




## Bucket classification for old patch targets

Bucket Meaning                   Files                                                                                 
------ -------                   -----                                                                                 
A      Command surface           Scripts/Commands/AIGMCompanionCommand.cs                                              
B      Action executor core      Scripts/Custom/AIGM/AIGMActionExecutor.cs; Scripts/Custom/AIGM/AIGMCompanionActionE...
C      Intent/parser/skill/state Scripts/Custom/AIGM/AIGMCompanionIntent.cs; Scripts/Custom/AIGM/AIGMCompanionIntent...
D      Movement/router           Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs; Scripts/Custom/AIGM/Movement/UMG...




## Generated patch folder
- $root

## Generated candidate patch names
- aigm-command-surface-only.patch - aigm-action-executor-core-only.patch - aigm-intent-parser-skill-state-only.patch - aigm-movement-router-only.patch

## Dry-run result for each candidate
### aigm-command-surface-only.patch
- numstat exit: 128
git : error: No valid patches in input (allow with "--allow-empty")
At line:81 char:14
+   $numstat = git apply --numstat $out 2>&1 | Out-String
+              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (error: No valid...--allow-empty"):String) [], RemoteException
    + FullyQualifiedErrorId : NativeCommandError
 

- stat exit: 128
git : error: No valid patches in input (allow with "--allow-empty")
At line:83 char:11
+   $stat = git apply --stat $out 2>&1 | Out-String
+           ~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (error: No valid...--allow-empty"):String) [], RemoteException
    + FullyQualifiedErrorId : NativeCommandError
 

- check exit: 128
git : error: No valid patches in input (allow with "--allow-empty")
At line:85 char:12
+   $check = git apply --check $out 2>&1 | Out-String
+            ~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (error: No valid...--allow-empty"):String) [], RemoteException
    + FullyQualifiedErrorId : NativeCommandError
 

- safe to apply later: no

### aigm-action-executor-core-only.patch
- numstat exit: 0
512	154	Scripts/Custom/AIGM/AIGMActionExecutor.cs
594	0	Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs
166	0	Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs

- stat exit: 0
 Scripts/Custom/AIGM/AIGMActionExecutor.cs          |  666 +++++++++++++++-----
 Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs |  594 ++++++++++++++++++
 .../Custom/AIGM/AIGMCompanionDirectActionPolicy.cs |  166 +++++
 3 files changed, 1272 insertions(+), 154 deletions(-)

- check exit: 1
git : error: patch failed: Scripts/Custom/AIGM/AIGMActionExecutor.cs:1
At line:85 char:12
+   $check = git apply --check $out 2>&1 | Out-String
+            ~~~~~~~~~~~~~~~~~~~~~~~~~~~
    + CategoryInfo          : NotSpecified: (error: patch fa...onExecutor.cs:1:String) [], RemoteException
    + FullyQualifiedErrorId : NativeCommandError
 
error: Scripts/Custom/AIGM/AIGMActionExecutor.cs: patch does not apply

- safe to apply later: no

### aigm-intent-parser-skill-state-only.patch
- numstat exit: 0
64	0	Scripts/Custom/AIGM/AIGMCompanionIntent.cs
562	0	Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs
390	0	Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs
404	0	Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs

- stat exit: 0
 Scripts/Custom/AIGM/AIGMCompanionIntent.cs        |   64 ++
 Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs  |  562 +++++++++++++++++++++
 Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs |  390 +++++++++++++++
 Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs   |  404 +++++++++++++++
 4 files changed, 1420 insertions(+)

- check exit: 0

- safe to apply later: yes

### aigm-movement-router-only.patch
- numstat exit: 0
35	0	Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs
236	0	Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs

- stat exit: 0
 Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs |   35 +++
 Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs |  236 +++++++++++++++++++++
 2 files changed, 271 insertions(+)

- check exit: 0

- safe to apply later: yes



## Whether movement/router was separated
- yes

## Confirmation
- no old patch was applied
- no regenerated patch was applied
- no commit occurred

## Recommendation for next phase
- Apply candidate later: aigm-intent-parser-skill-state-only.patch
- Apply candidate later: aigm-movement-router-only.patch
