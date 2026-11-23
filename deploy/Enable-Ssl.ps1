param([switch]$LoadOnly)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Deployment.Common.ps1')
function Invoke-FarmAcmeClient {
    param([string]$Executable, [string[]]$Arguments)
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) { throw ('win-acme failed with exit code ' + $LASTEXITCODE + '. Check its log under ProgramData/win-acme and external TCP 80.') }
}
function Invoke-FarmSslSetup {
$script:FarmSslStep = 'Checking IIS site and DNS'
Write-Host ('SSL: ' + $script:FarmSslStep)
Assert-FarmAdministrator
Import-Module WebAdministration
$site = Get-Website | Where-Object Name -eq $FarmSiteName
if (-not $site) { throw 'Deploy the ThienMyFarm IIS site first.' }
if (-not (Get-WebBinding -Name $FarmSiteName -Protocol http | Where-Object bindingInformation -eq "*:80:$FarmDomain")) { throw 'Expected HTTP domain binding is missing.' }
$addresses = @(Resolve-DnsName $FarmDomain -ErrorAction Stop | Where-Object { $_.Type -in @('A','AAAA') } | Select-Object -ExpandProperty IPAddress)
if ($addresses.Count -eq 0) { throw 'Domain has no public A/AAAA records.' }
Write-Output ('DNS addresses: ' + ($addresses -join ', '))
Write-Output 'All A/AAAA records must reach this VPS. Keep TCP 80 open for HTTP-01 issuance and renewal, and TCP 443 for HTTPS. Disable Cloudflare proxy during initial setup.'
if (([string](Read-Host 'DNS is correct and inbound 80/443 are open? [y/N]')).Trim() -notin @('y','yes')) { throw 'SSL cancelled by your answer. No certificate was requested. Rerun INSTALL-SSL.bat when ready.' }
$email = Read-Host 'Your real email for the ACME account'
try { $parsedEmail = [Net.Mail.MailAddress]::new($email) } catch { throw 'Invalid email address.' }
if ($parsedEmail.Address -ne $email) { throw 'Enter a single email address without a display name.' }
Write-Output 'Issuance registers an ACME account with Let''s Encrypt and publishes the domain in Certificate Transparency.'
if (([string](Read-Host 'Accept Let''s Encrypt terms at https://letsencrypt.org/repository/ ? [y/N]')).Trim() -notin @('y','yes')) { throw 'Terms not accepted. SSL was not requested.' }
$script:FarmSslStep = 'Preparing verified win-acme download'
Write-Host ('SSL: ' + $script:FarmSslStep)
$dependencies = Get-FarmDependencies
Set-FarmPrivateDirectory $FarmStateRoot
$toolFolder = Join-Path $FarmStateRoot ('win-acme/' + $dependencies.WinAcmeVersion)
$downloads = Join-Path $FarmStateRoot 'downloads'
New-Item -ItemType Directory -Path $downloads -Force | Out-Null
$zip = Join-Path $downloads ('win-acme-' + $dependencies.WinAcmeVersion + '.zip')
Get-FarmDownload $dependencies.WinAcmeUrl $zip SHA256 $dependencies.WinAcmeSha256
# The persistent folder is used by the renewal task; never run wacs from a temporary package folder.
if (-not (Test-Path -LiteralPath (Join-Path $toolFolder 'wacs.exe'))) { Expand-Archive -LiteralPath $zip -DestinationPath $toolFolder -Force }
$wacs = Join-Path $toolFolder 'wacs.exe'
$script:FarmSslStep = 'Inspecting renewal tasks and HTTPS binding'
Write-Host ('SSL: ' + $script:FarmSslStep)
$renewalTask = @(Get-FarmRenewalTasks $wacs)
$binding = Get-WebBinding -Name $FarmSiteName -Protocol https | Where-Object bindingInformation -eq "*:443:$FarmDomain"
$certificate = $null
if ($binding -and $binding.certificateHash) {
    $hash = $binding.certificateHash
    if ($hash -is [byte[]]) { $hash = [BitConverter]::ToString($hash).Replace('-','') }
    $store = $binding.certificateStoreName
    $certificate = Get-Item "Cert:\LocalMachine\$store\$hash" -ErrorAction SilentlyContinue
}
if ($certificate -and $certificate.NotAfter -gt (Get-Date).AddDays(14) -and $renewalTask.Count -gt 0) {
    Write-Output 'HTTPS certificate and renewal task already exist; no new certificate requested.'
} else {
    $script:FarmSslStep = 'Requesting certificate and installing IIS HTTPS binding'
    Write-Host ('SSL: ' + $script:FarmSslStep)
    Invoke-FarmAcmeClient $wacs @('--source','iis','--siteid',[string]$site.Id,'--host',$FarmDomain,'--validation','selfhosting','--store','certificatestore','--installation','iis','--installationsiteid',[string]$site.Id,'--emailaddress',$email,'--accepttos','--friendlyname',"ThienMyFarm-$FarmDomain")
    $binding = Get-WebBinding -Name $FarmSiteName -Protocol https | Where-Object bindingInformation -eq "*:443:$FarmDomain"
    if (-not $binding -or -not $binding.certificateHash) { throw 'No HTTPS certificate binding found; SSL setup is incomplete.' }
    $script:FarmSslStep = 'Checking automatic certificate renewal'
    Write-Host ('SSL: ' + $script:FarmSslStep)
    $renewalTask = @(Get-FarmRenewalTasks $wacs)
    if ($renewalTask.Count -eq 0) {
        Invoke-FarmAcmeClient $wacs @('--setuptaskscheduler')
        $renewalTask = @(Get-FarmRenewalTasks $wacs)
        if ($renewalTask.Count -eq 0) { throw 'Renewal task was not detected. Verify Task Scheduler before considering SSL complete.' }
    }
}
Set-WebBinding -Name $FarmSiteName -BindingInformation "*:443:$FarmDomain" -PropertyName sslFlags -Value 1
if ($renewalTask | Where-Object State -eq 'Disabled') { throw 'The certificate renewal task is disabled. Enable it in Task Scheduler.' }
$script:FarmSslStep = 'Verifying public HTTPS login'
Write-Host ('SSL: ' + $script:FarmSslStep)
try {
    $check = Invoke-WebRequest -UseBasicParsing -Uri "https://$FarmDomain/Account/Login" -TimeoutSec 30
    if ($check.StatusCode -ne 200) { throw 'Login returned a non-200 response.' }
    Write-Output 'Public HTTPS login check passed with normal certificate validation.'
} catch { throw 'HTTPS binding exists, but public HTTPS verification failed. Check certificate, DNS, firewall and application startup before using the site.' }
Write-Output "SSL binding, renewal and public HTTPS check succeeded: https://$FarmDomain"
Write-Output ('Renewal task: ' + ($renewalTask.TaskName -join ', '))
}
if (-not $LoadOnly) {
    try { Invoke-FarmSslSetup }
    catch { throw ('SSL step [' + $script:FarmSslStep + '] failed at ' + $_.InvocationInfo.ScriptName + ':' + $_.InvocationInfo.ScriptLineNumber + '. ' + $_.Exception.Message) }
}
