$FarmDomain = 'quanly.thienmyfarm.com'
$FarmSiteName = 'ThienMyFarm'
$FarmSiteRoot = 'C:\Sites\ThienMyFarm'
$FarmStateRoot = Join-Path $env:ProgramData 'ThienMyFarm'

function Get-FarmRenewalTasks {
    param([string]$Executable)
    @(Get-ScheduledTask | Where-Object {
        $matchesExecutable = $false
        foreach ($action in @($_.Actions)) {
            if ($null -eq $action) { continue }
            $path = [string]$action.Execute
            if ([string]::IsNullOrWhiteSpace($path)) { continue }
            $path = [Environment]::ExpandEnvironmentVariables($path.Trim().Trim('"'))
            if ([string]::Equals($path, $Executable, [StringComparison]::OrdinalIgnoreCase)) { $matchesExecutable = $true }
        }
        $matchesExecutable
    })
}

function Assert-FarmAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not ([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Right-click the BAT file and select Run as administrator.'
    }
    if (-not [Environment]::Is64BitProcess) { throw 'Use 64-bit PowerShell on Windows Server x64.' }
}
function Get-FarmDependencies {
    Get-Content (Join-Path $PSScriptRoot 'deployment-dependencies.json') -Raw | ConvertFrom-Json
}
function Get-FarmDownload {
    param([string]$Url, [string]$Path, [string]$Algorithm, [string]$Hash)
    if (-not $Hash -or $Url -notmatch '^https://') { throw 'Missing dependency checksum or HTTPS URL.' }
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    if (-not (Test-Path -LiteralPath $Path) -or (Get-FileHash -LiteralPath $Path -Algorithm $Algorithm).Hash -ne $Hash) {
        Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $Path
    }
    if ((Get-FileHash -LiteralPath $Path -Algorithm $Algorithm).Hash -ne $Hash) { throw 'Dependency checksum verification failed. File will not be executed.' }
}
function Set-FarmPrivateDirectory {
    param([string]$Path)
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
    $acl = New-Object Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($sid in @('S-1-5-32-544', 'S-1-5-18')) {
        $rule = New-Object Security.AccessControl.FileSystemAccessRule(
            [Security.Principal.SecurityIdentifier]::new($sid), 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
        $acl.AddAccessRule($rule)
    }
    Set-Acl -LiteralPath $Path -AclObject $acl
}
function Assert-FarmPackage {
    param([string]$PackageRoot)
    foreach ($file in @('app/Farm-Management.dll','app/web.config','app/appsettings.Production.json','deployment-dependencies.json','Enable-Ssl.ps1','Initialize-Admin.ps1','database-update.sql')) {
        if (-not (Test-Path -LiteralPath (Join-Path $PackageRoot $file) -PathType Leaf)) { throw ('Missing package file: ' + $file) }
    }
    if (Test-Path -LiteralPath (Join-Path $PackageRoot 'app/appsettings.Local.json')) { throw 'Package must not contain private database credentials.' }
    if (Test-Path -LiteralPath (Join-Path $PackageRoot 'app/wwwroot/uploads')) { throw 'Package must not contain runtime uploads.' }
    $production = Get-Content (Join-Path $PackageRoot 'app/appsettings.Production.json') -Raw | ConvertFrom-Json
    if (($production.AllowedHosts -split ';') -notcontains $FarmDomain) { throw 'Published host configuration does not include the farm domain.' }
    $web = [xml](Get-Content (Join-Path $PackageRoot 'app/web.config') -Raw)
    if ($web.configuration.location.'system.webServer'.aspNetCore.processPath -ne 'dotnet') { throw 'Unexpected IIS web.config.' }
}
