$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
. (Join-Path $repo 'deploy/Enable-Ssl.ps1') -LoadOnly

# Exercise the whole setup orchestrator with IIS/CA/OS boundaries replaced.
# No real site, firewall, certificate, task, download or ACME account is changed.
function Assert-FarmAdministrator {}
function Import-Module {}
function Get-Website { [pscustomobject]@{Name='ThienMyFarm';Id=42} }
function Get-WebBinding {
    param($Name,$Protocol)
    if ($Protocol -eq 'http') { [pscustomobject]@{bindingInformation='*:80:quanly.thienmyfarm.com'} }
    elseif ($script:hasBinding) { [pscustomobject]@{bindingInformation='*:443:quanly.thienmyfarm.com';certificateHash='ABC123';certificateStoreName='WebHosting'} }
}
function Resolve-DnsName { [pscustomobject]@{Type='A';IPAddress='192.0.2.1'} }
function Read-Host { $answer=$script:answers[0]; $script:answers=@($script:answers | Select-Object -Skip 1); return $answer }
function Set-FarmPrivateDirectory {}
function New-Item {}
function Get-FarmDownload { $script:downloadCalls++ }
function Test-Path { return $false }
function Expand-Archive { $script:expands++ }
function Get-Item { [pscustomobject]@{NotAfter=$script:expires} }
function Set-WebBinding { $script:sniCalls++ }
function Invoke-WebRequest {
    if ($script:httpsFails) { throw 'Mock HTTPS failure' }
    [pscustomobject]@{StatusCode=200}
}
function Get-ScheduledTask {
    [pscustomobject]@{TaskName='No actions';Actions=$null;State='Ready'}
    [pscustomobject]@{TaskName='Null executable';Actions=@([pscustomobject]@{Execute=$null});State='Ready'}
    [pscustomobject]@{TaskName='COM handler';Actions=@([pscustomobject]@{ClassId='COM-handler'});State='Ready'}
    [pscustomobject]@{TaskName='Other task';Actions=@([pscustomobject]@{Execute='C:\Windows\unrelated.exe'});State='Ready'}
    if ($script:hasTask) {
        $path=Join-Path $FarmStateRoot ('win-acme/' + (Get-FarmDependencies).WinAcmeVersion + '/wacs.exe')
        [pscustomobject]@{TaskName='win-acme renewal';Actions=@($null,[pscustomobject]@{ClassId='COM'},[pscustomobject]@{Execute=('"'+$path.ToUpperInvariant()+'"')});State=$script:taskState}
    }
}
function Invoke-FarmAcmeClient {
    param([string]$Executable,[string[]]$Arguments)
    $script:clientCalls+=,@($Arguments)
    if ($script:caFails) { throw 'win-acme failed with exit code 1' }
    if ($Arguments -contains '--source') {
        if ($Arguments[3] -ne '42' -or $Arguments[5] -ne $FarmDomain -or $Arguments -notcontains '--accepttos') { throw 'Incorrect ACME request arguments' }
        if (-not $script:noBindingAfterIssue) { $script:hasBinding=$true }
        if (-not $script:noAutoTask) { $script:hasTask=$true }
    } elseif ($Arguments -contains '--setuptaskscheduler') {
        if (-not $script:cannotCreateTask) { $script:hasTask=$true }
    } else { throw 'Unexpected ACME command' }
}
function Reset-Scenario {
    $script:answers=@('yes','test@example.invalid','y')
    $script:hasBinding=$false; $script:hasTask=$false; $script:expires=(Get-Date).AddDays(60)
    $script:taskState='Ready'; $script:clientCalls=@(); $script:downloadCalls=0; $script:expands=0; $script:sniCalls=0
    $script:caFails=$false; $script:httpsFails=$false; $script:noBindingAfterIssue=$false
    $script:noAutoTask=$false; $script:cannotCreateTask=$false
}
function Check([bool]$condition,[string]$message) { if (-not $condition) { throw ('FAIL: '+$message) }; Write-Output ('PASS: '+$message) }
function Expect-Failure([string]$pattern,[string]$message) {
    $failure=$null
    try { Invoke-FarmSslSetup | Out-Null } catch { $failure=$_.Exception.Message }
    Check ($null -ne $failure -and $failure -match $pattern) $message
}

Reset-Scenario
$result=@(Invoke-FarmSslSetup)
Check ($script:clientCalls.Count -eq 1 -and $script:hasBinding -and $script:hasTask -and $script:sniCalls -eq 1) 'First installation completes with unrelated null/COM scheduled task actions'
Check (($result -join ' ') -match 'public HTTPS check succeeded') 'Success is reported only after public HTTPS validation'

Reset-Scenario; $script:hasBinding=$true; $script:hasTask=$true
Invoke-FarmSslSetup | Out-Null
Check ($script:clientCalls.Count -eq 0) 'Existing valid certificate and renewal task avoid reissuance; quoted case-insensitive task path is matched'

Reset-Scenario; $script:noAutoTask=$true
Invoke-FarmSslSetup | Out-Null
Check ($script:clientCalls.Count -eq 2 -and $script:clientCalls[1][0] -eq '--setuptaskscheduler') 'Missing renewal task is created after initial certificate issuance'

Reset-Scenario; $script:hasBinding=$true; $script:hasTask=$true; $script:expires=(Get-Date).AddDays(-1)
Invoke-FarmSslSetup | Out-Null
Check ($script:clientCalls.Count -eq 1) 'Expired certificate is not treated as a working installation'

Reset-Scenario; $script:caFails=$true
Expect-Failure 'win-acme failed' 'ACME client failure stops setup with a clear error'

Reset-Scenario; $script:noBindingAfterIssue=$true
Expect-Failure 'No HTTPS certificate binding' 'CA success without an IIS certificate binding is rejected'

Reset-Scenario; $script:noAutoTask=$true; $script:cannotCreateTask=$true
Expect-Failure 'Renewal task was not detected' 'Missing renewal task cannot be reported as complete'

Reset-Scenario; $script:hasBinding=$true; $script:hasTask=$true; $script:taskState='Disabled'
Expect-Failure 'renewal task is disabled' 'Disabled renewal is explicitly reported'

Reset-Scenario; $script:httpsFails=$true
Expect-Failure 'public HTTPS verification failed' 'HTTPS failure never reports working SSL'

Reset-Scenario; $script:answers=@('n')
Expect-Failure 'cancelled' 'Negative confirmation makes no certificate request'
Check ($script:clientCalls.Count -eq 0 -and $script:downloadCalls -eq 0) 'Declining confirmation changes nothing'

Reset-Scenario; $script:answers=@($null)
Expect-Failure 'cancelled' 'Empty/null input is handled without a null method exception'

Reset-Scenario; $script:answers=@('Y','invalid-email','yes')
Expect-Failure 'Invalid email' 'Invalid ACME email is rejected before download or issuance'

Reset-Scenario; $script:answers=@(' YES ','test@example.invalid','n')
Expect-Failure 'Terms not accepted' 'Terms rejection does not issue a certificate'
Check ($script:clientCalls.Count -eq 0 -and $script:downloadCalls -eq 0) 'No ACME request is made without terms acceptance'

Write-Output 'SSL orchestration checks complete using mocked IIS/ACME/OS boundaries. No live certificate was issued.'
