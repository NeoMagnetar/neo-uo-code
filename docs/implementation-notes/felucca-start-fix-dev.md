# Dev Felucca Start Fix

## Problem
A fresh manually validated Dev character still spawned in Britain on Trammel even after the visible final placement block in `Scripts/Misc/CharacterCreation.cs` had been changed to use the Felucca Britain siege start definition.

Observed controlled retest result before final fix:
- coordinates: `1602 1591 20`
- region: `Britain`
- facet: `Trammel`

## Why the earlier patch was not sufficient
The location patch in `CharacterCreation.cs` set the intended coordinates and map at creation time, but downstream shard behavior still depended on the built-in siege path for forcing Trammel-start players onto Felucca.

A deeper trace found the relevant existing logic in:
- `Scripts/Mobiles/PlayerMobile.cs`

This path only executes when:
- `Siege.SiegeShard == true`

The Dev shard still had:
- `Config/Siege.cfg`
- `IsSiege=false`

So the built-in Trammel-to-Felucca correction path was inactive.

## Final fix applied
Dev-only config change:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Config\Siege.cfg`
- `IsSiege=false` -> `IsSiege=true`

## Result
After restarting Dev with siege enabled:
- Dev booted cleanly on `127.0.0.1:2594`
- siege initialization was confirmed in startup output
- fresh-character manual validation passed
- Britain start now resolves on Felucca in Dev

## Scope
- Dev only
- baseline/source untouched
- staging untouched

## Operational note
This fix relies on the shard's intended siege-mode downstream path instead of adding broader custom post-login relocation hacks.
