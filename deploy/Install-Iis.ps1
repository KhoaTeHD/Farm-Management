param([switch]$CheckPackage)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Deployment.Common.ps1')
try {
    Assert-FarmPackage $PSScriptRoot
    if ($CheckPackage) { Write-Output "Package OK. Target: $FarmDomain -> $FarmSiteRoot. No server changes made."; exit 0 }
    Assert-FarmAdministrator
    if (-not (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue)) { Import-Module ServerManager }
    $feature = Install-WindowsFeature Web-Server,Web-Mgmt-Console -IncludeManagementTools
    if (-not $feature.Success) { throw 'IIS installation failed.' }
    if ($feature.RestartNeeded -eq 'Yes') { exit 3010 }
    Import-Module WebAdministration
    $dependencies = Get-FarmDependencies
    Set-FarmPrivateDirectory $FarmStateRoot
    $downloads = Join-Path $FarmStateRoot 'downloads'
    New-Item -ItemType Directory -Path $downloads -Force | Out-Null
    $dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
    $runtimeReady = $false
    if (Test-Path -LiteralPath $dotnet) { $runtimeReady = @(& $dotnet --list-runtimes | Where-Object { $_ -match '^Microsoft.AspNetCore.App 8\.' }).Count -gt 0 }
    if (-not $runtimeReady -or -not (Get-WebGlobalModule | Where-Object Name -eq 'AspNetCoreModuleV2')) {
        $installer = Join-Path $downloads 'dotnet-hosting.exe'
        Get-FarmDownload $dependencies.HostingUrl $installer SHA512 $dependencies.HostingSha512
        $signature = Get-AuthenticodeSignature -LiteralPath $installer
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'Hosting Bundle Microsoft signature validation failed.' }
        $install = Start-Process -FilePath $installer -ArgumentList '/install','/quiet','/norestart' -WindowStyle Hidden -Wait -PassThru
        if ($install.ExitCode -in @(3010,1641)) { exit 3010 }
        if ($install.ExitCode -ne 0) { throw ('Hosting Bundle failed: ' + $install.ExitCode) }
        if (-not (Get-WebGlobalModule | Where-Object Name -eq 'AspNetCoreModuleV2')) { throw 'Restart Windows Server after Hosting Bundle installation, then rerun this BAT.' }
    }
    $env:PATH = (Split-Path $dotnet -Parent) + ';' + $env:PATH
    # Refuse to overwrite another site's content or share its pool.
    foreach ($other in Get-Website) {
        if ($other.Name -ne $FarmSiteName) {
            if ($other.ApplicationPool -eq $FarmSiteName -or @($other.Bindings.Collection | Where-Object { $_.bindingInformation -match (':' + [regex]::Escape($FarmDomain) + '$') }).Count -gt 0) {
                throw ('Domain or application pool is already used by another IIS site: ' + $other.Name)
            }
        }
    }
    $site = Get-Website | Where-Object Name -eq $FarmSiteName
    if ($site -and [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($site.PhysicalPath)).TrimEnd('\') -ne $FarmSiteRoot) { throw 'Existing site points to another folder. Resolve it in IIS before proceeding.' }
    if ((Test-Path -LiteralPath $FarmSiteRoot) -and @(Get-ChildItem -LiteralPath $FarmSiteRoot -Force).Count -gt 0 -and -not (Test-Path -LiteralPath (Join-Path $FarmSiteRoot 'Farm-Management.dll'))) {
        throw 'Target folder contains unrelated files; no files have been overwritten.'
    }
    $configPath = Join-Path $FarmSiteRoot 'appsettings.Local.json'
    $connection = $null
    if (-not (Test-Path -LiteralPath $configPath)) {
        $secure = Read-Host 'SQL Server connection string (hidden input; use the existing FarmManagement database)' -AsSecureString
        try { $connection = (New-Object Net.NetworkCredential('', $secure)).Password } finally { $secure.Dispose() }
        if ([string]::IsNullOrWhiteSpace($connection) -or $connection -notmatch '(?i)(Database|Initial Catalog)\s*=') { throw 'A database connection string is required.' }
    } else {
        $existingConfig = Get-Content $configPath -Raw | ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace($existingConfig.ConnectionStrings.DefaultConnection)) { throw 'Existing database configuration is empty. Correct it before deployment.' }
    }
    # Back up before stopping and replacing application binaries; never use /MIR or delete uploads.
    if (Test-Path -LiteralPath $FarmSiteRoot) {
        $backup = Join-Path $FarmStateRoot ('backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
        New-Item -ItemType Directory -Path $backup -Force | Out-Null
        Get-ChildItem -LiteralPath $FarmSiteRoot -Force | Copy-Item -Destination $backup -Recurse -Force
        Write-Output ('File backup: ' + $backup + ' (SQL backup must be managed separately).')
    }
    if ($site -and $site.State -eq 'Started') { Stop-Website -Name $FarmSiteName }
    if (Test-Path "IIS:\AppPools\$FarmSiteName") {
        if ((Get-WebAppPoolState $FarmSiteName).Value -eq 'Started') { Stop-WebAppPool $FarmSiteName }
        for ($i=0; $i -lt 30 -and (Get-WebAppPoolState $FarmSiteName).Value -ne 'Stopped'; $i++) { Start-Sleep -Seconds 1 }
        if ((Get-WebAppPoolState $FarmSiteName).Value -ne 'Stopped') { throw 'Application pool has not stopped. No binaries replaced.' }
    } else { New-WebAppPool -Name $FarmSiteName | Out-Null }
    Set-ItemProperty "IIS:\AppPools\$FarmSiteName" -Name managedRuntimeVersion -Value ''
    Set-ItemProperty "IIS:\AppPools\$FarmSiteName" -Name enable32BitAppOnWin64 -Value $false
    Set-ItemProperty "IIS:\AppPools\$FarmSiteName" -Name processModel.identityType -Value 4
    Set-ItemProperty "IIS:\AppPools\$FarmSiteName" -Name processModel.loadUserProfile -Value $true
    New-Item -ItemType Directory -Path $FarmSiteRoot -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'app') -Force | Copy-Item -Destination $FarmSiteRoot -Recurse -Force
    if ($connection) { @{ ConnectionStrings = @{ DefaultConnection = $connection } } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $configPath -Encoding UTF8 }
    $uploads = Join-Path $FarmSiteRoot 'wwwroot/uploads'
    New-Item -ItemType Directory -Path $uploads -Force | Out-Null
    & icacls.exe $FarmSiteRoot /grant "IIS AppPool\${FarmSiteName}:(OI)(CI)(RX)" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not grant application read access.' }
    & icacls.exe $uploads /grant "IIS AppPool\${FarmSiteName}:(OI)(CI)(M)" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Could not grant upload write access.' }
    $acl = New-Object Security.AccessControl.FileSecurity
    $acl.SetAccessRuleProtection($true,$false)
    foreach ($sid in @('S-1-5-32-544','S-1-5-18')) { $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule([Security.Principal.SecurityIdentifier]::new($sid),'FullControl','Allow'))) }
    $acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule("IIS AppPool\$FarmSiteName",'Read','Allow')))
    Set-Acl -LiteralPath $configPath -AclObject $acl
    if (-not $site) { New-Website -Name $FarmSiteName -ApplicationPool $FarmSiteName -PhysicalPath $FarmSiteRoot -Port 80 -HostHeader $FarmDomain | Out-Null }
    elseif (-not (Get-WebBinding -Name $FarmSiteName -Protocol http | Where-Object bindingInformation -eq "*:80:$FarmDomain")) { New-WebBinding -Name $FarmSiteName -Protocol http -Port 80 -HostHeader $FarmDomain }
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $FarmSiteName -Filter 'system.webServer/security/authentication/anonymousAuthentication' -Name enabled -Value $true
    Set-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Location $FarmSiteName -Filter 'system.webServer/security/authentication/anonymousAuthentication' -Name userName -Value ''
    foreach ($port in @(80,443)) {
        $ruleName = "ThienMyFarm-TCP-$port"
        if (-not (Get-NetFirewallRule -Name $ruleName -ErrorAction SilentlyContinue)) { New-NetFirewallRule -Name $ruleName -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $port | Out-Null }
    }
    & (Join-Path $PSScriptRoot 'Initialize-Admin.ps1') -AppDirectory $FarmSiteRoot
    if ((Get-WebAppPoolState $FarmSiteName).Value -ne 'Started') { Start-WebAppPool -Name $FarmSiteName }
    if ((Get-Website | Where-Object Name -eq $FarmSiteName).State -ne 'Started') { Start-Website -Name $FarmSiteName }
    & (Join-Path $PSScriptRoot 'Enable-Ssl.ps1')
    Write-Output "Deployment complete: https://$FarmDomain . Follow DEPLOY-IIS.md for database and login checks."
} catch {
    Write-Error ('Deployment stopped at ' + $_.InvocationInfo.ScriptName + ':' + $_.InvocationInfo.ScriptLineNumber + '. ' + $_.Exception.Message) -ErrorAction Continue
    Write-Output 'Existing uploads/config are preserved. If SSL failed after deployment, fix DNS/ports then run INSTALL-SSL.bat. Do not expose credentials in support logs.'
    exit 1
}
