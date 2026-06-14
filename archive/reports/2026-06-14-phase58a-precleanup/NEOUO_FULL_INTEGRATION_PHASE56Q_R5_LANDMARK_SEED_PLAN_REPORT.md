# NEOUO FULL INTEGRATION PHASE56Q R5 LANDMARK SEED PLAN REPORT

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
- Git status: repo contains many unrelated untracked report files; no C# mutation was performed in this phase

## Baseline Build Result
Command:
- `dotnet build .\ServUO.sln -v:minimal`

Result:
- Build succeeded
- Errors: `0`
- Warnings: `0`
- Warning files: none

## Files / Reports Inspected
Landmark stack:
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmarkKind.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmark.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmarkQuery.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationLandmarkResult.cs`
- `Scripts/Custom/AIGM/Navigation/IUMGNavigationLandmarkRegistry.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationNoOpLandmarkRegistry.cs`

Prior reports:
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_LANDMARK_MODEL_P_COMMIT_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_LANDMARK_REGISTRY_CONTRACT_P_COMMIT_REPORT.md`
- `NEOUO_FULL_INTEGRATION_PHASE56Q_R5_LANDMARK_REGISTRY_NOOP_P_COMMIT_REPORT.md`

Potential landmark authorities and references:
- `Data/Regions.xml`
- `Data/teleporters.csv`
- `Data/Locations/felucca.xml`
- `Data/Locations/trammel.xml`
- `Data/Decoration/Britannia/britain.cfg`
- `Data/Decoration/Britannia/moonglow.cfg`
- `Data/Decoration/Britannia/shrines.cfg`
- `Data/Decoration/Trammel/NewHaven.cfg`
- `Spawns/felucca.xml`
- `Spawns/trammel.xml`
- `RevampedSpawns/DespiseRevamped.xml`
- `RevampedSpawns/ShameRevamped.xml`
- `RevampedSpawns/WrongRevamped.xml`
- `Scripts/Gumps/Go/LocationTree.cs`
- `Scripts/Custom/DevMoongates.cs`

## Candidate Landmark Source Inventory

### A. Direct repo/source coordinate authority
These are the strongest in-repo coordinate candidates for initial landmark seeds.

1. `Data/Locations/felucca.xml`
- Classification: **A**
- Why useful: explicit named location tree with exact `x/y/z` coordinates for towns, dungeon entrances/levels, shrines, and specific town subareas.
- Examples observed:
  - Felucca town nodes for Britain, Magincia, Minoc, Moonglow, Skara Brae, Trinsic, Vesper, Yew
  - Dungeon entrances and levels for Covetous, Deceit, Despise, Destard, Hythloth, Shame, Wrong, Fire, Ice
  - Shrine nodes for Chaos, Compassion, Honesty, Honor, Humility, Justice, Sacrifice, Spirituality, Valor
- Usefulness for first seeds: very high for town anchors, dungeon entrances, and shrine/safe-point candidates.

2. `Data/Locations/trammel.xml`
- Classification: **A**
- Why useful: same coordinate structure as Felucca, including New Haven/Haven plus mirrored dungeon/town landmarks on Trammel.
- Examples observed:
  - `Haven -> New Haven` at `3506,2570,14`
  - Town subareas including Magincia bank and Moonglow center/docks
- Usefulness for first seeds: very high for Trammel-specific starter-safe seeds and dual-facet parity.

3. `Data/Regions.xml`
- Classification: **A** for area envelopes and moongate rectangles; **B** for exact standing coordinate inference unless paired with `go` or location data.
- Why useful: region names and rectangles are authoritative for area labeling; includes precise moongate region rectangles for Felucca and `go` points for many regions.
- Examples observed:
  - `Moongates` guarded region rectangles for Britain, Jhelom, Minoc, Trinsic, Yew, Skara Brae, Moonglow, Magincia in Felucca
  - named region `go` points like Heartwood/Sanctuary/Painted Caves/Prism of Light/Blighted Grove/Palace of Paroxysmus
- Usefulness for first seeds: strong for assigning `AreaName` / `AreaKey`, and for validating town/moongate area semantics.

4. `Data/teleporters.csv`
- Classification: **A** for dungeon transition graph and level transition coordinates
- Why useful: explicit entrance/exit coordinate pairs, good for understanding dungeon ingress points and later route-node relationships.
- Examples observed:
  - Shame, Deceit, Covetous, Hythloth, Despise, Destard transition pairs
- Usefulness for first seeds: medium-high for dungeon entrance validation and future route-node planning; less useful for first town-bank seeds.

### B. Likely world-spawn / decorator source
These are useful as corroboration or secondary authority, but not ideal as the only source for first committed seeds.

5. `Spawns/felucca.xml` and `Spawns/trammel.xml`
- Classification: **B**
- Why useful: can reveal banker/minter/healer-like NPC spawn centers and named spawn clusters.
- Examples observed:
  - Trammel banker/minter spawn entry: `banker/minter 3` centered at `3484,2572,20`
  - dungeon spawn clusters like `Despise#25`, `Fire#9`, `TownsPeople#9`
- Risks:
  - spawn centers are not always ideal landmark standing positions
  - shard customizations can drift over time
- Usefulness for first seeds: good for manual verification targets, not ideal as sole committed authority.

6. `RevampedSpawns/*.xml`
- Classification: **D** for safe hardcoding unless manually verified and intentionally shard-owned
- Why useful: indicates shard-custom dungeon emphasis and may contain curated zones.
- Risks: heavily shard-custom, likely to evolve independently, poor choice for first universal seed authority.

7. `Data/Decoration/Britannia/*.cfg`, `Data/Decoration/Trammel/*.cfg`, `Data/Decoration/*/shrines.cfg`
- Classification: **B**
- Why useful: static world decoration placements can corroborate exact structure/item positions near candidate landmarks.
- Examples observed:
  - shrine ankhs in `shrines.cfg`
  - extensive city decoration in `britain.cfg`, `moonglow.cfg`, `NewHaven.cfg`
- Risks:
  - decoration files are verbose and not semantically labeled around every bank/stable/healer
  - many entries are object placements, not intent-level landmark declarations
- Usefulness for first seeds: good corroboration source; especially useful for shrine coordinates and town structure checks.

### C. Manual verification needed
8. `Scripts/Gumps/Go/LocationTree.cs` + `Data/Locations/*.xml`
- Classification: **C** as workflow aid, **A** indirectly via the XML data
- Why useful: shows the repo already treats `Data/Locations/*.xml` as a location tree the shard uses for navigation/Go UI.
- Implication: this is a strong signal that `Data/Locations/*.xml` should be the primary semantic authority for first landmark planning.

### D. Shard-custom and unsafe to hardcode
9. `Scripts/Custom/DevMoongates.cs`
- Classification: **D**
- Why useful: proves custom moongate-like locations can exist.
- Why unsafe: explicitly dev/test-only custom gates; should not influence production landmark seeds.

### E. External/general UO knowledge only; not enough
10. General memory of classic UO town/bank/moongate coordinates
- Classification: **E**
- Reason: not enough for safe commit without in-repo confirmation and in-game verification.

### F. Irrelevant for first seed authority
11. Generic decorative/artifact item files, command files, non-location consumables
- Classification: **F**
- Reason: not semantically useful for first landmark seed planning.

## Source Confidence Tier Definitions
- **Tier 0**: no coordinate, semantic placeholder only. Useful in design docs but should not be runtime-seeded.
- **Tier 1**: repo-derived coordinate, untested. Acceptable for planning and candidate lists only.
- **Tier 2**: repo-derived and manually verified in-game. Minimum confidence to consider first committed seed entries.
- **Tier 3**: repo-derived, manually verified, and runtime adapter later validates passability / area match. Best long-term production confidence.
- **Tier X**: external/general UO coordinate only. Do not commit as runtime seed without repo alignment and manual verification.

## Recommended Initial Seed Categories
Keep the first seed set very small. Do not start with dozens or hundreds of landmarks.

### Recommended first categories
1. **1-2 Banks**
- Why they matter: strongest player-facing landmark utility; easy natural-language queries; useful for travel, commerce, and orientation.
- Suggested first candidates:
  - Trammel / New Haven bank-adjacent area (starter-friendly, safer)
  - Felucca or Trammel Magincia bank from `Data/Locations/*.xml`
- Flags:
  - `IsSafePoint = true`
  - `IsRouteNode = true` if later used as travel anchor
  - `RiskLevel = low`

2. **1-2 Moongates**
- Why they matter: core fast-travel landmarks and likely route-node anchors.
- Suggested first candidates:
  - Britain moongate
  - Moonglow moongate
- Source path:
  - `Data/Regions.xml` moongate rectangles + manual standing coordinate verification
- Flags:
  - `IsRouteNode = true`
  - `IsSafePoint = usually true`

3. **1 Healer / Safe Point**
- Why it matters: recovery / regroup landmark, useful for danger-aware planning later.
- Suggested first candidate:
  - New Haven / Haven healer-adjacent point if confirmed in repo or in-game
- Flags:
  - `IsSafePoint = true`
  - `RiskLevel = very low`
- Note: this likely requires more manual verification because healer coordinates are less cleanly exposed than banks/moongates.

4. **1 Stable**
- Why it matters: companion/pack animal logistics later; also semantically strong landmark.
- Suggested first candidate:
  - New Haven or Britain stable-adjacent point, but only after explicit repo-backed and in-game verification.
- Note: stable locations are likely Tier 1/Tier C from spawn/decor context and need more manual work than banks/moongates.

5. **1 Dungeon Entrance**
- Why it matters: earliest danger-point / destination seed with clear tactical meaning.
- Suggested first candidates:
  - Shame entrance
  - Despise entrance
- Source path:
  - `Data/Locations/felucca.xml` and `trammel.xml` already provide exact entrance coordinates
- Flags:
  - `IsDangerPoint = true`
  - `RiskLevel = medium/high`
  - `IsRouteNode = false` initially unless explicitly needed later

6. **1 Road / Crossroads test node**
- Why it matters: validates non-building navigation landmarks without committing to full path planning.
- Suggested first candidate:
  - a central city square / obvious town center from `Data/Locations` (for example Britain Center or Moonglow Center)
- Safer than inventing arbitrary road coordinates from decoration files.

## Recommended Coordinate Verification Process
Use the same process for every proposed seed candidate.

1. **Start with repo authority**
- Prefer `Data/Locations/*.xml` first.
- For moongates, combine `Data/Regions.xml` rectangle with nearby town center or manual stand point.
- Use spawn/decor files only as corroboration, not sole authority, for first seeds.

2. **Assign provisional tier**
- Repo-only coordinate = Tier 1.

3. **Manual in-game verification**
- Teleport or Go to the candidate location.
- Confirm the landmark is truly what we think it is.
- Confirm the tile is standable and not blocked.
- Confirm it is not inside walls/doors/incorrect Z.
- Confirm whether the point should mark a landmark center, approach point, or entrance tile.
- Promote to Tier 2 when verified.

4. **Future runtime verification**
- Once static seeds exist and adapter-backed lookup exists, use region/probe checks to validate passability and area labeling.
- Promote to Tier 3 after runtime validation is available.

5. **Facet handling rule**
- Treat Felucca and Trammel entries as separate seeds unless a later import/build step intentionally duplicates verified landmarks across both facets.
- Do not assume same coordinates imply same shard semantics without explicit policy.

## First Seed Safety Rules
- No external/general UO coordinates without repo backing or manual verification.
- No seed promotion above Tier 1 without explicit verification notes.
- Prefer exact named semantic anchors from `Data/Locations/*.xml` over inferred decoration centers.
- Prefer open standable approach tiles over decorative center tiles when the exact object tile may be blocked.
- Keep `AreaName` human-readable and `AreaKey` stable.
- Preserve neutral naming: `FacetName`, `AreaName`, `AreaKey`.
- Distinguish “landmark object” from “arrival tile” in future seed metadata notes if needed.
- Do not mix dev/test/custom-only landmarks into initial production seeds.
- Keep first set tiny so each seed can be manually audited.

## Recommended First Minimal Seed Set Strategy
Primary recommendation: start with **5-6 seeds total**.

Suggested candidate mix:
- 1 bank: `Trammel / Haven / New Haven` bank-adjacent anchor if verified
- 1 bank: `Magincia Bank` or another town bank with clean location source
- 1 moongate: `Britain Moongate`
- 1 moongate: `Moonglow Moongate`
- 1 healer/safe point: `New Haven` healer-adjacent point after manual verification
- 1 dungeon entrance: `Shame Entrance` or `Despise Entrance`

Why this mix works:
- Covers commerce, travel, safety, and danger
- Uses categories already represented in `UMGNavigationLandmarkKind`
- Avoids overcommitting to dozens of towns before the registry flow is proven
- Keeps verification burden manageable

## Seed Storage Strategy Recommendation
### Option evaluation
**A. C# static seed class**
- Pros: smallest compile-safe first implementation; no loader/parser complexity; easy to review in diffs; strong type safety with current DTO model.
- Cons: harder for non-code editing later; not ideal for large datasets.

**B. JSON seed file**
- Pros: easy to edit and externalize.
- Cons: needs loader, error handling, deployment conventions, and format ownership too early.

**C. XML seed file**
- Pros: consistent with some existing ServUO data patterns.
- Cons: still adds loader complexity before seed semantics are stable.

**D. Generated from existing ServUO spawn/decorator data**
- Pros: potentially authoritative once mature.
- Cons: overbuilt for first slice; hard to guarantee semantic correctness; risk of hidden shard-specific errors.

**E. UMG block repo supplies semantic landmarks later, NeoUO runtime ingests them**
- Pros: good future separation of semantic authoring vs runtime ingestion.
- Cons: premature for first runtime seed slice without import boundary and verification process.

### Recommendation
- **Primary:** Option **A** — a small static C# seed class when implementation begins.
- **Fallback:** Option **B** — JSON file only after the first handful of seeds are verified and the semantic shape is stable.

Reason:
- The first implementation should optimize for compile safety, diff clarity, and a tiny reviewed seed set.
- Do not build external loaders before the first 3-5 landmarks are verified and proven useful.

## Future File / Class Shape Recommendation
When seed implementation begins, prefer a very small, explicit shape such as:
- one static seed provider class in NeoUO runtime repo
- returns `IEnumerable<UMGNavigationLandmark>` or `List<UMGNavigationLandmark>`
- one method for `GetSeeds()`
- optional internal helper builders for readability
- later, registry lookup can consume this seed provider

Do **not** start with:
- generalized import pipelines
- dynamic scanning of decorators/spawns at runtime
- automatic facet mirroring
- broad fuzzy text resolution

## Future Implementation Phases
### Primary next phase
**Phase 56Q-R5-LANDMARK-SEED-MINIMAL-PLAN-VERIFY**
- Purpose: define the exact first 3-5 landmark candidates and the manual verification checklist/output for each.
- Why primary: the current plan identifies good source surfaces, but the next risk is bad coordinates. Verification should happen before any static runtime seed class is added.

### Alternate next phase
**Phase 56Q-R5-LANDMARK-REGISTRY-SEED-SKELETON**
- Purpose: add a static registry/seed skeleton with zero seed data.
- Why alternate: useful if you want to lock runtime shape first, but slightly riskier than verifying real candidates first.

### Likely later sequence
1. `LANDMARK-SEED-MINIMAL-PLAN-VERIFY`
2. `LANDMARK-REGISTRY-SEED-SKELETON`
3. `LANDMARK-REGISTRY-STATIC-SEED`
4. `LANDMARK-REGISTRY-RESOLVE-NOOP-TO-STATIC`
5. later adapter-assisted validation / confidence promotion

## Cross-Repo Alignment Notes
- **NeoUO code repo:** should own runtime seed registry and lookup implementation later.
- **NeoUO wiki/NL repo:** should later document landmark categories, verification method, confidence tiers, and update workflow.
- **UO UMG repo:** can later provide semantic block definitions for landmark classes, route nodes, safe/danger points, but should not directly drive NeoUO runtime ingestion until an explicit import boundary exists.
- **IR glyph lane:** later glyph work can map landmark, route node, safe point, danger point, unresolved landmark; no glyph changes should happen in this phase.

## Recommended Source Strategy Summary
Best-first authority order:
1. `Data/Locations/*.xml` for semantic named coordinates
2. `Data/Regions.xml` for area naming and moongate/shard area envelopes
3. `Data/teleporters.csv` for dungeon transition corroboration
4. `Spawns/*.xml` for banker/healer/stable corroboration and manual verification targets
5. `Data/Decoration/*.cfg` for object-level corroboration, especially shrines and structured town layout
6. external/general UO knowledge only as non-authoritative background

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
