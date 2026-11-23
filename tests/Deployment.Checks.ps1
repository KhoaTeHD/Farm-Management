param([Parameter(Mandatory=$true)][string]$PackageRoot)
$ErrorActionPreference = 'Stop'
$package = (Resolve-Path -LiteralPath $PackageRoot).Path
$repo = Split-Path $PSScriptRoot -Parent
$fixture = Join-Path $repo ('.local/DeploymentChecks-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $fixture 'app/wwwroot') -Force | Out-Null
foreach ($name in @('Install-Iis.ps1','Deployment.Common.ps1','Enable-Ssl.ps1','Initialize-Admin.ps1','deployment-dependencies.json','database-update.sql')) {
    Copy-Item -LiteralPath (Join-Path $package $name) -Destination $fixture
}
foreach ($name in @('Farm-Management.dll','web.config','appsettings.Production.json')) {
    Copy-Item -LiteralPath (Join-Path $package ('app/' + $name)) -Destination (Join-Path $fixture 'app')
}
function Assert-CheckResult([bool]$Expected, [string]$Label) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $fixture 'Install-Iis.ps1') -CheckPackage 2>&1
        $passed = $LASTEXITCODE -eq 0
    } finally { $ErrorActionPreference = $previousPreference }
    if ($passed -ne $Expected) { throw ('FAIL: ' + $Label + [Environment]::NewLine + ($output -join [Environment]::NewLine)) }
    Write-Output ('PASS: ' + $Label)
}
Assert-CheckResult $true 'Valid IIS package passes without changing IIS, firewall or certificates'
$private = Join-Path $fixture 'app/appsettings.Local.json'
Set-Content -LiteralPath $private -Value '{}'
Assert-CheckResult $false 'Private configuration inside publish package is rejected'
Remove-Item -LiteralPath $private
$uploads = Join-Path $fixture 'app/wwwroot/uploads'
New-Item -ItemType Directory -Path $uploads | Out-Null
Assert-CheckResult $false 'Runtime upload directory in package is rejected'
Remove-Item -LiteralPath $uploads
$productionPath = Join-Path $fixture 'app/appsettings.Production.json'
$production = Get-Content -LiteralPath $productionPath -Raw
Set-Content -LiteralPath $productionPath -Value '{"AllowedHosts":"wrong.example"}'
Assert-CheckResult $false 'Package for another domain is rejected'
Set-Content -LiteralPath $productionPath -Value $production
$dll = Join-Path $fixture 'app/Farm-Management.dll'
Move-Item -LiteralPath $dll -Destination ($dll + '.bak')
Assert-CheckResult $false 'Missing published application is rejected'
Move-Item -LiteralPath ($dll + '.bak') -Destination $dll
Assert-CheckResult $true 'Restored package passes again'
$web = [xml](Get-Content (Join-Path $package 'app/web.config') -Raw)
$variables = $web.configuration.location.'system.webServer'.aspNetCore.environmentVariables.environmentVariable
foreach ($entry in @(@{Name='ASPNETCORE_ENVIRONMENT';Value='Production'},@{Name='DOTNET_ENVIRONMENT';Value='Production'},@{Name='ASPNETCORE_HTTPS_PORT';Value='443'})) {
    if (-not ($variables | Where-Object { $_.name -eq $entry.Name -and $_.value -eq $entry.Value })) { throw ('Missing IIS environment: ' + $entry.Name) }
    Write-Output ('PASS: IIS environment ' + $entry.Name + '=' + $entry.Value)
}
foreach ($name in @('DEPLOY-QUANLY.bat','INSTALL-SSL.bat')) {
    $bat = Get-Content (Join-Path $package $name) -Raw
    if ($bat -notmatch '"%~dp0[^"\r\n]+\.ps1"' -or $bat -notmatch 'exit /b %RESULT%') { throw ('Invalid BAT wrapper: ' + $name) }
    Write-Output ('PASS: BAT quotes its script path and preserves exit code: ' + $name)
}
Write-Output 'Deployment checks complete. Actual IIS installation and ACME issuance require the target Windows Server and public DNS.'
