# NEOUO FULL INTEGRATION AIGM MISSING TYPE SURFACE REPORT

- Purpose: locate the missing AIGM type/interface definitions needed to make the full-server integration target baseline-compile before any subsystem patch dry-run.

## Target branch / HEAD

- Branch: `neo/staging-aigm`
- HEAD: `feefa82bde7bb5cb9987eed29ea090e9a123b3a2`

## Build result

Command used:
- `dotnet build .\ServUO.sln -v:minimal`

Result:
- failed
- warnings: `0`
- errors: `3`

## Missing symbols

- `AIGMExecutionContext`
- `IAIGMActor`
- `IAIGMInventoryCapability`

## Reference locations in full integration target

### `AIGMExecutionContext`
Referenced in:
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs:80`
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs:90`

Definition in full integration target:
- not found

### `IAIGMActor`
Referenced in:
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs:13`

Definition in full integration target:
- not found

### `IAIGMInventoryCapability`
Referenced in:
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs:41`

Definition in full integration target:
- not found

## Definitions found in preserved Dev

### `AIGMExecutionContext`
- definition found: yes
- defining file:
  - `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMRequest.cs`
- note:
  - this is embedded inside the AIGM request model file rather than a standalone file

### `IAIGMActor`
- definition found: yes
- defining file:
  - `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\IAIGMActor.cs`

### `IAIGMInventoryCapability`
- definition found: yes
- defining file:
  - `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\IAIGMInventoryCapability.cs`

## Related preserved Dev surfaces discovered

Additional directly related supporting type:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\AIGM\AIGMCounselorInventoryCapability.cs`
  - implements `IAIGMInventoryCapability`

Observed references in preserved Dev:
- `AIGMActionExecutor.cs`
- `AIGMBridgeClient.cs`
- `AIGMNativeAddAdapter.cs`
- `AIGMCounselor.cs`
- `AIGMRequest.cs`
- `IAIGMActor.cs`
- `IAIGMInventoryCapability.cs`
- `AIGMCounselorInventoryCapability.cs`

## Patch bundle coverage result

Searched existing repaired patch bundle:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\migration-patches\phase56j-repair-20260607-083446`

Result:
- no existing patch contains:
  - `AIGMExecutionContext`
  - `IAIGMActor`
  - `IAIGMInventoryCapability`
  - `AIGMCounselorInventoryCapability`

Interpretation:
- existing subsystem patch bundles include references that assume these abstractions exist
- but the abstraction layer itself was omitted from the current patch bundle set

## Per-symbol classification

### `AIGMExecutionContext`
- referenced in integration target: yes
- defined in integration target: no
- defined in preserved Dev: yes
- defining file: `Scripts\Custom\AIGM\AIGMRequest.cs`
- included in existing patch bundle: no
- likely patch group role: prerequisite core abstraction / bridge dependency

### `IAIGMActor`
- referenced in integration target: yes
- defined in integration target: no
- defined in preserved Dev: yes
- defining file: `Scripts\Custom\AIGM\IAIGMActor.cs`
- included in existing patch bundle: no
- likely patch group role: prerequisite core abstraction / counselor dependency

### `IAIGMInventoryCapability`
- referenced in integration target: yes
- defined in integration target: no
- defined in preserved Dev: yes
- defining file: `Scripts\Custom\AIGM\IAIGMInventoryCapability.cs`
- included in existing patch bundle: no
- likely patch group role: prerequisite core abstraction / counselor inventory dependency

## Recommended baseline repair path

Outcome:
- **Option B**

Meaning:
- the missing definitions do exist in preserved Dev
- but they are **not** present in any current patch bundle

Recommendation:
- create a new prerequisite patch:
  - `aigm-baseline-abstractions.patch`

Minimum likely contents:
- `Scripts\Custom\AIGM\AIGMRequest.cs` (because it defines `AIGMExecutionContext`)
- `Scripts\Custom\AIGM\IAIGMActor.cs`
- `Scripts\Custom\AIGM\IAIGMInventoryCapability.cs`
- likely also:
  - `Scripts\Custom\AIGM\AIGMCounselorInventoryCapability.cs`

Reason for likely including `AIGMCounselorInventoryCapability.cs`:
- `AIGMCounselor.cs` constructs `new AIGMCounselorInventoryCapability(this)`
- even after restoring the interface, that concrete type may still be required for successful baseline compile

## Can Phase 56L proceed now?

- No

Reason:
- baseline compile target is still red
- prerequisite AIGM abstraction layer is missing from the target and from the current patch bundle set
- first safe next step is to create the prerequisite baseline abstraction patch before any dry-run of the larger subsystem bundles

## Recommended next phase

- **Phase 56K-V: Create Prerequisite AIGM Baseline Abstractions Patch**

That phase should generate a small targeted patch for the missing abstraction layer only.
After that, the next safety gate should be:
- `git apply --check` for the prerequisite patch against the full integration target

## Boundary confirmation

This phase did **not**:
- apply patches
- copy files
- edit code
- clean anything
- modify preserved Dev
- modify staging/prod/baseline sources
- implement loader work
- restart any server
