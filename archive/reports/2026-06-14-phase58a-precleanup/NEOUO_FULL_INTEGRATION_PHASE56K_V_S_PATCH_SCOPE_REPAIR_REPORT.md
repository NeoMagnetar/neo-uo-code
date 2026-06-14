# NEOUO FULL INTEGRATION PHASE56K V S PATCH SCOPE REPAIR REPORT

- target branch: $(git -C C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg branch --show-current)
- target HEAD: $(git -C C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg rev-parse HEAD)
- previous patch path: $prevPatch
- repaired patch path: $patchOut
- patch byte size: 6806

## Target file existence/tracked-status table


## Diff headers found
- diff --git a/Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs b/Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs
- diff --git a/Scripts/Custom/AIGM/AIGMRequest.cs b/Scripts/Custom/AIGM/AIGMRequest.cs
- diff --git a/Scripts/Custom/AIGM/IAIGMActor.cs b/Scripts/Custom/AIGM/IAIGMActor.cs
- diff --git a/Scripts/Custom/AIGM/IAIGMInventoryCapability.cs b/Scripts/Custom/AIGM/IAIGMInventoryCapability.cs

## New-file entries found
- new file mode 100644
- new file mode 100644
- new file mode 100644

## Expected symbols found
- diff --git a/Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs b/Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs
- +++ b/Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs
- +    public sealed class AIGMCounselorInventoryCapability : IAIGMInventoryCapability
- +        public AIGMCounselorInventoryCapability(AIGMCounselor counselor)
- +    public class AIGMExecutionContext
- +        public AIGMExecutionContext Execution { get; set; }
- diff --git a/Scripts/Custom/AIGM/IAIGMActor.cs b/Scripts/Custom/AIGM/IAIGMActor.cs
- +++ b/Scripts/Custom/AIGM/IAIGMActor.cs
- +    public interface IAIGMActor
- +        IAIGMInventoryCapability Inventory { get; }
- diff --git a/Scripts/Custom/AIGM/IAIGMInventoryCapability.cs b/Scripts/Custom/AIGM/IAIGMInventoryCapability.cs
- +++ b/Scripts/Custom/AIGM/IAIGMInventoryCapability.cs
- +    public interface IAIGMInventoryCapability

## git apply --stat result
- succeeded

## git apply --check result
- exit code: 1
- result: failed

## Files changed
- no

## Temp worktree removed
- yes

## Can Phase 56M proceed?
- no
