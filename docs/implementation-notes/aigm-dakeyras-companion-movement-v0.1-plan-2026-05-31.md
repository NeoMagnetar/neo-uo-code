# AIGM Dakeyras Companion Travel / Movement v0.1 Result
_Date: 2026-05-31_

## Status
Working success, with known rough edges.

The live Dakeyras companion travel lane was developed and validated in the active shard source tree:

`C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts`

This implementation note records the current mirrored implementation snapshot now synced into this repo under:

`custom/scripts/aigm-current/Scripts/...`

## What was accomplished

The companion lane now contains a real travel / autopathing subsystem rather than only basic movement behavior.

### Core capabilities added or extended
- named destination travel
- destination registry with aliases
- travel status reporting
- stop / cancel travel support
- companion-native travel objective/state
- breadcrumb memory and failed-point tracking
- stuck-zone tracking
- trap recovery / escape search
- PathFollower-first long-range travel intent
- wall-follow / obstacle-skirt behavior for blocked terrain
- cleaner strategy-selection vs movement-execution separation

## Key files now present in the mirrored implementation lane

### Travel / navigation core
- `AIGMCompanionTravelController.cs`
- `AIGMCompanionDestinationRegistry.cs`
- `AIGMCompanionTravelObjective.cs`
- `AIGMCompanionMapNavigator.cs`
- `AIGMCompanionAutoPathNavigator.cs`
- `AIGMCompanionTrapRecovery.cs`
- `AIGMTravelPathStrategy.cs`
- `AIGMTravelMemory.cs`
- `AIGMMovementStartOptions.cs`

### Companion integration files updated
- `AIGMCompanionIntent.cs`
- `AIGMCompanionIntentParser.cs`
- `AIGMCompanionActionExecutor.cs`
- `AIGMCompanionDirectActionPolicy.cs`
- `AIGMCompanionStateAccess.cs`
- `AIGMMovementController.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`

## Current behavior summary

Dakeyras can now:
- accept named travel commands
- resolve a larger set of destinations
- attempt long-range travel with pathing support
- report travel status
- stop travel cleanly
- persist through some blocked-terrain situations better than before

The current system is improved but not final.

## Important design learnings

### 1. Live source truth
The active runtime source for this lane is the live `NeoUO-Dev\Scripts` tree, not older curated snapshots.

### 2. Local-only detour logic was not enough
Caves, mountain ranges, and winding terrain exposed the weakness of purely reactive local stepping.

### 3. PathFollower should anchor long-range movement
Long-range travel now intentionally leans toward PathFollower-first behavior rather than only local twitch logic.

### 4. Strategy selection and execution needed separation
A cleanup refactor was performed so the navigator can choose a strategy in one place and execute movement in another, reducing overlap and internal conflict.

### 5. Obstacle-skirt behavior remains the weakest current layer
Wall-follow / reroute behavior improved, but remains the most likely area for future tuning.

## Current destination coverage

The destination registry was expanded substantially beyond the initial Britain-only lane, including categories such as:
- major towns
- banks
- classic moongates
- virtue shrines
- major dungeon entrances
- extra Britain landmarks / aliases

## Current outcome assessment

This milestone should be treated as:
- successful
- practically improved
- publishable as a real implementation advance
- still open to future tuning for difficult terrain and route quality

## Recommended next steps
- continue observing difficult terrain cases
- improve wall-follow / obstacle-side reasoning
- improve progress scoring so temporary wrong-way movement is not over-penalized
- consider curated waypoint / pass / cave-mouth anchor routing for notoriously difficult geography
