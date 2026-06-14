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
- `e32bc99a3c99a39e9d617dfca15777f3e2191f24`
- HEAD subject: `chore: archive precleanup Phase58A report clutter`

## 3. Clean/dirty repo status
- Repo status: **dirty / intentionally untracked only**
- `git status --short` at ledger time shows:
  - `?? Saves_BACKUP_before_AIGMCounselor_delete_20260610-115356/`
  - `?? Saves_BLOCKED_AIGMCounselor_20260610-115749/`
- No tracked file modifications are pending at ledger time.

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

## 5. Phase56/57 inherited status summary
Inherited baseline from earlier recovery work:
- companion speech lane restored
- companion command capability scaffolding restored
- read-only awareness/reporting restored
- read-only tracking cycle restored
- tracking report surfaces restored
- bandage-based healing command routing restored and normalized
- pre-Phase58 documentation/report clutter archived but preserved

Practical meaning:
- By the start of stable Phase58 work, the repo already had speech, read-only tracking, and explicit healing-routing foundations.
- Phase58 builds on those surfaces rather than reintroducing the entire old Dev freeform lane.

## 6. Phase58A status
**Status: implemented in-tree, built, committed, partially runtime-proven**

Canonical commit:
- `0ea1f9c5c` — `feat: restore bounded monster autonomy for AIGM companions`

Canonical report:
- `docs/PHASE58A_DEV_PARITY_MONSTER_AUTONOMY_REPORT.md`

Phase58A verified implementation surfaces:
- bounded execution spine exists
- target validator exists
- combat controller exists
- door service exists
- self-sustain service exists
- monster-only pursuit/engagement lane exists
- cure potion path was verified against native `BaseCurePotion.Drink(...)`
- build passed after clearing a live file lock

Phase58A routing/commands confirmed by report:
- owner/natural speech includes:
  - `track monsters`
  - `start tracking monsters`
  - `hunt monsters`
  - `attack monsters`
  - `clear monsters`
  - `start hunting`
  - `hunt closest monster`
  - `stop hunting`
  - `stop tracking`
- GM helper command surface present:
  - `[tm]`, `[ta]`, `[tn]`, `[th]`, `[tp]`, `[tall]`, `[hm]`, `[ha]`, `[stoptrack]`, `[ts]`, `[td]`

## 7. Phase58B status
**Status: documentation and first-surface wiring evidence exist; canonical implementation commit not yet established in this ledger**

Canonical report currently present:
- `docs/PHASE58B_TRACKING_CATEGORY_RUNTIME_REPORT.md`

Verified from report contents:
- tracking categories identified as target split:
  - Animals
  - Monsters
  - NPCs
  - HumanNPCs
  - Players
  - All
- short command plan documented:
  - `[tm]`, `[ta]`, `[tn]`, `[th]`, `[tp]`, `[tall]`
  - `[hm]`, `[ha]`, `[stoptrack]`, `[ts]`, `[td]`
- action gate direction documented:
  - Monsters: track/report + pursue/attack lane
  - Animals: track/report only unless explicit animal-hunt lane
  - NPCs/HumanNPCs/Players/All: report-only for now

Important repo-truth caveat:
- The report text itself still says `build result: Pending Phase58B implementation.`
- However, current repo state and Phase58A report confirm the tracking categories and GM short commands are already present in-tree now.
- Therefore, for canonical status purposes, **Phase58B is best described as “surface wiring present, runtime-category proof incomplete, canonical finish point not yet consolidated.”**

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

Archived supporting reports:
- `archive/reports/2026-06-14-phase58a-precleanup/`

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
- Owner/natural phrases documented for Phase58A monster lane:
  - `track monsters`
  - `start tracking monsters`
  - `hunt monsters`
  - `attack monsters`
  - `clear monsters`
  - `start hunting`
  - `hunt closest monster`
  - `stop hunting`
  - `stop tracking`

## 10. What is proven
Proven by repo docs/commits/build state:
- Phase58A bounded monster autonomy is present in-tree.
- Execution spine, target validator, combat controller, door service, and self-sustain service exist.
- Cure potion lane was verified against native ServUO potion mechanics (`BaseCurePotion.Drink`).
- Build succeeded for the committed Phase58A lane.
- Tracking categories and short tracking commands now exist in-tree.

Proven by runtime artifacts explicitly referenced in docs:
- monster hunt scenario key recognized
- valid nearby monster selected
- pursuit phase entered
- movement trace present
- hostile-monster validator behavior present
- closest-hostile targeting proof artifacts exist

## 11. What is not proven
Not yet canonical/proven broadly enough for source-of-truth status:
- full long-running autonomous hunt loop under varied live conditions
- broad reacquire/clear-completion proof across multiple monster sequences
- robust door-open proof in multiple door/pathing scenes
- self-bandage proof under sustained live combat conditions as a completed end-to-end behavior packet
- self-cure proof under live poisoned hunt conditions as a completed end-to-end behavior packet
- Phase58B category runtime proof matrix for all short commands (`[ta]`, `[tm]`, `[tp]`, `[tall]`, `[hm]`, `[ha]`) captured and consolidated into one canonical completion state

## 12. What is blocked
Current explicit blockers:
- runtime proof breadth, not core compile/build viability
- Phase58B category runtime proof consolidation is still incomplete
- repo truth is now centralized by this ledger, but older docs still reflect intermediate checkpoints and may not all agree on final wording

Historical blocker already cleared:
- build lock from live `ServUO Server` using `Ultima.dll` was resolved during Phase58A validation

## 13. What is next
Immediate next action:
- treat this ledger and `docs/NEOUO_PHASE58_STATUS.json` as canonical source of truth before any further gameplay work

Recommended next engineering step after ledger pass:
- perform a deliberate Phase58B runtime proof sweep and write/refresh a canonical Phase58B runtime completion report covering:
  - `[ta]` + `[td]`
  - `[tm]` + `[td]`
  - `[tp]` + `[td]`
  - `[tall]` + `[td]`
  - `[hm]` + monster dump/state confirmation
  - `[ha]` + explicit denied-lane confirmation

After that:
- decide whether Phase58B is complete enough to mark stable, or whether a Phase58C proof/consolidation pass is needed

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
