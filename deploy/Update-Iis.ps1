param(
    [Parameter(Mandatory = $true)]
    [string]$PackageZip,
    [int]$KeepBackups = 5
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Deployment.Common.ps1')

$stageDir = Join-Path $FarmStateRoot ('staging/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))

try {
    Assert-FarmAdministrator
    if (-not (Test-Path -LiteralPath $PackageZip -PathType Leaf)) {
        throw "Package zip not found: $PackageZip"
    }

    Import-Module WebAdministration
    Set-FarmPrivateDirectory $FarmStateRoot

    New-Item -ItemType Directory -Path $stageDir -Force | Out-Null
    Expand-Archive -LiteralPath $PackageZip -DestinationPath $stageDir -Force
    Assert-FarmPackage $stageDir

    $configPath = Join-Path $FarmSiteRoot 'appsettings.Local.json'
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "Missing $configPath on server. Run initial setup first."
    }

    $site = Get-Website | Where-Object Name -eq $FarmSiteName
    if (-not $site) {
        throw "IIS site '$FarmSiteName' not found. Run Install-Iis.ps1 for initial setup."
    }

    # Back up current site files before stopping and replacing binaries.
    $backupsRoot = Join-Path $FarmStateRoot 'backups'
    New-Item -ItemType Directory -Path $backupsRoot -Force | Out-Null
    $backupDir = Join-Path $backupsRoot (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Get-ChildItem -LiteralPath $FarmSiteRoot -Force | Copy-Item -Destination $backupDir -Recurse -Force
    Write-Output "Backup created: $backupDir"

    if ($site.State -eq 'Started') {
        Stop-Website -Name $FarmSiteName
    }
    if (Test-Path "IIS:\AppPools\$FarmSiteName") {
        if ((Get-WebAppPoolState $FarmSiteName).Value -eq 'Started') {
            Stop-WebAppPool $FarmSiteName
        }
        for ($i = 0; $i -lt 30 -and (Get-WebAppPoolState $FarmSiteName).Value -ne 'Stopped'; $i++) {
            Start-Sleep -Seconds 1
        }
        if ((Get-WebAppPoolState $FarmSiteName).Value -ne 'Stopped') {
            throw "Application pool '$FarmSiteName' did not stop in time. No binaries replaced."
        }
    }

    # Copy published app files while preserving appsettings.Local.json and wwwroot/uploads.
    Get-ChildItem -LiteralPath (Join-Path $stageDir 'app') -Force | Copy-Item -Destination $FarmSiteRoot -Recurse -Force

    if ((Get-WebAppPoolState $FarmSiteName).Value -ne 'Started') {
        Start-WebAppPool $FarmSiteName
    }
    if ((Get-Website | Where-Object Name -eq $FarmSiteName).State -ne 'Started') {
        Start-Website $FarmSiteName
    }

    # Prune older file backups to avoid filling disk.
    $oldBackups = Get-ChildItem -LiteralPath $backupsRoot -Directory | Sort-Object Name -Descending | Select-Object -Skip $KeepBackups
    foreach ($old in $oldBackups) {
        Remove-Item -LiteralPath $old.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }

    # Local health check against IIS binding.
    Start-Sleep -Seconds 2
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $response = Invoke-WebRequest -Uri "https://$FarmDomain/" -UseBasicParsing -TimeoutSec 20 -MaximumRedirection 5
    Write-Output "Deploy succeeded. Health check HTTP $($response.StatusCode) for https://$FarmDomain/"
}
finally {
    if (Test-Path -LiteralPath $stageDir) {
        Remove-Item -LiteralPath $stageDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
