param(
    [string]$Filter = "",
    [switch]$List,
    [switch]$FailFast,
    [switch]$Help,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

if (-not $NoBuild) {
    dotnet build tests\RtsGame.Tests.csproj --no-restore
}

$runnerArgs = @("run", "--project", "tests\RtsGame.Tests.csproj", "--no-build")
$testArgs = @()

if ($Help) {
    $testArgs += "--help"
}

if ($List) {
    $testArgs += "--list"
}

if ($Filter.Length -gt 0) {
    $testArgs += "--filter"
    $testArgs += $Filter
}

if ($FailFast) {
    $testArgs += "--fail-fast"
}

if ($testArgs.Count -gt 0) {
    $runnerArgs += "--"
    $runnerArgs += $testArgs
}

dotnet @runnerArgs
