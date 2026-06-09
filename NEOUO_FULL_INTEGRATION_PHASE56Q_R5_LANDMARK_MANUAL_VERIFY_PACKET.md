# NEOUO FULL INTEGRATION PHASE56Q R5 LANDMARK MANUAL VERIFY PACKET

## Purpose
This packet is for a human operator / GM to manually verify the current Tier 1 landmark candidates before any seed data is committed into the NeoUO runtime.

This packet does **not** authorize code changes.
This packet does **not** add seed data.
This packet does **not** perform in-game verification.

## Verification Status Rules
Current candidate status:
- All candidates in this packet are **Tier 1**
- All coordinates are **repo-derived**
- None are manually verified yet
- None are runtime adapter validated yet
- None are approved for seed commit yet

A candidate becomes seed-eligible only after:
1. manual verification is completed,
2. facet and coordinate are confirmed or corrected,
3. passability / nearby standable tile is recorded,
4. semantic kind is confirmed,
5. safe/danger/route-node flags are reviewed,
6. later runtime adapter validation path is accepted.

## Global Operator Instructions
For every candidate below:
1. Use GM/operator teleport or Go tooling to reach the coordinate.
2. Confirm the facet is correct.
3. Visually confirm the intended landmark identity.
4. Confirm the exact tile is standable.
5. If the exact tile is blocked, record the nearest safe standable correction.
6. Confirm the area/town/dungeon context matches expectations.
7. Confirm whether the landmark is useful as a route node, safe point, danger point, or semantic-only marker.
8. Record any corrections before seed data is approved.
9. If helpful, capture screenshot or operator notes.

## Runtime Validation Reminder
Later, after manual verification and once seed data exists, runtime adapter checks should confirm:
- `ProbePoint` does not fail unexpectedly
- `GetRegionAt` returns useful area info where available
- snapshot service can compose position / probe / area info
- no live object references are required
- no movement is required for validation

---

# Candidate 1
## Candidate Summary
- CandidateId: `LMK-NH-BANK-T1`
- DisplayName: `New Haven Bank (bank-adjacent)`
- Kind: `Bank`
- FacetName: `Trammel`
- X/Y/Z: `3506 / 2570 / 14`
- AreaName: `Haven / New Haven`
- AreaKey: `haven_new_haven`
- Source file: `Data/Locations/trammel.xml` + `Spawns/trammel.xml`
- Source context: `Haven -> New Haven x=3506 y=2570 z=14`; banker/minter spawn near `X=3482 Y=2570 Width=4 Height=4`
- Confidence tier: `Tier 1`
- Proposed role: `route node`, `safe point`, `semantic landmark`
- Proposed flags:
  - IsRouteNode: `true`
  - IsSafePoint: `true`
  - IsDangerPoint: `false`
- RiskLevel: `low`
- Notes: Best starter bank candidate. Exact arrival tile vs teller-facing tile still needs human verification.

## Manual Verification Checklist
- [ ] verify facet is `Trammel`
- [ ] teleport / go to `3506,2570,14`
- [ ] visually confirm this is the intended New Haven bank-adjacent area
- [ ] confirm exact coordinate is not inside a wall/object
- [ ] confirm the exact tile is standable
- [ ] if blocked, record nearest standable bank-adjacent tile
- [ ] confirm banker/minter presence is nearby and supports the semantic label
- [ ] confirm area context is Haven / New Haven
- [ ] confirm route usefulness as a starter destination anchor
- [ ] confirm safe-point classification
- [ ] record corrected coordinate if needed
- [ ] capture screenshot / notes if available

## Runtime Verification Checklist
- [ ] `ProbePoint` succeeds on final chosen tile
- [ ] `GetRegionAt` returns useful area information if available
- [ ] snapshot output is enough to identify surrounding context
- [ ] no live object references are required
- [ ] no movement is required

## Seed Eligibility Decision
- verified: `yes / no`
- corrected X/Y/Z: `_____ / _____ / _____`
- final Kind: `_____`
- final IsRouteNode: `yes / no`
- final IsSafePoint: `yes / no`
- final IsDangerPoint: `yes / no`
- final RiskLevel: `_____`
- verification notes: `________________________________________`
- approved for seed commit: `yes / no`

---

# Candidate 2
## Candidate Summary
- CandidateId: `LMK-MAG-BANK-FEL-T1`
- DisplayName: `Magincia Bank`
- Kind: `Bank`
- FacetName: `Felucca`
- X/Y/Z: `3730 / 2161 / 20`
- AreaName: `Magincia`
- AreaKey: `magincia`
- Source file: `Data/Locations/felucca.xml`
- Source context: `Magincia -> Bank x=3730 y=2161 z=20`
- Confidence tier: `Tier 1`
- Proposed role: `route node`, `safe point`, `semantic landmark`
- Proposed flags:
  - IsRouteNode: `true`
  - IsSafePoint: `true`
  - IsDangerPoint: `false`
- RiskLevel: `low`
- Notes: Strong explicit bank candidate from locations XML.

## Manual Verification Checklist
- [ ] verify facet is `Felucca`
- [ ] teleport / go to `3730,2161,20`
- [ ] visually confirm this is the intended Magincia bank area
- [ ] confirm exact coordinate is not inside a wall/object
- [ ] confirm exact tile is standable
- [ ] if blocked, record nearest standable bank-adjacent tile
- [ ] confirm area context is Magincia
- [ ] confirm route usefulness as city commerce anchor
- [ ] confirm safe-point classification
- [ ] record corrected coordinate if needed
- [ ] capture screenshot / notes if available

## Runtime Verification Checklist
- [ ] `ProbePoint` succeeds on final chosen tile
- [ ] `GetRegionAt` returns useful area information if available
- [ ] snapshot output is enough to identify surrounding context
- [ ] no live object references are required
- [ ] no movement is required

## Seed Eligibility Decision
- verified: `yes / no`
- corrected X/Y/Z: `_____ / _____ / _____`
- final Kind: `_____`
- final IsRouteNode: `yes / no`
- final IsSafePoint: `yes / no`
- final IsDangerPoint: `yes / no`
- final RiskLevel: `_____`
- verification notes: `________________________________________`
- approved for seed commit: `yes / no`

---

# Candidate 3
## Candidate Summary
- CandidateId: `LMK-BRIT-MOONGATE-FEL-T1`
- DisplayName: `Britain Moongate`
- Kind: `Moongate`
- FacetName: `Felucca`
- X/Y/Z: `1336 / 1997 / unknown`
- AreaName: `Moongates / Britain`
- AreaKey: `britain_moongate`
- Source file: `Data/Regions.xml`
- Source context: `Moongates` guarded region, Britain rect `x=1330 y=1991 width=13 height=13`
- Confidence tier: `Tier 1`
- Proposed role: `route node`, `safe point`, `semantic landmark`
- Proposed flags:
  - IsRouteNode: `true`
  - IsSafePoint: `true`
  - IsDangerPoint: `false`
- RiskLevel: `low`
- Notes: Rectangle-center candidate only. Exact standable tile and Z must be chosen during manual verification.

## Manual Verification Checklist
- [ ] verify facet is `Felucca`
- [ ] go to the candidate center area near `1336,1997`
- [ ] visually confirm Britain moongate identity
- [ ] identify the best standable tile in the moongate plaza / region
- [ ] record exact corrected X/Y/Z for final seed use
- [ ] confirm the tile is not inside gate art / obstruction
- [ ] confirm area context matches Britain moongate region
- [ ] confirm route usefulness as major travel anchor
- [ ] confirm safe-point classification
- [ ] capture screenshot / notes if available

## Runtime Verification Checklist
- [ ] `ProbePoint` succeeds on final chosen tile
- [ ] `GetRegionAt` returns useful area information if available
- [ ] snapshot output supports moongate-area recognition
- [ ] no live object references are required
- [ ] no movement is required

## Seed Eligibility Decision
- verified: `yes / no`
- corrected X/Y/Z: `_____ / _____ / _____`
- final Kind: `_____`
- final IsRouteNode: `yes / no`
- final IsSafePoint: `yes / no`
- final IsDangerPoint: `yes / no`
- final RiskLevel: `_____`
- verification notes: `________________________________________`
- approved for seed commit: `yes / no`

---

# Candidate 4
## Candidate Summary
- CandidateId: `LMK-MOONGLOW-MOONGATE-FEL-T1`
- DisplayName: `Moonglow Moongate`
- Kind: `Moongate`
- FacetName: `Felucca`
- X/Y/Z: `4467 / 1284 / unknown`
- AreaName: `Moongates / Moonglow`
- AreaKey: `moonglow_moongate`
- Source file: `Data/Regions.xml`
- Source context: `Moongates` guarded region, Moonglow rect `x=4459 y=1276 width=16 height=16`
- Confidence tier: `Tier 1`
- Proposed role: `route node`, `safe point`, `semantic landmark`
- Proposed flags:
  - IsRouteNode: `true`
  - IsSafePoint: `true`
  - IsDangerPoint: `false`
- RiskLevel: `low`
- Notes: Rectangle-center candidate only. Exact standable tile and Z must be chosen during manual verification.

## Manual Verification Checklist
- [ ] verify facet is `Felucca`
- [ ] go to the candidate center area near `4467,1284`
- [ ] visually confirm Moonglow moongate identity
- [ ] identify the best standable tile in the moongate plaza / region
- [ ] record exact corrected X/Y/Z for final seed use
- [ ] confirm the tile is not inside gate art / obstruction
- [ ] confirm area context matches Moonglow moongate region
- [ ] confirm route usefulness as major travel anchor
- [ ] confirm safe-point classification
- [ ] capture screenshot / notes if available

## Runtime Verification Checklist
- [ ] `ProbePoint` succeeds on final chosen tile
- [ ] `GetRegionAt` returns useful area information if available
- [ ] snapshot output supports moongate-area recognition
- [ ] no live object references are required
- [ ] no movement is required

## Seed Eligibility Decision
- verified: `yes / no`
- corrected X/Y/Z: `_____ / _____ / _____`
- final Kind: `_____`
- final IsRouteNode: `yes / no`
- final IsSafePoint: `yes / no`
- final IsDangerPoint: `yes / no`
- final RiskLevel: `_____`
- verification notes: `________________________________________`
- approved for seed commit: `yes / no`

---

# Candidate 5
## Candidate Summary
- CandidateId: `LMK-SPIRITUALITY-SHRINE-FEL-T1`
- DisplayName: `Shrine of Spirituality`
- Kind: `Shrine / SafePoint`
- FacetName: `Felucca`
- X/Y/Z: `1589 / 2485 / 5`
- AreaName: `Shrines / Spirituality`
- AreaKey: `shrine_spirituality`
- Source file: `Data/Locations/felucca.xml` + `Data/Decoration/Britannia/shrines.cfg`
- Source context: locations child `Spirituality x=1589 y=2485 z=5`; decor ankh at `1592 2489 20` nearby
- Confidence tier: `Tier 1`
- Proposed role: `safe point`, `semantic landmark`
- Proposed flags:
  - IsRouteNode: `false`
  - IsSafePoint: `true`
  - IsDangerPoint: `false`
- RiskLevel: `low`
- Notes: Strong safe-point candidate with semantic and decor corroboration. Final landing tile may need slight adjustment.

## Manual Verification Checklist
- [ ] verify facet is `Felucca`
- [ ] teleport / go to `1589,2485,5`
- [ ] visually confirm shrine identity
- [ ] confirm relationship between location node and ankh area
- [ ] confirm exact coordinate is not inside deco / blocked space
- [ ] if blocked, record nearest safe shrine-adjacent standable tile
- [ ] confirm area context is the Shrine of Spirituality
- [ ] confirm safe-point classification
- [ ] confirm whether route-node behavior is unnecessary
- [ ] capture screenshot / notes if available

## Runtime Verification Checklist
- [ ] `ProbePoint` succeeds on final chosen tile
- [ ] `GetRegionAt` returns useful area information if available
- [ ] snapshot output supports shrine-area recognition
- [ ] no live object references are required
- [ ] no movement is required

## Seed Eligibility Decision
- verified: `yes / no`
- corrected X/Y/Z: `_____ / _____ / _____`
- final Kind: `_____`
- final IsRouteNode: `yes / no`
- final IsSafePoint: `yes / no`
- final IsDangerPoint: `yes / no`
- final RiskLevel: `_____`
- verification notes: `________________________________________`
- approved for seed commit: `yes / no`

---

# Candidate 6
## Candidate Summary
- CandidateId: `LMK-SHAME-ENTRANCE-FEL-T1`
- DisplayName: `Shame Entrance`
- Kind: `DungeonEntrance`
- FacetName: `Felucca`
- X/Y/Z: `514 / 1561 / 0`
- AreaName: `Shame`
- AreaKey: `shame`
- Source file: `Data/Locations/felucca.xml`
- Source context: `Shame -> Entrance x=514 y=1561 z=0`
- Confidence tier: `Tier 1`
- Proposed role: `danger point`, `semantic landmark`
- Proposed flags:
  - IsRouteNode: `false`
  - IsSafePoint: `false`
  - IsDangerPoint: `true`
- RiskLevel: `medium`
- Notes: Clean explicit dungeon entrance candidate.

## Manual Verification Checklist
- [ ] verify facet is `Felucca`
- [ ] teleport / go to `514,1561,0`
- [ ] visually confirm this is the intended Shame entrance area
- [ ] confirm exact tile is standable and not inside geometry
- [ ] if blocked, record nearest standable entrance-adjacent tile
- [ ] confirm the location is a useful destination anchor rather than a route node
- [ ] confirm danger-point classification is appropriate
- [ ] confirm area context is Shame entrance rather than an internal dungeon point
- [ ] capture screenshot / notes if available

## Runtime Verification Checklist
- [ ] `ProbePoint` succeeds on final chosen tile
- [ ] `GetRegionAt` returns useful area information if available
- [ ] snapshot output supports dungeon-entrance recognition
- [ ] no live object references are required
- [ ] no movement is required

## Seed Eligibility Decision
- verified: `yes / no`
- corrected X/Y/Z: `_____ / _____ / _____`
- final Kind: `_____`
- final IsRouteNode: `yes / no`
- final IsSafePoint: `yes / no`
- final IsDangerPoint: `yes / no`
- final RiskLevel: `_____`
- verification notes: `________________________________________`
- approved for seed commit: `yes / no`

---

## Final Seed Admission Rules
A candidate may be committed into runtime seed data only if all of the following are true:
1. repo-derived source exists
2. manual verification is completed
3. coordinate and facet are confirmed or corrected
4. passability or nearby standable tile is recorded
5. semantic kind is confirmed
6. no shard-specific ambiguity remains
7. safe/danger classification is deliberate and documented
8. future adapter validation path is clear
9. candidate remains within the intentionally tiny first seed scope
10. operator notes are preserved for later audit

## Missing / Excluded Categories
- Stable: intentionally excluded; not cleanly verified from repo-only inspection
- Healer-first-class seed: deferred until a cleaner operator-confirmed town healer candidate exists
- Road/crossroads test node: deferred unless explicitly needed after manual packet review

## Next Phase Recommendation
### Primary
- `Phase 56Q-R5-LANDMARK-MANUAL-VERIFY-PACKET-P — Commit Landmark Verification Packet`

### Alternate
- `Phase 56Q-R5-LANDMARK-SEED-MINIMAL-CANDIDATES — Add First Tiny Seed Set`

Do **not** use the alternate until the packet is reviewed and the manual verification strategy is accepted.
