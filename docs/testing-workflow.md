# Testing Workflow

This project uses a small custom C# test harness in `tests/Program.cs`.

The full suite is the authoritative local gate:

```powershell
dotnet build tests\RtsGame.Tests.csproj --no-restore
dotnet run --project tests\RtsGame.Tests.csproj --no-build
```

Equivalent local helper:

```powershell
.\scripts\test.ps1
```

The full run covers deterministic simulation, replay, lockstep, presentation boundaries, Godot bridge helpers, and chaos stress smoke scenarios.

For long-run deterministic pressure checks, run headless stress scenarios explicitly:

```powershell
dotnet run --project src\tools\Headless\RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v4 --ticks 5000 --seed 77
dotnet run --project src\tools\Headless\RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v5 --ticks 5000 --seed 77
```

GitHub Actions uses the same build and full-suite commands in `.github/workflows/ci.yml`.

## Focused Runs

The runner supports a deterministic name filter for faster local iteration:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --filter godot
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --filter=lockstep
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- chaos
```

Equivalent local helper:

```powershell
.\scripts\test.ps1 -Filter godot
```

Filtering is case-insensitive and matches against registered test names.

No arguments means all tests run.

To list registered test names without executing them:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --list
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --list --filter godot
```

Equivalent local helper:

```powershell
.\scripts\test.ps1 -List -Filter godot -NoBuild
```

To stop on the first failure during a focused or full run:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --filter godot --fail-fast
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --fail-fast
```

Equivalent local helper:

```powershell
.\scripts\test.ps1 -Filter godot -FailFast -NoBuild
```

To print the runner options:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --help
```

Equivalent local helper:

```powershell
.\scripts\test.ps1 -Help -NoBuild
```

## Suggested Loop

For small presentation or tooling slices:

1. Build once.
2. Run a focused filter for the touched area.
3. Run the full suite before committing.

For simulation, lockstep, replay, checksum, cleanup, ownership, or command validation changes:

1. Run the full suite.
2. Check the relevant replay or lockstep tests.
3. Check the relevant chaos scenario smoke tests.
4. Commit only after the full suite is clean.

## Current Guardrails

The suite includes source-level architecture guards:

- Simulation project must not reference presentation.
- Simulation source must not reference presentation or Godot.
- Godot bridge helpers must not use Godot engine API types.
- Godot client script must not reference simulation core directly.
- Godot client script must not switch on raw primitive kind values.

These tests are intentionally boring. They are there to stop architecture drift before it becomes expensive.
