# NEOUO FULL INTEGRATION COMPILE GAP INVENTORY REPORT

- Purpose: classify the existing baseline compile errors in the full-server integration target before any patch dry-run or patch application.

## Target state

- Target path: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- Branch: `neo/staging-aigm`
- HEAD: `feefa82bde7bb5cb9987eed29ea090e9a123b3a2`

## Baseline build result

Command used:
- `dotnet build .\ServUO.sln -v:minimal`

Result:
- failed
- warnings: `0`
- errors: `3`

## Baseline compile errors

### 1. `AIGMExecutionContext`
- file: `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
- line: `90`
- error: `CS0246`
- context:
  - `ContinueAfterAction(...)` creates a new `AIGMExecutionContext`
  - `BuildRequest(...)` also takes `AIGMExecutionContext execution`

Search result:
- symbol appears only as usage sites in `AIGMBridgeClient.cs`
- no definition found anywhere in the full integration target

Classification:
- **missing local type definition**

Likely meaning:
- this lane expects an execution-context model that is absent from the copied staging-old tree

### 2. `IAIGMActor`
- file: `Scripts\Mobiles\NPCs\AIGMCounselor.cs`
- line: `13`
- error: `CS0246`
- context:
  - `public class AIGMCounselor : PlayerVendor, IAIGMActor`

Search result:
- symbol appears only as usage in `AIGMCounselor.cs`
- no interface definition found anywhere in the full integration target

Classification:
- **missing local interface definition**

Likely meaning:
- counselor surface was written against a more advanced AIGM actor abstraction that is not present in this target

### 3. `IAIGMInventoryCapability`
- file: `Scripts\Mobiles\NPCs\AIGMCounselor.cs`
- line: `41`
- error: `CS0246`
- context:
  - `public IAIGMInventoryCapability Inventory { get { return new AIGMCounselorInventoryCapability(this); } }`

Search result:
- symbol appears only as usage in `AIGMCounselor.cs`
- no interface definition found anywhere in the full integration target

Classification:
- **missing local interface definition**

Likely meaning:
- counselor inventory capability abstractions are partially referenced but not fully present in this tree

## AIGM file inventory observation

The target does contain a partial AIGM surface, including files such as:
- `AIGMActionExecutor.cs`
- `AIGMActionHistory.cs`
- `AIGMActionPreview.cs`
- `AIGMActionProposal.cs`
- `AIGMBridgeClient.cs`
- `AIGMCommandAction.cs`
- `AIGMFileInsightStore.cs`
- `AIGMInvestigationState.cs`
- `AIGMRequest.cs`
- `AIGMResponse.cs`
- `AIGMResumeAdvisor.cs`
- `AIGMSceneContext.cs`
- `AIGMSceneScanner.cs`
- `AIGMSessionState.cs`
- `AIGMSettings.cs`
- `AIGMStubResponder.cs`
- `AIGMTargetInfo.cs`

Interpretation:
- the lane is not missing all AIGM work
- it is missing a specific abstraction layer used by counselor/bridge behavior
- this looks more like an incomplete or out-of-sync AIGM slice than a general ServUO failure

## Gap categories

### Category A: Missing abstraction/interface layer
- `AIGMExecutionContext`
- `IAIGMActor`
- `IAIGMInventoryCapability`

These are the immediate blockers.

### Category B: Possible dependent concrete types not yet evaluated
Because the missing interfaces/classes stop compilation early, related types may also be absent or unresolved later, for example:
- `AIGMCounselorInventoryCapability`
- any actor/capability contracts tied to companion inventory or execution flow

These should be treated as probable follow-on investigation targets, but not asserted as missing until directly checked.

## Recommended interpretation

This full integration target is:
- structurally valid as a full-server build lane
- but **not baseline-clean** because it already contains an incomplete AIGM abstraction layer

This means patch dry-runs are still blocked, but now for a much narrower and more actionable reason than before.

## Recommended next safe step

Recommended next phase:
- **Phase 56K-U: Baseline AIGM Missing-Type Surface Inventory**

That phase should answer:
- are these missing types present in preserved `NeoUO-Dev`?
- if yes, which exact files define them?
- are they part of the preserved patch bundles already, or outside them?
- what is the minimal baseline-repair set needed to make the full integration target compile before patch dry-runs?

## Boundary confirmation

This phase did **not**:
- apply patches
- run `git apply --check`
- modify target code
- modify preserved Dev
- modify staging/prod/baseline lanes
- implement loader work
- restart any server

## Bottom line

The baseline compile failure is now classified as a **missing AIGM abstraction layer problem** in the full integration target, not a generic full-server build failure.
