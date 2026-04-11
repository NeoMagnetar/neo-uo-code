# Dev Lane Validation Implementation Note — 2026-04-11

## Purpose

This note records the implementation-specific runtime change and validation proof for the NeoUO Dev shard client data lane.

## Runtime target

- Shard root: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`
- Listener target: `127.0.0.1:2594`
- Config file changed: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Config\DataPath.cfg`
- Resolver code inspected: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Misc\DataPath.cs`

## Actual implementation delta

Changed file:
- `NeoUO-Dev\Config\DataPath.cfg`

Old value:
- `CustomPath=C:\UO\Client\UOFiles`

New value:
- `CustomPath=C:\UO\Client\UOFiles-Test`

Backup created before patch:
- `C:\UO\Server\Neo Ultima Online\Backups\_agent-temp\DataPath.cfg.20260411-040529.bak`

## Why this works

`Scripts\Misc\DataPath.cs` resolves the shard client data path from config using `DataPath.CustomPath`. That means the effective runtime lane is controlled by `Config\DataPath.cfg`, not by an implicit assumption that the server must use the original shared `C:\UO\Client\UOFiles` path.

## Validation sequence

1. Restored `C:\UO\Client\UOFiles` from `UOFiles-HOLD` to return to a known-good checkpoint.
2. Confirmed the Dev shard path resolver logic in `Scripts\Misc\DataPath.cs`.
3. Inspected latest crash logs and found a pre-bind startup failure (`TileData: not found`) while the old path state was in flux.
4. Confirmed the active config source was `Config\DataPath.cfg`.
5. Patched `CustomPath` to `C:\UO\Client\UOFiles-Test`.
6. Restarted Dev in PowerShell.
7. Verified successful bind on `127.0.0.1:2594`.
8. Verified startup output reported:
   - `DataPath: C:\UO\Client\UOFiles-Test`
9. Verified login to character list and world entry.

## Strong no-fallback proof

A stronger proof was then performed to verify there was no hidden fallback dependence on the original shared lane:

1. Renamed `C:\UO\Client\UOFiles` → `C:\UO\Client\UOFiles-HOLD`
2. Restarted the Dev shard
3. Verified Dev still listened successfully on `127.0.0.1:2594`
4. Verified login still worked
5. Restored `C:\UO\Client\UOFiles-HOLD` → `C:\UO\Client\UOFiles`

Result:
- **passed**

## Runtime implication

The Dev shard is now explicitly routed to `C:\UO\Client\UOFiles-Test`. The original shared `C:\UO\Client\UOFiles` lane is not required for Dev startup or login.

## Lane roles after validation

- `C:\UO\Client\UOFiles` → shared/original/default lane
- `C:\UO\Client\UOFiles-Test` → Dev runtime validation lane
- `C:\UO\Client\UOFiles-Editor` → reserved editor/tools lane

## Next technical step

Keep editor tooling isolated from shard runtime. If editor-side work continues, route it deliberately through `C:\UO\Client\UOFiles-Editor` instead of reintroducing ambient path assumptions.
