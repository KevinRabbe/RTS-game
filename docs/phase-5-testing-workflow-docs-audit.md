# Phase 5 Testing Workflow Docs Audit

## Scope

This slice adds a short testing workflow document and links it from the root README.

## Architecture Notes

- No simulation behavior changed.
- No test runner behavior changed.
- No `GameState`, `TickRunner`, checksum, replay, lockstep, or chaos scenario code changed.

## Documentation Contract

The testing workflow documents:

- Full-suite build and run commands.
- Focused test filter examples.
- Suggested local iteration loop.
- Current architecture guardrail tests.

## Verification

This is documentation-only. Verification is a git diff check plus the previously passing test runner slice.
