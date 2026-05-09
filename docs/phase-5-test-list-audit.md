# Phase 5 Test List Audit

## Scope

This slice adds a `--list` mode to the custom C# test runner.

The command lists registered test names without executing them:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --list
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --list --filter godot
```

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- Default no-argument behavior still runs the full suite.
- Test listing follows the registered deterministic test order.
- Filtered listing uses the same case-insensitive name filter as normal focused runs.

## Verification

Tests cover:

- List-mode detection.
- Filter parsing in list mode.
- Normal filter mode remaining distinct from list mode.

Manual verification covers:

- Focused list output.
- Focused runner test execution.
