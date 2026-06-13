# Phase58A Execution Spine Compile / Fix Report

## final status
**B) Compile passed — runtime not verified.**

## compile command used
```powershell
dotnet build .\ServUO.sln -c Release
```

## compile result
Build succeeded.
- 0 errors
- warnings remain outside the Phase58A change set, plus pre-existing AIGM unreachable-code warnings
- runtime was **not** executed in this pass

## exact errors encountered on first compile
1. `AIGMDumpCommand.cs` and `AIGMScenarioCommand.cs`
   - `GetDistanceToSqrt(...)` returned `double`, but code stored to `int`
2. `UMGMovementPhase58AExecutor.cs`
   - used non-existent `UMGMovementExecutionResult` fields (`RequestId`, `Allowed`, `Executed`, `Message`)
   - incorrectly assumed a movement API returned `bool`
3. `AIGMCompanionTargetValidator.cs`
   - referenced non-existent `BaseEscort`
4. `AIGMCompanionExecutionSpine.cs`
   - incorrectly assumed `MoveToWorld(...)` returned `bool`
   - used invalid `new Serial(int)` constructor shape
   - stored `GetDistanceToSqrt(...)` to `int` without cast

## files fixed during compile pass
- `Scripts/Commands/AIGMDumpCommand.cs`
- `Scripts/Commands/AIGMScenarioCommand.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTargetValidator.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementPhase58AExecutor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionSpine.cs`

## movement hardening changes
The provisional movement path was hardened away from world-jump semantics.

### before
- attempted `MoveToWorld(target.Location, target.Map)` as if it were a bool-returning live pursuit result
- too blunt / too close to fake success semantics for Phase58A acceptance

### after
- movement remains **monster-target-only**
- movement remains **short-range only**
- movement remains **step-based only**
- executor now validates the target immediately before acting
- executor denies movement if target is outside bounded short range
- executor turns the companion toward target and uses a single `Move(direction)` step
- no coordinate travel, no teleport, no recall/gate semantics added

### current limitation
This is still a first safe compile-stable step, not a final pursuit runtime. It needs future timer/tick orchestration and deeper router integration before claiming robust autonomous hunt behavior.

## target validator confirmation
Executable action flow now re-checks monster-target validity before action.

Current validator rejects:
- null / deleted / dead targets
- different map
- self
- owner
- companions
- players
- non-creatures
- invulnerable/blessed creatures
- controlled or summoned creatures
- owner-controlled allies
- vendors / escortable civilians
- human-bodied creatures
- out-of-range targets
- same-team creatures

Revalidation occurs:
- before pursuit movement in `AIGMCompanionExecutionSpine`
- before movement execution in `UMGMovementPhase58AExecutor`
- before combat engagement in `AIGMCompanionCombatController`

## self-sustain confirmation
### bandage
- routes through `AIGMCompanionHealingService.TryBeginSelfBandage(...)`
- no direct `Hits` mutation added
- relies on real bandage/healing path

### cure potion
- only attempts if poisoned and a `BaseCurePotion` is found in backpack
- uses real item drink call
- if potion is unavailable, behavior remains unavailable/deferred rather than faked
- no spell healing or spell cure added

## commands currently wired
### speech / natural language
- `hunt monsters`
- `attack monsters`
- `clear monsters`
- `start hunting`
- `start monster tracking`
- `track monsters`
- `start tracking monsters`
- `stop hunting`
- `stop tracking`
- `stop tracking monsters`
- `stop monster tracking`
- `monster status`
- `hunt status`
- `hunting status`

Alias handling already existing in shell path should still allow `waylander` routing to Dakeyras.

### GM commands added
- `[AIGMScenario MonsterHunt | Healing | Door | Tracking`
- `[AIGMDump CompanionState | CapabilityState | ActionTrace`

## commands still scaffold-only
- `AIGMScenario` currently writes proof scaffolds and current-state metadata only
- it does **not** yet spawn full controlled runtime encounter fixtures
- `AIGMDump` currently reports execution-state summary, not a full persistent trace ledger

## runtime verification status
Not runtime-verified.
No server start or live scenario execution was performed in this pass.

## recommended commit set
Recommended commit scope for this pass:
1. compile/fix/harden changes only for Phase58A pass 01
2. include:
   - execution state/spine files
   - target validator
   - combat/door/self-sustain services
   - short-step movement executor fix
   - intent/capability wiring
   - GM scenario/dump scaffolds
   - implementation + compile/fix reports
3. do **not** claim runtime success in commit message

Suggested interpretation:
- **compile-stable bounded monster-hunt scaffold**
- **not runtime-proven**

## next patch recommendation
Phase58A pass 02 should focus on:
1. timer/tick-driven hunt loop instead of one-shot speech-trigger step behavior
2. tighter integration through `UMGMovementRouter`
3. richer trace capture for `ActionTrace`
4. controlled GM scenario spawning for proof generation
5. stop-command/state cleanup validation during live runtime
