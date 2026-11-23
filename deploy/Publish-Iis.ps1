$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$packageName = 'FarmManagement-IIS-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
$package = Join-Path $repo ('artifacts/' + $packageName)
$appFolder = Join-Path $package 'app'
New-Item -ItemType Directory -Path $appFolder -Force | Out-Null
Push-Location $repo
try {
    dotnet publish Farm-Management -c Release --no-restore -p:UseAppHost=false -o $appFolder --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed. Run dotnet restore first if dependencies are missing.' }
    # Never distribute development config, local credentials or runtime uploads.
    foreach ($name in @('appsettings.Local.json', 'appsettings.Development.json', 'appsettings.Local.example.json')) {
        $path = Join-Path $appFolder $name
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
    }
    if (Test-Path -LiteralPath (Join-Path $appFolder 'wwwroot/uploads')) { throw 'Runtime uploads must not be included in a publish package.' }
    @{
        AllowedHosts = 'quanly.thienmyfarm.com;localhost;127.0.0.1'
        Logging = @{ LogLevel = @{ Default = 'Information'; 'Microsoft.AspNetCore' = 'Warning' } }
    } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $appFolder 'appsettings.Production.json') -Encoding UTF8
    dotnet ef migrations script --idempotent --project Farm-Management --configuration Release --no-build --output (Join-Path $package 'database-update.sql')
    if ($LASTEXITCODE -ne 0) { throw 'Migration SQL generation failed.' }
    Copy-Item (Join-Path $PSScriptRoot 'DEPLOY-IIS.md') $package
    Copy-Item (Join-Path $PSScriptRoot 'Initialize-Admin.ps1') $package
    foreach ($file in @('DEPLOY-QUANLY.bat','INSTALL-SSL.bat','Install-Iis.ps1','Enable-Ssl.ps1','Deployment.Common.ps1','deployment-dependencies.json')) {
        Copy-Item (Join-Path $PSScriptRoot $file) $package
    }
    Copy-Item (Join-Path $repo 'Farm-Management/appsettings.Local.example.json') $package
    $config = [xml](Get-Content (Join-Path $appFolder 'web.config') -Raw)
    if ($config.configuration.location.'system.webServer'.aspNetCore.processPath -ne 'dotnet') { throw 'Unexpected IIS process path.' }
    $aspNetCore = $config.configuration.location.'system.webServer'.aspNetCore
    $variables = $aspNetCore.SelectSingleNode('environmentVariables')
    if (-not $variables) { $variables = $config.CreateElement('environmentVariables'); [void]$aspNetCore.AppendChild($variables) }
    foreach ($entry in @(@{Name='ASPNETCORE_ENVIRONMENT';Value='Production'},@{Name='DOTNET_ENVIRONMENT';Value='Production'},@{Name='ASPNETCORE_HTTPS_PORT';Value='443'})) {
        $variable = $variables.SelectSingleNode("environmentVariable[@name='$($entry.Name)']")
        if (-not $variable) { $variable = $config.CreateElement('environmentVariable'); [void]$variables.AppendChild($variable) }
        $variable.SetAttribute('name', $entry.Name); $variable.SetAttribute('value', $entry.Value)
    }
    $config.Save((Join-Path $appFolder 'web.config'))
    $zip = $package + '.zip'
    Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
    Set-Content ($zip + '.sha256') ($hash + '  ' + [IO.Path]::GetFileName($zip))
    Write-Output ('Package: ' + $zip)
    Write-Output ('SHA256: ' + $hash)
} finally { Pop-Location }
