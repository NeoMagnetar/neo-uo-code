# NEOUO FULL INTEGRATION PHASE56K V R PATCH FORMAT REPAIR REPORT

- target branch: $(git -C C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg branch --show-current)
- target HEAD: $(git -C C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg rev-parse HEAD)
- invalid patch path: $invalidPatch
- repaired patch folder: $root
- repaired patch path: $patchOut
- patch byte size: 1387

## Diff headers found
- diff --git a/Scripts/Custom/AIGM/AIGMRequest.cs b/Scripts/Custom/AIGM/AIGMRequest.cs

## Expected symbols found
- +    public class AIGMExecutionContext
- +        public AIGMExecutionContext Execution { get; set; }

## Excluded path check result
- no excluded path hits

## git apply --stat result
- succeeded

## git apply --check result
- exit code: 1
- result: failed

## Files changed
- no

## Files staged
- no

## Temp worktree removed
- no

## Can Phase 56L be treated as satisfied now?
- no

## Recommended next phase
- Re-check patch generation/validation before any apply phase
