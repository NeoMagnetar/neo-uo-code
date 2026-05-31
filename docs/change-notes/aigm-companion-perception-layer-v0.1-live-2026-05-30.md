# AIGM Companion Perception Layer v0.1 Live — 2026-05-30

## Scope

This change note records the first live implementation slice of the AIGM companion perception/navigation substrate.

The focus of this slice was not autonomous travel. It was the foundational world-state layer:
- location snapshots
- sextant reporting
- Tracking-backed sensing
- bounded recent perception memory
- direct report commands

## Files added / introduced in the perception slice

### New helper/state/service files
- `Scripts/Custom/AIGM/AIGMCompanionStateAccess.cs`
- `Scripts/Custom/AIGM/AIGMCompanionPerceptionModels.cs`
- `Scripts/Custom/AIGM/AIGMCompanionLocationService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionPerceptionBuffer.cs`
- `Scripts/Custom/AIGM/AIGMCompanionThreatClassifier.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingSensor.cs`

## Existing files updated

- `Scripts/Custom/AIGM/AIGMCompanionIntent.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs`
- `Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs`

## Technical implementation details

### 1. Shared companion state access
A shared state access helper was added so both Dakeyras and Danyal can expose common operational properties without duplicating companion-specific access logic everywhere:
- guard-owner mode
- next-support-action timestamp
- execution mode

### 2. Location snapshot service
A new location capture/report service now provides structured snapshots containing:
- companion serial/name
- map name
- region name
- X/Y/Z
- sextant text
- timestamp

This is used for `where are you` style reporting and establishes the navigation-facing location substrate.

### 3. Tracking sensor
A machine-readable tracking sensor wrapper was added over real Tracking-like shard logic.

Current sweep support:
- animals
- monsters
- human NPCs
- players

Each result entry captures:
- target serial
- name
- type name
- category
- distance
- direction approximation
- map/X/Y/Z
- hidden-known flag
- alive state
- threat hint
- timestamp

### 4. Perception buffer
A bounded perception buffer was added to store recent tracking sweeps and recent sightings by serial.

This avoids infinite append-only log growth while still enabling short-lived tactical memory.

### 5. Threat classification
A first-pass threat classifier maps raw tracking entries into simple tactical labels such as:
- Neutral
- Interesting
- PotentialThreat
- ImmediateThreat
- Owner
- KnownCompanion

This is intentionally conservative but sufficient for the first reporting pass.

## New companion intents

The companion intent surface was extended with:
- `report_location`
- `scan_area`
- `track_animals`
- `track_monsters`
- `track_human_npcs`
- `track_players`
- `report_threats`

These were also added to speech parsing phrases such as:
- `where are you`
- `scan area`
- `track monsters`
- `track players`
- `report threats`

## Critical bug fixed during rollout

The new intents initially parsed correctly but still fell through to async AI chat because the direct-action policy did not yet consider them direct companion abilities.

This was corrected in `AIGMCompanionDirectActionPolicy.cs`, after which the report commands executed locally through the structured perception path.

## Confirmed live runtime behavior

After rebuild/deploy and in-world validation, companions successfully performed:

### `where are you`
Returned live:
- X/Y/Z
- map
- region
- sextant coordinates

### `scan area`
Returned compact nearby presence and threat counts.

### `track players`
Returned actual nearby player detection with distance.

### `track animals`
Returned actual nearby animal detections with distances.

### `report threats`
Returned threat summaries from the bounded perception buffer.

This proves the first perception slice is running from local structured mechanics, not from async companion flavor responses.

## Architectural significance

This milestone marks the transition from:
- identity-separated chat companions

to:
- identity-separated companions with real shard-backed environmental perception

It is the correct prerequisite for later work such as:
- passive timed sensing
- shared silent awareness bus
- travel objective state
- cautious route movement
- leader/follower party travel logic
- richer tactical interpretation differences between Dakeyras and Danyal

## Recommended next steps

1. Validate all tracking categories in varied environments.
2. Improve response formatting polish.
3. Add timed passive sensing intervals.
4. Add shared-awareness bus skeleton.
5. Add travel-objective data model before autonomous travel execution.

## Milestone close

The AIGM companion system now has a live Perception Layer v0.1 with structured location awareness, Tracking-backed sensing, bounded tactical memory, and local direct-execute reporting commands.
