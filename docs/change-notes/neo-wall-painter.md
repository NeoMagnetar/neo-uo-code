# NeoWall Painter

## Purpose
Provide a Dev-only GM world-building tool for manually sketching perimeter walls in the live world one tile at a time.

## Commands
- `[NeoWallStart`
- `[NeoWallStop`
- `[NeoWallClear`

## Behavior
- when active, moving the GM in Felucca places one immovable wall tile at the GM's current location
- painted tiles are named `NeoUO Painted Wall`
- clear removes previously painted wall tiles created by this tool

## Implementation
File:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Custom\NeoWallPainter.cs`

Uses:
- `EventSink.Movement`
- Dev-only GM command registration
- world item cleanup by marker name

## Validation
Operator reported that the wall painter works in live Dev use.

## Scope
- Dev only
- no baseline/source changes
- no staging changes
