# Phase 5 Test Filter Audit

## Scope

This slice adds a simple name filter to the custom C# test runner.

Default behavior is unchanged:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build
```

still runs the full suite.

Focused runs can now use:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --filter godot
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --filter=lockstep
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- chaos
```

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- The filter only controls which registered tests execute.
- Test order stays deterministic.
- Filtering is case-insensitive and matches against test names.

## Verification

Tests cover:

- Separated `--filter value` parsing.
- Inline `--filter=value` parsing.
- Positional filter parsing.
- Case-insensitive matching.
- Empty-filter full-run behavior.
