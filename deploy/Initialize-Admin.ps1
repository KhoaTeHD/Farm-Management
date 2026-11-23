param([string]$AppDirectory = (Join-Path $PSScriptRoot 'app'), [switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $appPath 'Farm-Management.dll'))) { throw 'Published application not found.' }
function Invoke-AdminCommand([string]$Command, [string]$Username, [string]$PasswordValue) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
    $info.Arguments = 'Farm-Management.dll ' + $Command
    $info.WorkingDirectory = $appPath
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    if ($Command -eq '--create-admin') {
        $info.EnvironmentVariables['FARM_ADMIN_USERNAME'] = $Username
        $info.EnvironmentVariables['FARM_ADMIN_PASSWORD'] = $PasswordValue
    }
    try {
        $process = [System.Diagnostics.Process]::Start($info)
        $output = $process.StandardOutput.ReadToEndAsync()
        $errorOutput = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        # Only known status messages are displayed here; never print raw runtime/SQL exceptions.
        [void]$output.GetAwaiter().GetResult(); [void]$errorOutput.GetAwaiter().GetResult()
        return $process.ExitCode
    } finally {
        $info.EnvironmentVariables.Remove('FARM_ADMIN_PASSWORD')
        if ($process) { $process.Dispose() }
    }
}
$status = Invoke-AdminCommand '--check-admin' '' ''
if ($status -eq 10) { Write-Host 'Active administrator already exists. Skipping creation; existing login/password are unchanged.'; return }
if ($status -eq 14) { throw 'Accounts exist but no usable active administrator was found. Recover access through a trusted administrator; the installer will not replace existing accounts.' }
if ($status -ne 0) { throw 'Cannot check administrator. Verify SQL connection, database permissions and migration database-update.sql before continuing.' }
if ($CheckOnly) { Write-Host 'No accounts exist. A first administrator is required.'; return }
do {
    $username = Read-Host 'Administrator username (3-50 ASCII letters, numbers, dot, underscore, dash)'
    if ($username -notmatch '\A[a-zA-Z0-9._-]{3,50}\z') { Write-Host 'Invalid username. Please try again.' }
} while ($username -notmatch '\A[a-zA-Z0-9._-]{3,50}\z')
do {
    $password = Read-Host 'Temporary password (12-128 characters, including letters and numbers)' -AsSecureString
    $plain = (New-Object System.Net.NetworkCredential('', $password)).Password
    $valid = $plain.Length -ge 12 -and $plain.Length -le 128 -and $plain -match '\p{L}' -and $plain -match '\p{Nd}'
    if (-not $valid) { Write-Host 'Password must have 12-128 characters with letters AND numbers. Please enter it again.'; $password.Dispose(); $plain = $null }
} while (-not $valid)
try { $status = Invoke-AdminCommand '--create-admin' $username $plain }
finally { $plain = $null; $password.Dispose() }
switch ($status) {
    0 { Write-Host 'Administrator created. Change the temporary password at first login.' }
    10 { Write-Host 'Another administrator already exists. Skipping creation; passwords were not changed.' }
    11 { throw 'Username/password validation failed. Password must have 12-128 characters with letters and numbers.' }
    14 { throw 'Accounts exist without a usable active administrator. Existing accounts were not changed.' }
    default { throw 'Cannot create administrator. Verify SQL connection, permissions and database-update.sql migrations.' }
}
