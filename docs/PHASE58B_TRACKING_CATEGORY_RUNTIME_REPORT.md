# Phase58B Tracking Category Runtime Report

## status
Current canonical status after metadata consolidation and attempted runtime pass:
- Branch at proof-attempt time: `neo/phase56t-clean-speech-recovery`
- HEAD at proof-attempt time: `5266f370f0d0fedf9b019812e675040d57954fc8`
- Runtime label for the latest pass: `PHASE58B-BLOCKED`

## old Dev methods inspected
- `Tracking.cs` category split:
  - `IsAnimal`
  - `IsMonster`
  - `IsHumanNPC`
  - `IsPlayer`
  - nearest sorting through `InternalSorter`
- `AIGMCompanionTrackingSensor.cs`
- `AIGMCompanionTrackingCycle.cs`
- `AIGMCompanionActionExecutor.cs`
- `AIGMCompanionCommand.cs`
- `AIGMProofCommand.cs`

## current target files for this pass
- `Scripts\Custom\AIGM\AIGMCompanionTrackingService.cs`
- `Scripts\Custom\AIGM\AIGMCompanionTrackingMode.cs`
- `Scripts\Custom\AIGM\AIGMCompanionTrackingState.cs`
- `Scripts\Custom\AIGM\AIGMCompanionExecutionSpine.cs`
- `Scripts\Custom\AIGM\AIGMCompanionTargetValidator.cs`
- `Scripts\Custom\AIGM\AIGMCompanionCapabilityGate.cs`
- `Scripts\Custom\AIGM\AIGMCompanionCapabilityKind.cs`
- `Scripts\Custom\AIGM\AIGMCompanionIntentParser.cs`
- `Scripts\Custom\AIGM\AIGMCompanionIntent.cs`
- `Scripts\Commands\AIGMScenarioCommand.cs`
- `Scripts\Commands\AIGMDumpCommand.cs`
- `Scripts\Commands\AIGMTrackingCommand.cs`

## required gameplay split
Tracking categories to support distinctly:
- TrackAnimals
- TrackMonsters
- TrackNPCs
- TrackHumanNPCs
- TrackPlayers
- TrackAll

Action gate direction:
- Monsters: track/report, pursue, attack allowed
- Animals: track/report allowed; pursue/hunt only through explicit animal-hunt lane
- NPCs: report only for now
- Human NPCs: report only for now
- Players: report only for now
- All: report only for now

## current surfaced commands
- `[tm]`
- `[ta]`
- `[tn]`
- `[th]`
- `[tp]`
- `[tall]`
- `[hm]`
- `[ha]`
- `[stoptrack]`
- `[ts]`
- `[td]`

## latest proof attempt
Latest proof artifact:
- `docs/runtime/PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_20260614_142316.md`

### commands tested
Intended live sequence:
- `[ta]`, `[td]`
- `[tm]`, `[td]`
- `[tp]`, `[td]`
- `[tall]`, `[td]`
- `[hm]`, `[md]`, `[ma]`
- `[ha]`, `[td]`

### runtime outputs summarized
A direct desktop/UI-driven runtime pass was attempted, but the visible client surface could not be trusted.
Desktop capture/OCR repeatedly resolved to an overlapping poker/UO scene instead of a clean in-game command/journal surface.

Therefore this pass does **not** claim fresh per-command runtime output for the category commands.

## category behavior results
### `[ta]` result
- **Not freshly proven in this pass**
- Expected behavior remains: `Mode=Animals`, report-only, no pursuit, no attack

### `[tm]` result
- **Not freshly proven in this pass**
- Existing indirect evidence supports monster/animal separation because older runtime artifacts show `LastReject: animal_target` in hostile-monster proof lanes

### `[tp]` result
- **Not freshly proven in this pass**
- Documented intended behavior remains: players report-only, no pursuit, no attack

### `[tall]` result
- **Not freshly proven in this pass**
- Documented intended behavior remains: report-only, no pursuit, no attack

### `[hm]` result
- **Not freshly proven in this pass as a new live run**
- Existing runtime artifacts already support the narrower statement that hostile monster hunt rejects animals:
  - `docs/runtime/PHASE58A_CLOSEST_MONSTER_HUNT_PROOF_20260613_232244.md`
  - `docs/runtime/PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_232239.md`
  - `docs/runtime/PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260614_052044.md`
- Those artifacts include `LastReject: animal_target`

### `[ha]` gate result
- **Not freshly proven in this pass**
- Documented surfaced behavior remains explicit deny gate / placeholder lane

## whether animals are rejected from MonsterHunt
- **Yes, supported by existing runtime artifacts**
- Existing proof artifacts include `LastReject: animal_target`, which supports the statement that animals are rejected from the hostile monster hunt lane
- This was not re-captured live in the latest blocked pass

## whether players/NPCs remain report-only
- **Documented intent says yes**
- Current runtime pass did not freshly prove those lanes with reliable live command output

## proof artifact path
- `docs/runtime/PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_20260614_142316.md`

## build result
- No code changed in this pass
- No build run required

## remaining blockers
- no reliable visible command/journal surface for honest desktop-driven proof capture
- category runtime matrix still lacks one canonical direct proof packet for `[ta]`, `[tm]`, `[tp]`, `[tall]`, `[hm]`, `[ha]`

## next recommended lane
- restore a deterministic visible dev-client proof surface
- rerun the full Phase58B command matrix
- once direct outputs are captured, upgrade the label from `PHASE58B-BLOCKED` to one of:
  - `PHASE58B-CATEGORY-PROOF`
  - `PHASE58B-ACTION-GATE-PROOF`
  - `PHASE58B-HM-PROOF`
