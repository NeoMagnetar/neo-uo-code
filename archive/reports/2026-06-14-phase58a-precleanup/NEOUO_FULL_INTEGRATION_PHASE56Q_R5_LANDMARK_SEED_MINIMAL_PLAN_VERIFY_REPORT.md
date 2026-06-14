# NEOUO FULL INTEGRATION PHASE56Q R5 LANDMARK SEED MINIMAL PLAN VERIFY REPORT

## Selected Agent / Session Identity
- Agent/session: `agent:ultima-online:main`
- Selected agent: `ultima-online`
- Selected session: `main`
- Active subagent selected: no
- Active subagent used: no
- Historical child subagent metadata observed: yes
- Historical child subagent metadata treatment: warning-only per lane instruction

## Workspace / Repo State
- Workspace path: `C:\.openclaw\workspace-ultima-online`
- Target repo path: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- Branch: `neo/staging-aigm`
- HEAD: `498bef49ef831ccdb5649b10af6a2153cfe5f223`
- Latest commit: `498bef49e feat: add no-op UMG navigation landmark registry`
- Git status: repo contains unrelated untracked report files; no code or seed mutation was performed in this phase

## Baseline Build Result
Command:
- `dotnet build .\ServUO.sln -v:minimal`

Result:
- Build succeeded
- Errors: `0`
- Warnings: `0`
- Warning files: none

## Files / Reports Inspected
Reference report:
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_LANDMARK_SEED_PLAN_REPORT.md`

Primary landmark stack:
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmarkKind.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmark.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmarkQuery.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmarkResult.cs`
- `Scripts/Custom/AIGM/Navigation/IUMGNavigationLandmarkRegistry.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationNoOpLandmarkRegistry.cs`

Primary source files inspected:
- `Data/Locations/felucca.xml`
- `Data/Locations/trammel.xml`
- `Data/Regions.xml`
- `Data/teleporters.csv`
- `Spawns/trammel.xml`
- `Spawns/felucca.xml`
- `Data/Decoration/Britannia/shrines.cfg`
- `Data/Decoration/Britannia/britain.cfg`
- `Data/Decoration/Britannia/moonglow.cfg`
- `Data/Decoration/Trammel/NewHaven.cfg`

## Source Inventory Summary
### Authority ranking extracted from the seed plan
1. `Data/Locations/*.xml`
2. `Data/Regions.xml`
3. `Data/teleporters.csv`
4. `Spawns/*.xml`
5. `Data/Decoration/*.cfg`

### Confidence tiers applied
- **Tier 0**: semantic placeholder only, no coordinate
- **Tier 1**: repo-derived coordinate, untested
- **Tier 2**: repo-derived and manually verified in-game
- **Tier 3**: repo-derived, manually verified, and runtime adapter validates passability/area
- **Tier X**: external/general UO coordinate only; do not commit without verification

### Repo-backed observations that matter for first seeds
- `Data/Locations/*.xml` is the cleanest semantic source for named towns, town subareas, dungeon entrances, shrine nodes, and some bank nodes.
- `Data/Regions.xml` adds area envelope confirmation and explicit moongate guarded rectangles.
- `Spawns/trammel.xml` gives strong corroboration for New Haven banker/minter presence near the `Data/Locations/trammel.xml` New Haven point.
- `Data/Decoration/Britannia/shrines.cfg` gives explicit ankh placements that corroborate shrine coordinates from `Data/Locations`.
- Stable candidates were not cleanly surfaced from the inspected repo files with enough confidence to recommend first commit inclusion.

## Candidate Landmark Table
Target kept intentionally tiny: **6 candidates**.

| CandidateId | DisplayName | Kind | FacetName | X | Y | Z | AreaName | AreaKey | Source file | Source record / context | Source type | Confidence | IsRouteNode | IsSafePoint | IsDangerPoint | RiskLevel | Verification status | Notes |
|---|---|---|---|---:|---:|---:|---|---|---|---|---|---|---|---|---|---|---|---|
| LMK-NH-BANK-T1 | New Haven Bank (bank-adjacent) | Bank | Trammel | 3506 | 2570 | 14 | Haven / New Haven | haven_new_haven | `Data/Locations/trammel.xml` + `Spawns/trammel.xml` | `Haven -> New Haven x=3506 y=2570 z=14`; banker/minter spawn near `X=3482 Y=2570 Width=4 Height=4` | locations xml + spawn xml corroboration | Tier 1 | true | true | false | low | repo-derived, not manually verified | Best starter bank candidate. Semantically clean from location file; NPC corroboration from spawn file. Exact arrival tile vs teller tile still needs human verification. |
| LMK-MAG-BANK-FEL-T1 | Magincia Bank | Bank | Felucca | 3730 | 2161 | 20 | Magincia | magincia | `Data/Locations/felucca.xml` | `Magincia -> Bank x=3730 y=2161 z=20` | locations xml | Tier 1 | true | true | false | low | repo-derived, not manually verified | Strong clean bank candidate with explicit semantic label. Good first non-starter city bank. |
| LMK-BRIT-MOONGATE-FEL-T1 | Britain Moongate | Moongate | Felucca | 1336 | 1997 | unknown | Moongates / Britain | britain_moongate | `Data/Regions.xml` | `Moongates` guarded region, Britain rect `x=1330 y=1991 width=13 height=13` | regions xml | Tier 1 | true | true | false | low | repo-derived rectangle center only | Exact standable Z/tile is not explicitly given here; should be manually chosen within region during verification. Good route-node candidate, but not seed-commit-ready above Tier 1 without human pass. |
| LMK-MOONGLOW-MOONGATE-FEL-T1 | Moonglow Moongate | Moongate | Felucca | 4467 | 1284 | unknown | Moongates / Moonglow | moonglow_moongate | `Data/Regions.xml` | `Moongates` guarded region, Moonglow rect `x=4459 y=1276 width=16 height=16` | regions xml | Tier 1 | true | true | false | low | repo-derived rectangle center only | Same caveat as Britain moongate: semantically clear, but exact standable tile should be chosen during manual verification. |
| LMK-SPIRITUALITY-SHRINE-FEL-T1 | Shrine of Spirituality | Shrine / SafePoint | Felucca | 1589 | 2485 | 5 | Shrines / Spirituality | shrine_spirituality | `Data/Locations/felucca.xml` + `Data/Decoration/Britannia/shrines.cfg` | locations child `Spirituality x=1589 y=2485 z=5`; decor ankh at `1592 2489 20` nearby | locations xml + decoration cfg corroboration | Tier 1 | false | true | false | low | repo-derived, not manually verified | Strong safe-point candidate because both semantic node and shrine decor support the area. Exact landing tile may be location node or adjusted nearby tile. |
| LMK-SHAME-ENTRANCE-FEL-T1 | Shame Entrance | DungeonEntrance | Felucca | 514 | 1561 | 0 | Shame | shame | `Data/Locations/felucca.xml` | `Shame -> Entrance x=514 y=1561 z=0` | locations xml | Tier 1 | false | false | true | medium | repo-derived, not manually verified | Clean, explicit dungeon entrance. Strong first danger-point candidate. Could substitute `Despise Entrance` if desired. |

## Confidence Tiers Applied To Candidates
- All six retained candidates are **Tier 1**.
- None were elevated to Tier 2 because no in-game/manual verification was performed in this phase.
- None were elevated to Tier 3 because no adapter-backed runtime validation was performed in this phase.

## Recommended Tiny First Seed Scope
Recommended first eventual runtime seed set should stay in the **5-6 entry** range and should be selected from the following verified-clean candidate pool:
- `New Haven Bank (bank-adjacent)`
- `Magincia Bank`
- `Britain Moongate`
- `Moonglow Moongate`
- `Shrine of Spirituality` or another shrine/safe point only if you want one safe semantic non-bank anchor
- `Shame Entrance`

Recommended first commit shape after verification:
- 1 bank: `New Haven Bank (bank-adjacent)`
- 1 bank: `Magincia Bank`
- 1 moongate: `Britain Moongate`
- 1 moongate: `Moonglow Moongate`
- 1 safe point: `Shrine of Spirituality` or a later cleaner healer if verified
- 1 dungeon entrance: `Shame Entrance`

## Categories That Could Not Be Cleanly Verified
### Stable
- **Status:** missing from this minimal packet
- **Reason:** the inspected repo sources did not surface a clean, semantically named stable/stablemaster coordinate with confidence comparable to banks, moongates, shrine, or dungeon entrance.
- **Recommendation:** do not invent a stable seed. Carry stable as a missing category for a later targeted verification packet.

### Healer (town-safe, first-class)
- **Status:** not selected as primary minimal candidate
- **Reason:** spawn files expose many healer/wandering healer records, but these are noisier and more shard-contextual than the town/bank/moongate/location XML candidates.
- **Recommendation:** use shrine safe-point or bank-adjacent safe-point first unless a future verification pass isolates a clean New Haven healer coordinate.

### Road / Crossroads node
- **Status:** not selected as primary minimal candidate
- **Reason:** town `Center` nodes exist in `Data/Locations`, but a true road/crossroads semantic is not clearly named in the inspected sources.
- **Recommendation:** if a route-node-only test is desired later, `Britain Center` or `Moonglow Center` can serve as a surrogate after manual verification, but do not force it into the first seed batch now.

## First Seed Commit Criteria
A candidate should **not** be committed as actual runtime seed data unless all of the following are true:
1. Coordinate is repo-derived.
2. Facet is explicit.
3. Source file is identified and stable.
4. Semantic kind is reasonable and minimally ambiguous.
5. No shard-custom ambiguity blocks it.
6. Manual verification path is defined.
7. Future adapter validation path is defined.
8. Preferred landing tile is standable and not inside walls/objects.
9. `AreaName` / `AreaKey` choice is stable and human-readable.
10. Candidate remains within the tiny first-seed scope and does not trigger scope creep.

## Manual In-Game Verification Checklist
Do **not** perform this in this phase. This is the operator packet for a later verification step.

For each candidate:
1. Teleport or Go to the candidate coordinate.
2. Confirm the expected facet (`FacetName`).
3. Confirm the landmark exists and matches the intended semantic label.
4. Confirm the coordinate is on a standable tile or identify the nearest safe standable tile.
5. Confirm the point is not inside a wall, deco object, blocked gate, or wrong Z.
6. Confirm visual area identity (town, moongate plaza, shrine area, dungeon entrance, etc.).
7. Confirm route value:
   - is this a useful destination anchor?
   - is this a useful route node?
8. Confirm classification:
   - `IsSafePoint`
   - `IsDangerPoint`
   - `IsRouteNode`
9. Record any adjusted arrival tile if the semantic object tile itself is unusable.
10. Mark candidate promoted to Tier 2 only after this checklist is completed.

Suggested candidate-specific notes:
- **New Haven Bank:** verify teller/bank-adjacent stand tile and whether `3506,2570,14` is the best user-facing arrival point.
- **Magincia Bank:** verify exact bank-facing arrival tile.
- **Britain/Moonglow Moongates:** pick exact standable center tile within guarded moongate region rectangle.
- **Shrine of Spirituality:** decide whether the location node or nearby ankh-adjacent tile is the better landmark point.
- **Shame Entrance:** confirm that the entrance coordinate is standable and represents the intended front-door landmark.

## Runtime Adapter Verification Checklist
Do **not** implement this in this phase. This is for later adapter-backed validation.

For each candidate after static seed implementation exists:
1. `ProbePoint` on the chosen tile should succeed and not indicate obvious invalid placement.
2. `GetRegionAt` should return useful area metadata when available.
3. Landmark should remain purely passive and not require live object references.
4. Resolver should not depend on entity scanning or world mutation.
5. Nearby entity scan remains deferred for this slice.
6. If the probe reveals a blocked/unreachable tile, adjust the seed coordinate rather than expanding resolver complexity.
7. Promotion to Tier 3 requires both Tier 2 manual confirmation and successful adapter-level validation.

## Recommended Next Phase
### Primary recommendation
**Phase 56Q-R5-LANDMARK-REGISTRY-SEED-SKELETON**
- Reason: the candidate coordinates are clear enough at Tier 1 to lock the static seed-registry shape, but not yet strong enough to justify committing actual runtime seed data without a human verification pass.
- This keeps momentum without prematurely hardcoding possibly imperfect arrival tiles.

### Alternate recommendation
**Phase 56Q-R5-LANDMARK-MANUAL-VERIFY-PACKET**
- Reason: if you want to maximize correctness before any seed-bearing code lands, produce a dedicated operator packet from the six candidates above and verify them in-game before adding a static seed class.

### Explicit non-recommendation
- Do **not** jump to full landmark lookup/resolution implementation yet.
- Do **not** jump to broad static seed data commit before verifying at least the moongate landing tiles and bank arrival tiles.

## Cross-Repo Alignment Notes
- **NeoUO code repo:** should later host the static seed class / registry implementation and consume only verified landmarks.
- **NeoUO wiki/NL repo:** should later document candidate verification steps, confidence tiers, and seed maintenance policy.
- **UO UMG repo:** can later define semantic landmark classes, route-node meaning, safe/danger semantics, and unresolved-landmark block behavior, but should not directly feed runtime seed data yet.
- **IR glyph lane:** later can add glyphs for landmark, route node, safe point, danger point, unresolved landmark; no glyph work belongs in this phase.

## No-Mutation Confirmation
- No C# files changed: confirmed
- No seed files added: confirmed
- No registry implementation added: confirmed
- No lookup behavior added: confirmed
- No ServUO reads implemented: confirmed
- No movement/pathfinding/stuck recovery added: confirmed
- No world/control mutation: confirmed
- No OpenClaw main workspace touched: confirmed
- No wiki/NL repo changed: confirmed
- No UO UMG repo changed: confirmed
- No IR glyph work changed: confirmed
- No active subagents used: confirmed
- No commit occurred: confirmed
