# Phase 5 CI Workflow Audit

## Scope

This slice adds a minimal GitHub Actions workflow for the deterministic test gate.

The workflow runs on push and pull request events:

1. Checkout.
2. Setup .NET.
3. Build the test project.
4. Run the custom deterministic test harness.

## Architecture Notes

- No simulation behavior changed.
- No test runner behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- The workflow does not require Godot.
- The workflow runs the same full test harness used locally.

## Verification

Local verification should still use:

```powershell
dotnet build tests\RtsGame.Tests.csproj --no-restore
dotnet run --project tests\RtsGame.Tests.csproj --no-build
```

The workflow itself will execute once pushed to GitHub.
