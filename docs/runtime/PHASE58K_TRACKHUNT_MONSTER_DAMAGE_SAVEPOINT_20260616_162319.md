# Phase58K TrackHunt Monster Damage Savepoint

Timestamp: 2026-06-16 16:23:19 local / 2026-06-17 runtime log date

ServUO PID: 7496

Proof commands:

- `[AIGMTrackHuntProof trackhunt`
- User also confirmed manual ClassicUO journal proof for TrackHunt damage.

Damage proof:

- `Target hits 70->60`
- `Target hits 69->59`
- Fresh log evidence:
  - `TRACKHUNT_PROOF_DAMAGE companion=57 target=422 beforeHits=70 afterHits=60 pulse=2`
  - `TRACKHUNT_PROOF_RESULT mode=trackhunt result=pass reason=target_damaged companion=57 target=422 beforeHits=70 afterHits=60 pulses=2 combatant=422`
  - `TRACKHUNT_PROOF_DAMAGE companion=402 target=408 beforeHits=69 afterHits=59 pulse=5`
  - `TRACKHUNT_PROOF_RESULT result=pass reason=target_damaged companion=402 target=408 beforeHits=69 afterHits=59 pulses=5 combatant=408`

Confirmation:

- TrackHunt found valid monster targets.
- Companions pursued targets.
- Monsters took damage.
- User observed a lizardman corpse after proof.
- Native/Dev-parity kill path now works enough for savepoint.

Safety:

- No player/NPC aggression included in this proof.
- Coordinate movement WIP remained untouched and excluded.
- Save backups remained untouched and excluded.

Known next work:

- Fresh regression test for NPC/player no-attack commands.
- Refine pursuit behavior if needed.
- Optional cleanup or removal of GM proof harness after the feature stabilizes.
