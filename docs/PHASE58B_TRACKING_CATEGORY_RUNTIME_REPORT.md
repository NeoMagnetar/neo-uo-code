# Phase58B Tracking Category Runtime Report

## status
Current canonical status after proof-surface stabilization:
- Branch at stabilization time: `neo/phase56t-clean-speech-recovery`
- HEAD before stabilization commit: `d2873ad641e56b93060f03f10946745d370bbd30`
- Current lane result: deterministic server-side proof surface added
- Latest recommended invocation: `[p58b]` or `[tproof]`

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
- `Scripts\Commands\AIGMTrackingCommand.cs`
- `Scripts\Commands\AIGMPhase58BProofCommand.cs`

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
Normal live commands remain unchanged:
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

Deterministic proof-only commands added:
- `[p58b]`
- `[tproof]`

## proof-surface design
The new proof command:
- runs server-side without OCR or desktop capture
- writes directly to `docs/runtime/PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_<timestamp>.md`
- prints one compact client-facing line with proof path and final label
- may create **proof-only** local fixtures for animal/monster category checks
- does **not** change normal tracking command behavior
- does **not** enable player attack, NPC attack, human NPC attack, or animal hunting

Proof output includes:
- branch
- HEAD
- command used
- selected companion
- category tested
- accepted target
- rejected candidates and reasons
- pursuit allowed true/false
- attack allowed true/false
- movement result if applicable
- combat result if applicable
- final label

## latest proof attempt
Latest runtime attempt remains the blocked one:
- `docs/runtime/PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_20260614_142316.md`
- label: `PHASE58B-BLOCKED`

That artifact remains the honest record of the prior desktop/OCR failure.

## proof stabilization result
This stabilization pass does **not** claim category proof completion by itself.
It establishes the deterministic proof surface required to run the next honest Phase58B category matrix without screenshot ambiguity.

## category behavior policy targets
### animals
- track/report yes
- pursuit no
- attack no

### monsters
- track/report yes
- hunt/pursue/attack yes through MonsterHunt lane

### players
- track/report yes
- pursuit no
- attack no

### NPCs / human NPCs
- track/report yes
- pursuit no
- attack no

### all
- report only
- pursuit no
- attack no

### HuntAnimals
- blocked/gated

## whether animals are rejected from MonsterHunt
- Existing runtime artifacts already support this:
  - `docs/runtime/PHASE58A_CLOSEST_MONSTER_HUNT_PROOF_20260613_232244.md`
  - `docs/runtime/PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_232239.md`
  - `docs/runtime/PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260614_052044.md`
- Those include `LastReject: animal_target`

## build result
Build command run because code changed:
- `dotnet build .\ServUO.sln -c Release`

Outcome:
- `0 Error(s)`
- `15 Warning(s)`

Warnings were pre-existing unreachable-code warnings in unrelated files.

## remaining blockers
- the new proof command still needs to be executed live to generate a fresh deterministic Phase58B proof artifact
- player proof remains limited to nearby real player observation unless a safe native proof pattern exists; no fake player creation was added

## next recommended lane
1. run `[p58b]` or `[tproof]` in the dev shard
2. inspect the generated `docs/runtime/PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_<timestamp>.md`
3. update canonical metadata from that artifact
4. only then promote the label to one of:
   - `PHASE58B-CATEGORY-PROOF`
   - `PHASE58B-ACTION-GATE-PROOF`
   - `PHASE58B-HM-PROOF`
