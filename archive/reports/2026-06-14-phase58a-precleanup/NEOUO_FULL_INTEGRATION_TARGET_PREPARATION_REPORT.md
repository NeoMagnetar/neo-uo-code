# NEOUO FULL INTEGRATION TARGET PREPARATION REPORT

- Purpose: prepare a clean, full-server-capable integration target for later patch dry-runs and compile validation without applying any patches.

## Candidate source inventory summary

### `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`
- exists: yes
- full build surface: yes (`ServUO.sln`, `Server`, `Ultima`, `Scripts`)
- git repo: yes
- branch: `preserve/aigm-companion-working-20260607`
- status: dirty
- rejected as source because it is the preserved working fork and must remain untouched

### `C:\UO\Server\Neo Ultima Online\NeoUO-Staging`
- exists: yes
- full build surface: yes
- git repo: no
- useful as a filesystem lane, but weaker for source traceability

### `C:\UO\Server\Neo Ultima Online\NeoUO-Staging-Old-20260604-155950`
- exists: yes
- full build surface: yes
- git repo: yes
- branch: `neo/staging-aigm`
- HEAD: `feefa82bd Promote base AIGM counselor surfaces from dev to staging`
- status: clean
- selected as safest source for a new full-server integration copy

### `C:\UO\Server\Neo Ultima Online\Production-Preserved-Original`
- exists: yes
- full build surface: yes
- git repo: yes
- branch: `pub57`
- status: dirty config/data
- not selected because it is protected preserved original

### `C:\UO\Server\ServUO`
- exists: yes
- full build surface: yes
- git repo: yes
- branch: `pub57`
- status: dirty config/data
- not selected because it is baseline reference and already locally modified

## Selected source path

- `C:\UO\Server\Neo Ultima Online\NeoUO-Staging-Old-20260604-155950`

## Target path

- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`

## Copy method

- new dedicated target created via `robocopy`
- excluded from copy:
  - `bin`
  - `obj`
  - `_buildcheck`
  - `.vs`
  - `*.log`

## Git state of target

- target has `.git`: yes
- branch after copy: `neo/staging-aigm`
- HEAD after copy: `feefa82bd Promote base AIGM counselor surfaces from dev to staging`
- no branch switching or patch application was performed in this phase

## Solution / project files found

- `ServUO.sln`: yes
- `Server\Server.csproj`: yes
- `Ultima\Ultima.csproj`: yes
- `Scripts\Scripts.csproj`: yes

## Build command used

- `dotnet build .\ServUO.sln`

## Build result

- build progressed through full server restore/build surface
- result: failed
- warnings: `0`
- errors: `3`

### Error surfaces
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
  - missing `AIGMExecutionContext`
- `Scripts\Mobiles\NPCs\AIGMCounselor.cs`
  - missing `IAIGMActor`
  - missing `IAIGMInventoryCapability`

## Interpretation

This target is significantly better than the partial GitHub clone because:
- it has the full solution/build surface
- it can compile the full ServUO/NeoUO solution pipeline

But it is still **not baseline-clean**.
It carries pre-existing AIGM compile breakage from the selected staging-old source.

So the operational conclusion is:
- full-server integration target exists: yes
- full build surface exists: yes
- baseline build result is known: yes
- baseline is not green yet

## Patch bundle path confirmed

- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\migration-patches\phase56j-repair-20260607-083446`

## Patch application status

- no patches were applied
- no `git apply` was run
- no runtime bundle was copied into ServUO

## Recommended next phase

Because the target is full-server-capable but not baseline-clean, the safest next step is:
- **Phase 56K-T: Baseline AIGM Compile Gap Inventory for Full Integration Target**

That phase should:
- classify the three pre-existing compile gaps
- determine whether they are missing files, missing references, or intentionally incomplete staging-old work
- avoid applying migration patches until the baseline target is at least understood as a compile lane

## Bottom line

- full-server integration target now exists
- preserved Dev, Staging, Production-Preserved-Original, and baseline ServUO remained untouched
- this target is a much better future patch-validation lane than the partial GitHub clone
- but it still needs baseline compile-gap understanding before patch dry-runs begin
