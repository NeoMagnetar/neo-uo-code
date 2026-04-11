$ErrorActionPreference = 'Stop'
$serverRoot = 'C:\UO\Server\ServUO'
$serverExe = Join-Path $serverRoot 'ServUO.exe'
$serverPort = 2593
$serverTitle = 'NeoUO Main Server'
$clientRoot = 'C:\UO\Client\ClassicUO'
$clientExe = Join-Path $clientRoot 'ClassicUO.exe'
$activeSettings = Join-Path $clientRoot 'settings.json'
$envSettings = Join-Path $clientRoot 'settings.main.json'

function Test-PortListening($port) {
    return $null -ne (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
}

if (-not (Test-PortListening $serverPort)) {
    Start-Process powershell.exe -ArgumentList @('-NoExit','-Command',"`$Host.UI.RawUI.WindowTitle = '$serverTitle'; `$env:PATH = 'C:\Program Files\dotnet;' + `$env:PATH; Set-Location '$serverRoot'; & '$serverExe' -debug") -WorkingDirectory $serverRoot
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        if (Test-PortListening $serverPort) { break }
    }
}

if (-not (Test-PortListening $serverPort)) { throw 'NeoUO Main server did not start on port 2593.' }
Copy-Item -LiteralPath $envSettings -Destination $activeSettings -Force
Start-Process -FilePath $clientExe -WorkingDirectory $clientRoot
