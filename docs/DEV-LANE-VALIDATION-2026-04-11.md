# Dev Lane Validation — 2026-04-11

## Summary

Validated that the NeoUO Dev shard now uses an explicit server-side client data path and no longer requires the original shared `C:\UO\Client\UOFiles` lane to boot or accept login.

## Environment

- Dev shard root: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`
- Dev config file changed: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Config\DataPath.cfg`
- Dev listener: `127.0.0.1:2594`
- Explicit Dev lane: `C:\UO\Client\UOFiles-Test`
- Reserved editor lane: `C:\UO\Client\UOFiles-Editor`
- Shared/default lane restored after test: `C:\UO\Client\UOFiles`

## What was proved

1. Restored `C:\UO\Client\UOFiles` from `UOFiles-HOLD` to return to a known-good checkpoint.
2. Inspected `Scripts\Misc\DataPath.cs` and confirmed the shard resolves data path from config via `DataPath.CustomPath`.
3. Inspected latest Dev crash logs and found startup failure was due to client data load (`TileData: not found`) before listener bind.
4. Confirmed the actual Dev config source was `Config\DataPath.cfg`.
5. Set Dev explicitly to `CustomPath=C:\UO\Client\UOFiles-Test`.
6. Restarted Dev and verified clean bind on `127.0.0.1:2594`.
7. Verified clean startup log reporting `DataPath: C:\UO\Client\UOFiles-Test`.
8. Verified login flow worked through Dev and character successfully entered world.
9. Performed the strong no-fallback proof:
   - renamed `C:\UO\Client\UOFiles` to `C:\UO\Client\UOFiles-HOLD`
   - restarted Dev
   - verified Dev still listened on `127.0.0.1:2594`
10. Restored the original folder name back to `C:\UO\Client\UOFiles` after the proof.

## Final conclusion

The Dev shard runtime path is no longer ambient. It is explicitly routed to `C:\UO\Client\UOFiles-Test`, and the original shared `C:\UO\Client\UOFiles` lane is not required for Dev startup or login.

## Lane intent going forward

- `C:\UO\Client\UOFiles` → shared/original/default lane
- `C:\UO\Client\UOFiles-Test` → Dev runtime validation lane
- `C:\UO\Client\UOFiles-Editor` → reserved editor/tools lane

## Backup created during config patch

- `C:\UO\Server\Neo Ultima Online\Backups\_agent-temp\DataPath.cfg.20260411-040529.bak`

