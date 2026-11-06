# リポジトリの global.json で指定した SDK を使用する。
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'InteractionFlow.Standard.ConsoleSystem.ManualTests.csproj'
$applicationPath = Join-Path $PSScriptRoot 'bin\Debug\net10.0\InteractionFlow.Standard.ConsoleSystem.ManualTests.dll'

Push-Location -LiteralPath $PSScriptRoot
try {
    & dotnet build $projectPath --configuration Debug --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Manual test project build failed.' }

    & dotnet $applicationPath @args
    $applicationExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $applicationExitCode
