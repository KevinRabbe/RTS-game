# Phase 6.5 Current Checkpoint

**Branch:** `phase-6-visual-placeholder-pipeline`

## Latest Known Commits
- `e28079d`: Add DryArabiaTest01 default local 1v1 map
- `73f892d`: Improve Town Center placement preview and camera panning
- `eb82efd`: Clamp Godot camera to playable map bounds

## Current Status
- **Current Phase:** Phase 6.5 Core Action Playability Verification
- **Current Goal:** Ensure existing core RTS actions (selection, movement, construction, production, gathering) are intuitive and fully playable from the Godot client.

### Completed Features
- [x] **DryArabiaTest01:** Default map for F1 local 1v1.
- [x] **TC Placement Preview:** Visual ghost with green/red feedback (deterministic safe).
- [x] **Mouse Edge Pan:** Standard RTS camera movement.
- [x] **Camera Bounds:** Clamping to prevent drifting outside the playable area.

### Next Planned Steps
1. **Phase6Pack Art Mapping:** Wire up placeholder PNGs once asset cleanup is finalized.
2. **Core Action Verification:** Verify end-to-end loops (Gather -> Deposit -> Train -> Move -> Attack).
3. **RtsClientRoot Refactor:** Planned split into dedicated controllers (Input, Camera, Selection).

## Constraints & Forbidden Areas (Current Phase)
- No Bots (AI) implementation.
- No Networking / Multiplayer setup.
- No New Units or gameplay systems beyond the existing simulation core.
- No Final Production UI (focus remains on functional debug/playtest HUD).

## Manual Testing
A detailed test flow is documented in `docs/phase-6-5-dry-arabia-manual-playtest.md`.

**Next Task:** Phase 6.5c Art Mapping Completion after PNG cleanup.
