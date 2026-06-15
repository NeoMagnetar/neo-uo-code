# NeoUO Phase58 Progress Ledger

## Metadata Note
Chat metadata blocks are not the source of truth. They are session/presentation wrappers.

Repo truth is:
1. Git commits
2. `docs/NEOUO_PHASE58_PROGRESS_LEDGER.md`
3. `docs/NEOUO_PHASE58_STATUS.json`
4. `docs/runtime` proof artifacts
5. phase reports under `docs/`

## 1. Current branch
- `neo/phase56t-clean-speech-recovery`

## 2. Current HEAD
- `a4e8211f09cc861edd478d76928b0fa1c0991a09`
- HEAD subject at direct proof attempt start: `fix: add deterministic Phase58B tracking proof surface`

## 3. Clean/dirty repo status
- Repo status at direct proof attempt start: **dirty / intentionally untracked only**
- `git status --short` at preflight showed:
  - `?? Saves_BACKUP_before_AIGMCounselor_delete_20260610-115356/`
  - `?? Saves_BLOCKED_AIGMCounselor_20260610-115749/`
- No tracked file modifications were pending before this documentation-only update.

## 4. Important commits in order
Recent relevant commits, oldest to newest within current visible recovery line:
- `674ed4a2a` — `feat: restore AIGM companion speech lane`
- `386f7f598` — `feat: scaffold AIGM companion command capabilities`
- `2dca114b9` — `feat: add read-only AIGM companion awareness reports`
- `06f461c9e` — `feat: restore read-only AIGM companion tracking cycle`
- `8cc7f502d` — `feat: finalize skill-based AIGM companion tracking reports`
- `754f62544` — `feat: restore bandage-based AIGM companion healing commands`
- `2acde5c3d` — `fix: normalize AIGM companion healing command routing`
- `dac4deeb3` — `feat: scaffold Phase58A monster autonomy execution spine`
- `144928dfb` — `fix: make Phase58A monster hunt scenario deterministic`
- `a4f85d468` — `test: capture Phase58A monster pursuit runtime proof`
- `0b8498ceb` — `fix: bind Phase58A monster hunt to closest hostile target`
- `0ea1f9c5c` — `feat: restore bounded monster autonomy for AIGM companions`
- `e32bc99a3` — `chore: archive precleanup Phase58A report clutter`
- `5266f370f` — `docs: add Phase58 progress ledger and status metadata`
- `d2873ad64` — `test: capture Phase58B category tracking runtime proof`
- `a4e8211f0` — `fix: add deterministic Phase58B tracking proof surface`

## 5. Phase56/57 inherited status summary
Inherited baseline from earlier recovery work:
- companion speech lane restored
- companion command capability scaffolding restored
- read-only awareness/reporting restored
- read-only tracking cycle restored
- tracking report surfaces restored
- bandage-based healing command routing restored and normalized
- pre-Phase58 documentation/report clutter archived but preserved

## 6. Phase58A status
**Status: implemented in-tree, built, committed, partially runtime-proven**

Canonical commit:
- `0ea1f9c5c` — `feat: restore bounded monster autonomy for AIGM companions`

Canonical report:
- `docs/PHASE58A_DEV_PARITY_MONSTER_AUTONOMY_REPORT.md`

## 7. Phase58B status
**Status: deterministic proof surface exists in code, but direct proof execution is still blocked because the live shard/server context did not generate a new proof artifact**

Canonical report currently present:
- `docs/PHASE58B_TRACKING_CATEGORY_RUNTIME_REPORT.md`

Verified from repo/docs:
- surfaced short commands documented/present:
  - `[tm]`, `[ta]`, `[tn]`, `[th]`, `[tp]`, `[tall]`
  - `[hm]`, `[ha]`, `[stoptrack]`, `[ts]`, `[td]`
- proof-only deterministic commands added in code:
  - `[p58b]`
  - `[tproof]`

Direct proof attempt result:
- command run: `[p58b]`
- no new proof artifact created
- newest matching artifact remained:
  - `docs/runtime/PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_20260614_142316.md`
- most accurate current label:
  - `PHASE58B-PROOF-SURFACE-BLOCKED`

Most likely cause supported by current checks:
- live shard/server was not actually running the newly loaded command surface from commit `a4e8211f0`

## 8. Runtime proof artifacts list
Known proof/report artifacts currently present:
- `docs/PHASE58A_DEV_PARITY_MONSTER_AUTONOMY_REPORT.md`
- `docs/PHASE58A_OLDDEV_CLOSEST_HUNT_PARITY_REPORT.md`
- `docs/PHASE58B_TRACKING_CATEGORY_RUNTIME_REPORT.md`

Known runtime proof folder:
- `docs/runtime/`

Representative runtime artifacts observed in `docs/runtime/`:
- `PHASE58A_CLOSEST_MONSTER_HUNT_PROOF_20260613_232244.md`
- `PHASE58A_CLOSEST_MONSTER_HUNT_PROOF_20260614_052057.md`
- `PHASE58A_CLOSEST_MONSTER_HUNT_PROOF_20260614_052115.md`
- `PHASE58A_MONSTER_HUNT_RUNTIME_PROOF_20260613_0629.md`
- `PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_153118.md`
- `PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_160413.md`
- `PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_162201.md`
- `PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_165732.md`
- `PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_170051.md`
- `PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_170335.md`
- `PHASE58A_MONSTERHUNTENTER[AIGMDUMP COMPANIONSTATE_RUNTIME_PROOF_20260613_155955.md`
- `PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_20260614_142316.md`

## 9. Commands currently available
Verified from current repo/docs:
- Tracking/category/status GM helpers:
  - `[tm]` — track monsters
  - `[ta]` — track animals
  - `[tn]` — track NPCs
  - `[th]` — track human NPCs
  - `[tp]` — track players
  - `[tall]` — track all
  - `[ts]` — tracking status
  - `[td]` — tracking dump
  - `[stoptrack]` — stop tracking and hunt
- Hunt helpers:
  - `[hm]` — start monster hunt
  - `[ha]` — animal hunt placeholder / denied lane
- Proof helpers present in code:
  - `[p58b]` — deterministic Phase58B category proof writer
  - `[tproof]` — alias for deterministic Phase58B category proof writer

## 10. What is proven
Proven by repo docs/commits/build state:
- Phase58A bounded monster autonomy is present in-tree.
- Tracking categories and short tracking commands exist in-tree.
- Deterministic server-side Phase58B proof surface exists in code via `[p58b]` / `[tproof]`.
- Release build passed for the proof-surface implementation with `0 Error(s)`.

Proven by existing runtime artifacts explicitly referenced in docs:
- hostile MonsterHunt rejects animals via `animal_target` in older runtime proofs

## 11. What is not proven
Not yet canonical/proven broadly enough for source-of-truth status:
- fresh direct Phase58B deterministic proof artifact generated through `[p58b]` or `[tproof]`
- full category runtime matrix for all surfaced commands consolidated into one success-state proof packet
- direct fresh runtime proof that players/NPCs remain report-only during the current surfaced command matrix
- direct fresh runtime proof that TrackAll remains report-only
- direct fresh runtime proof that HuntAnimals remains blocked through the deterministic proof command path

## 12. What is blocked
Current explicit blockers:
- live shard/server context did not generate a new deterministic proof artifact after `[p58b]` execution attempt
- proof command exists in source, but the running shard likely was not restarted/reloaded onto commit `a4e8211f0`
- player proof remains dependent on nearby real player observation unless a safe native test pattern is later introduced

## 13. What is next
Immediate next action:
- ensure the dev shard/server is actually running the code from commit `a4e8211f0`
- restart/reload the server if needed
- rerun `[p58b]` or `[tproof]`
- read the newly generated proof artifact
- then update canonical Phase58 metadata from that direct server-side report

## 14. Safety gates still enforced
Safety boundaries still enforced by repo truth:
- monster-only attack lane
- no player pursuit or attack
- no owner/companion targeting
- no vendor / civilian / human NPC attack lane
- no animal attack in monster-hunt lane
- no broad coordinate travel restoration
- no freeform travel reactivation
- self-bandage uses native `BandageContext`
- self-cure uses native `BaseCurePotion.Drink(...)`
- combat assignment only after explicit validation
- animal hunt remains explicitly denied in current surfaced lane

## 15. Files/folders that are intentionally untracked
- `Saves_BACKUP_before_AIGMCounselor_delete_20260610-115356/`
- `Saves_BLOCKED_AIGMCounselor_20260610-115749/`

## 16. Phase58C status
**Status: implemented in-tree, built, pending commit/proof consolidation**

Current untracked coordinate movement WIP intentionally remains untouched by Phase58D:
- `Scripts/Commands/AIGMCoordinateMovementCommand.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCoordinateMovementService.cs`

## 17. Phase58D status
**Status: implemented in-tree, built, source-proofed, live-proofed on the dev shard**

Canonical report:
- `docs/PHASE58D_PARTY_SPEECH_RESTORATION_REPORT.md`

Proof command surface:
- `[p58speech]`
- `[pspeech]`
- `[sdump]`

Source-generated proof packet:
- `docs/runtime/PHASE58D_PARTY_SPEECH_RUNTIME_PROOF_20260615_112637.md`

Canonical live proof packet:
- `docs/runtime/PHASE58D_PARTY_SPEECH_RUNTIME_PROOF_20260615_205353.md`

Implemented behavior:
- owner group speech builds a party listener context instead of selecting only the closest companion
- direct named commands select one primary companion and relay context silently to siblings
- group conversation can select up to three in-character responders
- companion-to-companion dialogue allows one bounded follow-up
- second-hop companion dialogue is blocked with `PHASE58D-ECHO-BLOCKED`
- UMG bridge requests include listener set, selected responders, suppressed reasons, state context, parsed intent, and safety posture

Build result:
- `dotnet build .\ServUO.sln -c Release`
- 0 errors
- 0 warnings from the explicit preflight build
- server startup script compilation still reported the known unreachable-code warnings

Live runtime proof status:
- ServUO started from the Phase58D repo and listened on `127.0.0.1:2595`
- ClassicUO dev login entered world as `NeoMagnetar`
- `[p58speech]` registered and wrote a runtime proof artifact
- `[sdump]` registered and displayed the latest context dump in-game
- first proof run found Dakeyras and Danyal only
- Dardalion was spawned and claimed through existing live command paths (`[Dardalion`, then `dardalion follow me`)
- final proof run found Dakeyras, Danyal, and Dardalion
- final proof labels:
  - `PHASE58D-PARTY-HEARING-PROOF`
  - `PHASE58D-GROUP-DIALOGUE-PROOF`
  - `PHASE58D-COMPANION-DIALOGUE-PROOF`
  - `PHASE58D-ECHO-BLOCKED`

Live proof observations:
- direct named command `dak track monsters` selected Dakeyras only and suppressed Danyal/Dardalion as context-only
- group owner message selected Dakeyras, Danyal, and Dardalion
- companion-to-companion exchange selected one follow-up responder and suppressed the source/extra responder
- chain-depth echo test selected no responders and returned `PHASE58D-ECHO-BLOCKED`
- `[sdump]` showed mode, owner, text, listener set, selected set, suppressed set, reasons, chain depth, and state

## 18. Phase58D useful-hardening recovery checkpoint
**Status: useful live-speech hardening diff inspected, built, committed, and post-commit live-proofed**

Classification:
- `USEFUL-HARDENING`

Files changed:
- `Scripts/Custom/AIGM/AIGMBridgeClient.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCommandBoundary.cs`
- `Scripts/Custom/AIGM/AIGMCompanionDialogueBus.cs`
- `Scripts/Custom/AIGM/AIGMCompanionDialogueEvent.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs`
- `Scripts/Custom/AIGM/AIGMCompanionPartySpeechContext.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSpeechQueue.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSpeechRequest.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTurnCoordinator.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs`

Build result:
- `dotnet build .\ServUO.sln -c Release`
- 0 errors
- 0 warnings

Recovery notes:
- ServUO PID `4696` was stopped before the build.
- Coordinate movement WIP files remain intentionally unstaged and untouched.
- Save-backup folders remain intentionally unstaged and untouched.
- Hardening commit: `0d222f621ec8c22fa3640fe9433b5e49091e78d9`
- ServUO was restarted from the FullIntegration repo after the hardening commit.
- Live proof after this hardening commit completed through `[p58speech]` and `[sdump]`.
- Fresh proof artifact: `docs/runtime/PHASE58D_PARTY_SPEECH_RUNTIME_PROOF_20260615_223822.md`
- Fresh proof found Dakeyras, Danyal, and Dardalion.
- Fresh proof selected Dakeyras only for direct named routing.
- Fresh proof selected all three companions for group owner speech.
- Fresh proof selected one companion follow-up and blocked second-hop echo dialogue.
