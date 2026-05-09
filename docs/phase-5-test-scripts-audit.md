# Phase 5 Test Scripts Audit

## Scope

This slice adds a small PowerShell wrapper for the custom C# test runner.

The helper lives at:

```powershell
scripts\test.ps1
```

## Architecture Notes

- No simulation behavior changed.
- No test runner behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.
- The script only forwards arguments to the existing test harness.

## Usage

```powershell
.\scripts\test.ps1
.\scripts\test.ps1 -Filter godot
.\scripts\test.ps1 -List -Filter godot -NoBuild
.\scripts\test.ps1 -Filter godot -FailFast -NoBuild
.\scripts\test.ps1 -Help -NoBuild
```

## Verification

Manual verification covers:

- Help forwarding.
- Filter forwarding.
