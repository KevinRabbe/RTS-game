# Phase 5 Test Help Audit

## Scope

This slice adds `--help` / `-h` mode to the custom C# test runner.

The command prints runner usage without executing tests:

```powershell
dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --help
```

## Architecture Notes

- No simulation behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- Default no-argument behavior still runs the full suite.
- Help mode exits before listing or running tests.

## Verification

Tests cover:

- Long help flag detection.
- Short help flag detection.
- Normal filter mode remaining distinct from help mode.

Manual verification covers:

- Help output.
- Focused runner test execution.
