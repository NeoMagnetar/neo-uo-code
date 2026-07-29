# Phase64D1B Build Notes

Accepted server root:
`C:\UO\Server\Neo Ultima Online\NeoUO-Phase60X-CleanProof`

Build commands:

```powershell
dotnet restore .\ServUO.sln
dotnet build .\ServUO.sln -c Release --no-restore
```

Accepted result:
- Restore: succeeded.
- Release build: 0 warnings, 0 errors.
- Deployed `Scripts.dll` SHA-256: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`.

Runtime pins:
- ServUO listener: `127.0.0.1:2595`.
- Middleware listener: `127.0.0.1:4876`.
- Middleware accepted state: fallback 0, timeout 0, lastError null.
- Middleware request count is dynamic telemetry.

Publication controls:
- Build outputs are not tracked.
- Audit ZIPs are not tracked.
- Private runtime state is not tracked.
