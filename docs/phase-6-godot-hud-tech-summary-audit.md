# Phase 6.3 Godot HUD Tech Summary Audit

## Scope

This slice improves in-match HUD readability for tech/testing state:

- research queue display includes remaining queue count suffix
- completed tech display includes total completed count suffix
- modifier display includes additional modifier count suffix
- HUD now displays rejected command counter from match DTO

## Boundary

- Presentation-only changes (`GodotBridge` + tests).
- No simulation rule, checksum logic, or command validation behavior changes.

## Coverage

Added/updated tests for:

- queued research count suffix
- modifier count suffix
- completed tech count suffix
- rejected command count HUD text

Full suite passes, including replay, lockstep, and chaos stress tests.
