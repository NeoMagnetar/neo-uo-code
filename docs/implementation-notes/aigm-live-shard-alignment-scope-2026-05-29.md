# AIGM Live Shard Alignment Scope
_Date: 2026-05-29_

## Goal

Align `neo-uo-code` more closely with the true current AIGM work from the live NeoUO dev shard without blindly importing unrelated shard drift.

## Included in promotion lane
The following AIGM surfaces are considered intentional and are being promoted into the long-term `custom/scripts/aigm/` lane:
- Commands
- Core AIGM implementation files
- Gumps
- Mobiles
- Interfaces

## Intentionally not promoted in this pass
These remain out of the curated AIGM lane for now:
- `Config/*.cfg` machine/environment drift
- `Scripts/Items/Functional/PublicMoongate.cs`
- `Scripts/Misc/CharacterCreation.cs`
- `Scripts/Services/Help/StuckMenu.cs`
- `Scripts/Custom/DevMoongates.cs`
- `Scripts/Custom/LeaguePerimeterShell.cs`
- `Scripts/Custom/NeoWallPainter.cs`
- crash logs, saves, backups, logs, probes

## Why

The goal is to keep the repo aligned with true work while preserving repository quality and avoiding accidental scope creep.

## Follow-up expectation

Future passes may promote additional shard-specific surfaces, but only when they are explicitly classified and documented.
