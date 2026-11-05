# インストール済み SDK を使用する。リポジトリの global.json は変更しない。
$ErrorActionPreference = 'Stop'
$scenarioArguments = $args
$projectPath = Join-Path $PSScriptRoot 'InteractionFlow.Standard.ConsoleSystem.ManualTests.csproj'
$applicationPath = Join-Path $PSScriptRoot 'bin\Debug\net9.0\InteractionFlow.Standard.ConsoleSystem.ManualTests.dll'

$sdkVersions = @(& dotnet --list-sdks | ForEach-Object {
    if ($_ -match '^(\d+\.\d+\.\d+) \[' -and ([version]$Matches[1]).Major -ge 9) {
        $Matches[1]
    }
})
if ($sdkVersions.Count -eq 0) {
    throw '.NET SDK 9 or later is required.'
}

$preferredVersions = @($sdkVersions | Where-Object { ([version]$_).Major -eq 9 })
if ($preferredVersions.Count -eq 0) { $preferredVersions = $sdkVersions }
$sdkVersion = $preferredVersions | Sort-Object { [version]$_ } -Descending | Select-Object -First 1
$runnerPath = Join-Path ([IO.Path]::GetTempPath()) ('InteractionFlow.ConsoleManualTests.' + [guid]::NewGuid())
$sdkConfigurationPath = Join-Path $runnerPath 'global.json'
$previousRollForward = [Environment]::GetEnvironmentVariable('DOTNET_ROLL_FORWARD', 'Process')

New-Item -ItemType Directory -Path $runnerPath | Out-Null
try {
    @{ sdk = @{ version = $sdkVersion } } | ConvertTo-Json | Set-Content -LiteralPath $sdkConfigurationPath
    Write-Host "Building manual tests with SDK $sdkVersion"
    Push-Location -LiteralPath $runnerPath
    try {
        & dotnet build $projectPath --configuration Debug --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw 'Manual test project build failed.' }
    }
    finally {
        Pop-Location
    }

    # net9.0 を維持し、ランタイム9が未導入の場合のみ新しいメジャーのランタイムを使用する。
    $hasRuntime9 = @(& dotnet --list-runtimes | Where-Object { $_ -match '^Microsoft.NETCore.App 9\.' }).Count -gt 0
    if (!$hasRuntime9) {
        [Environment]::SetEnvironmentVariable('DOTNET_ROLL_FORWARD', 'Major', 'Process')
        Write-Host 'Runtime 9 is unavailable; using a newer installed runtime.'
    }

    & dotnet $applicationPath @scenarioArguments
    $applicationExitCode = $LASTEXITCODE
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ROLL_FORWARD', $previousRollForward, 'Process')
    # 作成した一ファイルと、空になった一時ディレクトリだけを削除する（再帰削除はしない）。
    if (Test-Path -LiteralPath $sdkConfigurationPath) { Remove-Item -LiteralPath $sdkConfigurationPath }
    Remove-Item -LiteralPath $runnerPath
}

exit $applicationExitCode
