# Dev Lane Validation — 2026-04-11

## Summary

Validated that the NeoUO Dev shard now uses an explicit server-side client data path and no longer requires the original shared `C:\UO\Client\UOFiles` lane to boot or accept login.

## Environment

- Dev shard root: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`
- Dev config file changed: `Config\DataPath.cfg`
- Dev listener: `127.0.0.1:2594`
- Explicit Dev lane: `C:\UO\Client\UOFiles-Test`
- Reserved editor lane: `C:\UO\Client\UOFiles-Editor`
- Shared/default lane restored after test: `C:\UO\Client\UOFiles`

## What was proved

1. `Scripts\Misc\DataPath.cs` resolves the client data path from config via `DataPath.CustomPath`.
2. The actual Dev config source for that value is `Config\DataPath.cfg`.
3. Dev was set explicitly to `CustomPath=C:\UO\Client\UOFiles-Test`.
4. Dev restarted cleanly and listened on `127.0.0.1:2594`.
5. Startup output confirmed `DataPath: C:\UO\Client\UOFiles-Test`.
6. Login and world entry succeeded.
7. Strong no-fallback proof passed:
   - `C:\UO\Client\UOFiles` was temporarily renamed to `UOFiles-HOLD`
   - Dev was restarted
   - Dev still listened on `127.0.0.1:2594`
8. The original folder name was restored after the proof.

## Final conclusion

The Dev shard runtime path is explicitly routed to `C:\UO\Client\UOFiles-Test`, and the original shared `C:\UO\Client\UOFiles` lane is not required for Dev startup or login.

## Lane intent

- `C:\UO\Client\UOFiles` → shared/original/default lane
- `C:\UO\Client\UOFiles-Test` → Dev runtime validation lane
- `C:\UO\Client\UOFiles-Editor` → reserved editor/tools lane
