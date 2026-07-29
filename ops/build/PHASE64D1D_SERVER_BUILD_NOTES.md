# Phase64D1D Server Build Notes

## Command

```powershell
dotnet restore .\ServUO.sln
dotnet build .\ServUO.sln -c Release --no-restore
```

The authoritative server deployment used:

```powershell
dotnet build .\ServUO.sln -c Release
```

## Result

- Restore result: success.
- Release build result: 0 warnings, 0 errors.
- Previous accepted D1C `Scripts.dll`: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`
- Final deployed/runtime D1D `Scripts.dll`: `EE6956036DDD769E052CD42FACC16751C4411B8EF0C32142A862F71AAE2BE8F2`
- Existing `versions_v2.json`: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`

## Changed Source

- `Scripts/Custom/AIGM/UMG/AIGMUMGOperationalLayoutModel.cs`
- `Scripts/Custom/AIGM/UMG/AIGMUMGOperationalLayoutService.cs`
- `Scripts/Gumps/AIGMUMGSleeveSelectorGump.cs`
- `Scripts/Commands/AIGMUMGCommand.cs`
- `Scripts/Commands/AIGMUMGOperationalLayoutProofCommand.cs`

## Deployment Boundary

Only `Scripts.dll` was deployed to the live server for D1D. ClassicUO files remained unchanged from D1C.

Rollback remains one file: restore the pre-D1D `Scripts.dll` backup after stopping ServUO, then start ServUO and verify the listener on `127.0.0.1:2595`.