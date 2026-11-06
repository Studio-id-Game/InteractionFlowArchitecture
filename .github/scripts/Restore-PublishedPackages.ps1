param(
    [Parameter(Mandatory)]
    [string] $Project,

    [Parameter(Mandatory)]
    [string] $Source
)

$ErrorActionPreference = 'Stop'
# Restore failures are expected while NuGet.org validates the published packages.
$PSNativeCommandUseErrorActionPreference = $false
$timer = [System.Diagnostics.Stopwatch]::StartNew()
$timeout = [TimeSpan]::FromMinutes(60)
$interval = [TimeSpan]::FromMinutes(5)
$maxAttempts = 13

for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    Write-Host "Restore attempt $attempt/$maxAttempts"
    & dotnet restore $Project --source $Source --no-cache --force
    $restoreExitCode = $LASTEXITCODE
    if ($restoreExitCode -eq 0) {
        exit 0
    }

    $remaining = $timeout - $timer.Elapsed
    if ($attempt -eq $maxAttempts -or $remaining -le [TimeSpan]::Zero) {
        break
    }

    $delaySeconds = [Math]::Min($interval.TotalSeconds, $remaining.TotalSeconds)
    Write-Host "Packages are not yet restorable. Retrying in $([Math]::Ceiling($delaySeconds)) seconds."
    Start-Sleep -Seconds $delaySeconds
}

throw "Packages could not be restored within the 60-minute retry window (last restore exit code: $restoreExitCode). See the restore output above."
