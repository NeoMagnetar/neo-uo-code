# Local Desktop Launchers

## Chosen method

Per-environment PowerShell launcher scripts were created so the operator can use a dedicated desktop shortcut for Main, Dev, or Staging.

Each launcher:
- checks whether the target server port is already listening
- starts the matching server in a titled PowerShell window if needed
- waits for the port to come up
- swaps the ClassicUO active settings file to the matching environment config
- launches ClassicUO

## Client routing method

ClassicUO currently stores routing in `C:\UO\Client\ClassicUO\settings.json`.

To avoid manual port editing, the following environment-specific copies were created:
- `C:\UO\Client\ClassicUO\settings.main.json`
- `C:\UO\Client\ClassicUO\settings.dev.json`
- `C:\UO\Client\ClassicUO\settings.staging.json`

The active `settings.json` is replaced at launch time by the selected environment launcher.

## Paths and ports

### Main
- server path: `C:\UO\Server\ServUO`
- port: `2593`
- launcher script: `tools\NeoUO-Main-Launcher.ps1`

### Dev
- server path: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`
- port: `2594`
- launcher script: `tools\NeoUO-Dev-Launcher.ps1`

### Staging
- server path: `C:\UO\Server\Neo Ultima Online\NeoUO-Staging`
- port: `2595`
- launcher script: `tools\NeoUO-Staging-Launcher.ps1`

## Backup note

Original active client config was backed up to:
- `C:\UO\Client\ConfigBackups\settings-20260410-181228-pre-launchers.json`

## Validation summary

Validation target for this task is launch routing, not deep gameplay.

Expected checks:
- Main launcher routes to `127.0.0.1:2593`
- Dev launcher routes to `127.0.0.1:2594`
- Staging launcher routes to `127.0.0.1:2595`
- if the matching server is not running, the launcher starts it before opening ClassicUO
