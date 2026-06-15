# PHASE58C Old Tracking Behavior Restoration Report

## Purpose
Restore the old Dev tracking feel under the current UMG/capability shell without reopening unsafe attack lanes.

## What was restored

### Direct tracking commands now behave like immediate result reports
These commands now produce immediate old-style tracking results instead of generic watch acknowledgments:

- `[ta]` / `track animals`
- `[tm]` / `track monsters`
- `[tn]` / `track npcs`
- `[th]` / `track human npcs`
- `[tp]` / `track players`
- `[tall]` / `track all`

Expected style restored:
- nearest target name
- tile coordinates
- direction
- distance in tiles
- `Pursuit remains gated.`

### Watch-cycle commands remain separate
These remain watch/state controls:

- `start tracking`
- `start tracking animals`
- `start tracking monsters`
- `tracking status`
- `stop tracking`

### Hunt remains separate
Monster pursuit/attack stays in the explicit hunt lane only:

- `[hm]`
- `hunt monsters`

Tracking commands remain read-only.

## Key fixes made

### 1. Short command routing fix
`AIGMTrackingCommand.RunTracking(...)` was incorrectly calling `StartTracking(...)`, which forced short direct commands into watch behavior.

Fixed so short direct commands call:
- `AIGMCompanionTrackingService.BuildTrackingSweepReport(...)`

### 2. Companion routing consistency
Danyal and Dardalion were still routing direct tracking through deferred/watch-style awareness. Their direct tracking report methods were aligned with Dakeyras to use immediate tracking sweep reports.

### 3. Capability split fix
Direct `Track*` intents were separated from `StartTracking*` intents so that:
- direct `Track*` -> `TrackReadOnly`
- `StartTracking*` -> `TrackingCycle`

### 4. One-shot state isolation
Direct one-shot tracking reports no longer leave tracking watch state behind when a watch was not intentionally started.

This fixed the dump state leak where direct reports were leaving:
- `Active=True`
- `Mode=General`

### 5. Category usefulness improvements
Tracking category usefulness was tightened for:
- `HumanNPCs`
- `NPCs`
- `Players`
- `All`

`track all` now provides a multi-category summary instead of collapsing into owner-rejection noise.

## Current live behavior summary

### Working direct result lanes
- monsters
- animals
- human NPCs
- all summary
- players clear no-result behavior

### Tracking shell behavior
- direct `track X` = immediate result report
- `start tracking X` = tracking watch
- `tracking status` = watch/status
- `stop tracking` = stop watch
- `hunt monsters` = explicit action lane

### Safety preserved
- tracking remains read-only by default
- pursuit remains gated on tracking commands
- no player pursuit/attack from tracking
- no NPC pursuit/attack from tracking
- no animal hunting enabled by default
- hunt remains monster-only

## Main code surfaces

### Command entry points
- `Scripts/Commands/AIGMTrackingCommand.cs`
- `Scripts/Commands/AIGMDumpCommand.cs`

### Tracking behavior
- `Scripts/Custom/AIGM/AIGMCompanionTrackingService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingSensor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingState.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingMode.cs`

### Intent / routing
- `Scripts/Custom/AIGM/AIGMCompanionIntent.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs`

### Hunt interaction / debug
- `Scripts/Custom/AIGM/AIGMCompanionExecutionSpine.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionState.cs`

## Natural-language review of what the commands do

### `track monsters`
Immediately scans nearby mobiles from the companion location, filters true hostile monsters, applies tracking skill checks, sorts by nearest distance, and reports the nearest monster or a clear no-trail result. No pursuit is started.

### `track animals`
Immediately scans nearby animals, sorts by nearest distance, and reports the nearest animal or a clear no-trail result. No pursuit is started.

### `track human npcs`
Immediately scans for non-player human-bodied NPCs (including vendor-like humans where applicable), reports the nearest result, and remains gated.

### `track npcs`
Immediately scans useful non-player NPCs/civilians and reports the nearest result. No pursuit or attack is started.

### `track players`
Immediately scans for players, applies tracking difficulty rules, and reports the nearest result or a clear no-player result. No pursuit is started.

### `track all`
Runs a multi-category summary across animals, monsters, NPCs, human NPCs, and players, then reports a useful summary instead of only rejection noise.

### `start tracking`
Begins a tracking watch/cycle and stores tracking state for later status reporting.

### `tracking status`
Reports the current watch mode, last sweep, and last tile information.

### `stop tracking`
Stops the tracking watch.

## Build status at checkpoint
- `dotnet build .\ServUO.sln -c Release` -> success
- warnings only, 0 errors

## Remaining work
- direct `track npcs` should be validated in more NPC-dense scenes
- proof/report command surface can still be expanded/documented further
- hunt / tracking proof artifacts and metadata can be cleaned up further without changing behavior
