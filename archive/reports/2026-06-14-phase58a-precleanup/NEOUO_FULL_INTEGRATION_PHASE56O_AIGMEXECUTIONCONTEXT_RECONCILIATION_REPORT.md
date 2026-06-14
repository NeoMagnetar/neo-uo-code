# NEOUO FULL INTEGRATION PHASE56O AIGMEXECUTIONCONTEXT RECONCILIATION REPORT

- branch before: $branchBefore
- HEAD before: $headBefore

## Git status before
?? NEOUO_FULL_INTEGRATION_AIGM_BASELINE_ABSTRACTIONS_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_AIGM_MISSING_TYPE_SURFACE_REPORT.md
?? NEOUO_FULL_INTEGRATION_COMPILE_GAP_INVENTORY_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_R_PATCH_FORMAT_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_S_PATCH_SCOPE_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_T_SPLIT_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56L_PREREQ_PATCH_DRY_RUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56M_ADD_ONLY_ABSTRACTIONS_APPLY_BUILD_REPORT.md
?? NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md
?? Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs
?? Scripts/Custom/AIGM/IAIGMActor.cs
?? Scripts/Custom/AIGM/IAIGMInventoryCapability.cs


## Current Phase 56M files present
- Scripts/Custom/AIGM/IAIGMActor.cs
- Scripts/Custom/AIGM/IAIGMInventoryCapability.cs
- Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs

## Preserved source file inspected
- C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMRequest.cs

## Target AIGMRequest.cs inspected
- .\Scripts\Custom\AIGM\AIGMRequest.cs

### Target preview
using System;

namespace Server.Custom.AIGM
{
    public class AIGMRequest
    {
        public string RequestId { get; set; }
        public string TimestampUtc { get; set; }
        public string ShardName { get; set; }
        public string RequesterName { get; set; }
        public string AccessLevel { get; set; }
        public string MapName { get; set; }
        public string RegionName { get; set; }
        public string Question { get; set; }
        public AIGMTargetInfo Target { get; set; }
        public AIGMSceneContext Scene { get; set; }
    }
}


### Preserved Dev preview
using System;

using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMExecutionContext
    {
        public string Mode { get; set; }
        public string LastActionDescription { get; set; }
        public string LastActionResult { get; set; }
        public int StepCount { get; set; }
    }

    public class AIGMConversationContext
    {
        public string ActiveTaskSummary { get; set; }
        public string LastWorldSummary { get; set; }
        public List<AIGMConversationTurn> RecentTurns { get; set; }

        public AIGMConversationContext()
        {
            RecentTurns = new List<AIGMConversationTurn>();
        }
    }

    public class AIGMRequest
    {
        public string RequestId { get; set; }
        public string TimestampUtc { get; set; }
        public string ShardName { get; set; }
        public string RequesterName { get; set; }
        public string AccessLevel { get; set; }
        public string MapName { get; set; }
        public string RegionName { get; set; }
        public string Question { get; set; }
        public AIGMTargetInfo Target { get; set; }
        public AIGMSceneContext Scene { get; set; }
        public AIGMExecutionContext Execution { get; set; }
        public AIGMConversationContext Conversation { get; set; }
    }
}


## All AIGMExecutionContext references found

Scripts\Custom\AIGM\AIGMBridgeClient.cs:80:            AIGMExecutionContext execution = new AIGMExecutionContext();
Scripts\Custom\AIGM\AIGMBridgeClient.cs:90:        private static AIGMRequest BuildRequest(Mobile from, string 
question, AIGMTargetInfo target, AIGMExecutionContext execution)




## Selected implementation option
- Option A
- Created new file: Scripts/Custom/AIGM/AIGMExecutionContext.cs
- Reason: the missing type is standalone and can be safely extracted without broad AIGMRequest.cs replacement.

## Exact files changed
- Scripts/Custom/AIGM/AIGMExecutionContext.cs

## AIGMRequest.cs modified?
- no

## Git status after minimal reconciliation
?? NEOUO_FULL_INTEGRATION_AIGM_BASELINE_ABSTRACTIONS_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_AIGM_MISSING_TYPE_SURFACE_REPORT.md
?? NEOUO_FULL_INTEGRATION_COMPILE_GAP_INVENTORY_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_R_PATCH_FORMAT_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_S_PATCH_SCOPE_REPAIR_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56K_V_T_SPLIT_PATCH_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56L_PREREQ_PATCH_DRY_RUN_REPORT.md
?? NEOUO_FULL_INTEGRATION_PHASE56M_ADD_ONLY_ABSTRACTIONS_APPLY_BUILD_REPORT.md
?? NEOUO_FULL_INTEGRATION_TARGET_PREPARATION_REPORT.md
?? Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs
?? Scripts/Custom/AIGM/AIGMExecutionContext.cs
?? Scripts/Custom/AIGM/IAIGMActor.cs
?? Scripts/Custom/AIGM/IAIGMInventoryCapability.cs


## dotnet build result
- exit code: 1
- result: failed

## Exact remaining compile errors, if any
  Determining projects to restore...
  All projects are up-to-date for restore.
  Ultima -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Ultima.dll
  Server -> C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\ServUO.exe
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(34,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(45,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(48,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(93,13): error CS0246: The type or namespace name 'AIGMConversationContext' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(93,56): error CS0246: The type or namespace name 'AIGMConversationContext' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(96,58): error CS1061: 'AIGMSessionState' does not contain a definition for 'ActiveTaskSummary' and no accessible extension method 'ActiveTaskSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(97,57): error CS1061: 'AIGMSessionState' does not contain a definition for 'LastWorldSummary' and no accessible extension method 'LastWorldSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(98,29): error CS1061: 'AIGMSessionState' does not contain a definition for 'Conversation' and no accessible extension method 'Conversation' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(99,63): error CS1061: 'AIGMSessionState' does not contain a definition for 'Conversation' and no accessible extension method 'Conversation' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(114,17): error CS0117: 'AIGMRequest' does not contain a definition for 'Execution' [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(115,17): error CS0117: 'AIGMRequest' does not contain a definition for 'Conversation' [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(158,26): error CS1061: 'AIGMResponse' does not contain a definition for 'Plan' and no accessible extension method 'Plan' accepting a first argument of type 'AIGMResponse' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(159,26): error CS1061: 'AIGMResponse' does not contain a definition for 'Plan' and no accessible extension method 'Plan' accepting a first argument of type 'AIGMResponse' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(64,159): error CS1061: 'AIGMActionProposal' does not contain a definition for 'Title' and no accessible extension method 'Title' accepting a first argument of type 'AIGMActionProposal' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(55,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(64,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(101,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(138,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(142,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(151,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(165,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(175,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(109,37): error CS1061: 'AIGMSessionState' does not contain a definition for 'ActiveTaskSummary' and no accessible extension method 'ActiveTaskSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(111,37): error CS1061: 'AIGMSessionState' does not contain a definition for 'AddTurn' and no accessible extension method 'AddTurn' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(114,52): error CS0103: The name 'AIGMProposalAugmenter' does not exist in the current context [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(117,29): error CS0103: The name 'AIGMExecutionLog' does not exist in the current context [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(131,41): error CS1061: 'AIGMSessionState' does not contain a definition for 'AddTurn' and no accessible extension method 'AddTurn' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(132,41): error CS1061: 'AIGMSessionState' does not contain a definition for 'LastWorldSummary' and no accessible extension method 'LastWorldSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(107,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(119,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(192,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(252,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Distance' and no accessible extension method 'Distance' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(253,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsContainer' and no accessible extension method 'IsContainer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(254,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsDoor' and no accessible extension method 'IsDoor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(255,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsStatic' and no accessible extension method 'IsStatic' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(256,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsMovable' and no accessible extension method 'IsMovable' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(257,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsAlive' and no accessible extension method 'IsAlive' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(258,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'ParentTypeName' and no accessible extension method 'ParentTypeName' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(259,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsContainer' and no accessible extension method 'IsContainer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(260,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsDoor' and no accessible extension method 'IsDoor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(261,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsStatic' and no accessible extension method 'IsStatic' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(262,40): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsMovable' and no accessible extension method 'IsMovable' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(263,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'ParentTypeName' and no accessible extension method 'ParentTypeName' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(280,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Distance' and no accessible extension method 'Distance' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(281,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsPlayer' and no accessible extension method 'IsPlayer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(282,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsNpc' and no accessible extension method 'IsNpc' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(283,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsVendor' and no accessible extension method 'IsVendor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(284,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsAlive' and no accessible extension method 'IsAlive' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(285,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsPlayer' and no accessible extension method 'IsPlayer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(286,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsNpc' and no accessible extension method 'IsNpc' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(287,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsVendor' and no accessible extension method 'IsVendor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(288,40): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsAlive' and no accessible extension method 'IsAlive' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(300,27): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Tags' and no accessible extension method 'Tags' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(301,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Tags' and no accessible extension method 'Tags' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(67,35): error CS0246: The type or namespace name 'AIGMCounselorManagementGump' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(68,31): error CS0246: The type or namespace name 'AIGMCounselorManagementGump' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(276,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]

Build FAILED.

C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(34,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(45,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(48,25): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(55,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(64,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(101,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(138,17): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(142,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(151,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(165,21): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(175,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(107,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(119,33): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(192,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(276,13): warning CS0162: Unreachable code detected [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(93,13): error CS0246: The type or namespace name 'AIGMConversationContext' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(93,56): error CS0246: The type or namespace name 'AIGMConversationContext' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(96,58): error CS1061: 'AIGMSessionState' does not contain a definition for 'ActiveTaskSummary' and no accessible extension method 'ActiveTaskSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(97,57): error CS1061: 'AIGMSessionState' does not contain a definition for 'LastWorldSummary' and no accessible extension method 'LastWorldSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(98,29): error CS1061: 'AIGMSessionState' does not contain a definition for 'Conversation' and no accessible extension method 'Conversation' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(99,63): error CS1061: 'AIGMSessionState' does not contain a definition for 'Conversation' and no accessible extension method 'Conversation' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(114,17): error CS0117: 'AIGMRequest' does not contain a definition for 'Execution' [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(115,17): error CS0117: 'AIGMRequest' does not contain a definition for 'Conversation' [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(158,26): error CS1061: 'AIGMResponse' does not contain a definition for 'Plan' and no accessible extension method 'Plan' accepting a first argument of type 'AIGMResponse' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Custom\AIGM\AIGMBridgeClient.cs(159,26): error CS1061: 'AIGMResponse' does not contain a definition for 'Plan' and no accessible extension method 'Plan' accepting a first argument of type 'AIGMResponse' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMResponseGump.cs(64,159): error CS1061: 'AIGMActionProposal' does not contain a definition for 'Title' and no accessible extension method 'Title' accepting a first argument of type 'AIGMActionProposal' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(109,37): error CS1061: 'AIGMSessionState' does not contain a definition for 'ActiveTaskSummary' and no accessible extension method 'ActiveTaskSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(111,37): error CS1061: 'AIGMSessionState' does not contain a definition for 'AddTurn' and no accessible extension method 'AddTurn' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(114,52): error CS0103: The name 'AIGMProposalAugmenter' does not exist in the current context [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(117,29): error CS0103: The name 'AIGMExecutionLog' does not exist in the current context [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(131,41): error CS1061: 'AIGMSessionState' does not contain a definition for 'AddTurn' and no accessible extension method 'AddTurn' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(132,41): error CS1061: 'AIGMSessionState' does not contain a definition for 'LastWorldSummary' and no accessible extension method 'LastWorldSummary' accepting a first argument of type 'AIGMSessionState' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(252,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Distance' and no accessible extension method 'Distance' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(253,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsContainer' and no accessible extension method 'IsContainer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(254,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsDoor' and no accessible extension method 'IsDoor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(255,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsStatic' and no accessible extension method 'IsStatic' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(256,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsMovable' and no accessible extension method 'IsMovable' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(257,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsAlive' and no accessible extension method 'IsAlive' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(258,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'ParentTypeName' and no accessible extension method 'ParentTypeName' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(259,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsContainer' and no accessible extension method 'IsContainer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(260,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsDoor' and no accessible extension method 'IsDoor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(261,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsStatic' and no accessible extension method 'IsStatic' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(262,40): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsMovable' and no accessible extension method 'IsMovable' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(263,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'ParentTypeName' and no accessible extension method 'ParentTypeName' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(280,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Distance' and no accessible extension method 'Distance' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(281,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsPlayer' and no accessible extension method 'IsPlayer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(282,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsNpc' and no accessible extension method 'IsNpc' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(283,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsVendor' and no accessible extension method 'IsVendor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(284,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsAlive' and no accessible extension method 'IsAlive' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(285,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsPlayer' and no accessible extension method 'IsPlayer' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(286,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsNpc' and no accessible extension method 'IsNpc' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(287,39): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsVendor' and no accessible extension method 'IsVendor' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(288,40): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'IsAlive' and no accessible extension method 'IsAlive' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(300,27): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Tags' and no accessible extension method 'Tags' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Gumps\AIGMQuestionGump.cs(301,26): error CS1061: 'AIGMTargetInfo' does not contain a definition for 'Tags' and no accessible extension method 'Tags' accepting a first argument of type 'AIGMTargetInfo' could be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(67,35): error CS0246: The type or namespace name 'AIGMCounselorManagementGump' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Mobiles\NPCs\AIGMCounselor.cs(68,31): error CS0246: The type or namespace name 'AIGMCounselorManagementGump' could not be found (are you missing a using directive or an assembly reference?) [C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\Scripts\Scripts.csproj]
    15 Warning(s)
    42 Error(s)

Time Elapsed 00:00:20.60


## Recommendation for commit/no-commit
- Do not commit yet; report remaining narrow errors for next-phase handling.

## Recommended next phase
- Next narrow reconciliation phase based on remaining compile errors
