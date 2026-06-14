# Phase58A OldDev Closest-Hunt Parity Report

## old Dev files inspected
- `NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionTrackingSensor.cs`
- `NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionTrackingCycle.cs`
- `NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionActionExecutor.cs`
- `NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionDirectActionPolicy.cs`
- `NeoUO-Dev\Scripts\Custom\AIGM\AIGMCompanionAutoPathNavigator.cs`
- `NeoUO-Dev\Scripts\Custom\AIGM\AIGMMovementController.cs`
- `NeoUO-Dev\Scripts\Commands\AIGMCompanionCommand.cs`
- `NeoUO-Dev\Scripts\Commands\AIGMProofCommand.cs`
- `NeoUO-Dev\Scripts\Skills\Tracking.cs`

## method-level old-vs-current map
### tracking / category separation
- old Dev evidence:
  - `Tracking.cs` / `TrackWhoGump.DisplayTo(...)`
  - category separation by `IsAnimal`, `IsMonster`, `IsHumanNPC`, `IsPlayer`
  - nearest sort via `InternalSorter`
- current equivalent:
  - `AIGMCompanionTrackingService.MatchesCategory(...)`
  - `BuildTrackingSweepReport(...)`
- missing before this pass:
  - `MonsterHunt` acquisition did not use monster-category separation; it scanned all validator-legal creatures

### closest target selection
- old Dev evidence:
  - nearest-by-distance tracking list sort
- current equivalent:
  - `AIGMCompanionExecutionSpine.AcquireNearestMonsterTarget(...)`
- missing before this pass:
  - no hostile-monster prefilter before validator
  - cows/bovines could satisfy generic nearest-creature proofs

### movement handoff
- old Dev evidence:
  - `AIGMCompanionAutoPathNavigator.Step(...)`
  - `AIGMMovementController.TryMoveTo(...)`
- current equivalent:
  - `AIGMCompanionExecutionSpine.TickMonsterHunt(...)`
  - move-truth instrumentation from prior pass
- status:
  - retained current safe one-step bounded movement proof lane; no pathfinding/autonomy expansion added in this parity pass

### combat handoff
- old Dev evidence:
  - action executor / direct action policy / combat path handoff surfaces
- current equivalent:
  - `AIGMCompanionCombatController.TryEngageMonster(...)`
- status:
  - retained current native combat handoff; no broad combat expansion in this parity pass

## current files changed
- `Scripts\Custom\AIGM\AIGMCompanionExecutionState.cs`
- `Scripts\Custom\AIGM\AIGMCompanionTargetValidator.cs`
- `Scripts\Custom\AIGM\AIGMCompanionExecutionSpine.cs`
- `Scripts\Commands\AIGMScenarioCommand.cs`
- `Scripts\Commands\AIGMDumpCommand.cs`

## cow/bovine category drift
Fixed at the Phase58A MonsterHunt acquisition layer.

### before
- `MonsterHunt` could choose any nearest validator-legal creature
- animals such as cows could appear in proof runs

### after
- added hostile-monster candidate filter in `AIGMCompanionTargetValidator.IsHostileMonsterCandidate(...)`
- `ValidateMonsterTarget(...)` now uses that hostile-monster classification first
- `AcquireNearestMonsterTarget(...)` now applies monster-category filtering before validator acceptance
- animals/cows/bulls are rejected for `MonsterHunt` with reasons such as `animal_target`

## short commands added
- `[mh` → MonsterHunt alias
- `[mhp` → one-shot MonsterHunt proof alias
- `[md` → CompanionState dump alias
- `[ma` → ActionTrace dump alias
- `[mc` → CapabilityState dump alias

## one-shot proof behavior
Current `mhp` behavior:
- runs deterministic MonsterHunt scenario alias
- writes proof artifact under runtime docs
- echoes compact result line in-client

Note: this pass did not yet implement automatic hostile spawn placement. It stops the long-command churn and keeps proof in one short command path, but deterministic hostile setup may still depend on current scene content until a later narrow proof-harness patch.

## candidate / rejection proof visibility
Added candidate summary into execution state and dump/report output.
This records:
- accepted target name + distance
- rejected candidates/reasons (limited sample)

That is sufficient to show why a target was accepted or rejected without guessing.

## build result
Pending final build validation after patch set.

## proof artifact path
Current scenario command writes proof files under:
- `docs\runtime\PHASE58A_CLOSEST_MONSTER_HUNT_PROOF_<timestamp>.md` when using the one-shot proof alias path
- existing `PHASE58A_<SCENARIO>_RUNTIME_PROOF_<timestamp>.md` path remains for direct scenario command usage

## what is now proven
Once build passes, the code path will preserve all prior proof while binding MonsterHunt toward closest hostile-monster semantics instead of generic creature drift.

## what remains pending
- final build confirmation
- runtime proof that `MonsterHunt` no longer selects cows/bovines
- fully automated hostile spawn placement if desired for zero-manual proof setup
- B4 combat engagement trace proof
