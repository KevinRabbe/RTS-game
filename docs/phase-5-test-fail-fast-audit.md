# Phase 5 Test Fail Fast Audit

## Scope

This slice adds a `--fail-fast` mode to the custom C# test runner.

The command stops execution after the first failed selected test:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --filter godot --fail-fast
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --fail-fast
```

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- Default behavior still runs every selected test.
- `--fail-fast` is compatible with normal full runs and filtered runs.
- Filter parsing skips runner flags so argument order stays predictable.

## Verification

Tests cover:

- Fail-fast flag detection.
- Fail-fast combined with filters.
- Filter parsing when `--fail-fast` appears before `--filter`.

Manual verification covers:

- Focused runner test execution.
