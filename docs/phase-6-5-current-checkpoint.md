# Phase 6.5 Current Checkpoint

**Branch:** `phase-6-visual-placeholder-pipeline`

## Current Status
- **Current Phase:** Phase 6.5 stabilization and verification
- **Current Goal:** keep local Godot playability stable while preserving deterministic sim contracts.

### Recently Completed
- [x] DryArabia default local startup (1v1 and 6-player local flow).
- [x] Core action routing: move, gather, assign build, attack, train, trade route.
- [x] Sprite/primitive toggle pipeline (`F9`) with fallback diagnostics.
- [x] Multi-villager movement reliability hardening (`#33`).
- [x] Worker reservation/no-progress lifecycle stabilization (`#29`).
- [x] Full deterministic gate remains green (`tests=462 failed=0`, ChaosV4 5000 ticks clean).

### Next Planned Steps
1. Phase 6.5 manual playability smoke rerun and notes refresh.
2. Focused `RtsClientRoot` controller split (input/camera/selection extraction only, no gameplay changes).
3. Keep scenario-first regression discipline for any further reliability bug.

## Constraints & Forbidden Areas (Current Phase)
- No bots (AI) implementation.
- No networking/multiplayer transport changes.
- No new gameplay systems beyond the current simulation scope.
- No final production UI pass; keep debug/playtest HUD oriented.

## Manual Testing
A detailed test flow is documented in `docs/phase-6-5-dry-arabia-manual-playtest.md`.
