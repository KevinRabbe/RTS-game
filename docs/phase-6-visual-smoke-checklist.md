# Phase 6 Visual Smoke Test Checklist

Purpose: verify the Phase 6 sprite-render pipeline works without changing gameplay/simulation behavior.

Date:
Tester:
Build/Commit:

## Startup and Session Flow

- [ ] Godot launches successfully.
- [ ] `F1` starts local 1v1.
- [ ] `F6` starts local 6-player FFA.

## Render Mode and HUD

- [ ] `F9` toggles primitive/sprite rendering.
- [ ] HUD shows current render mode.

## Sprite Coverage

- [ ] Sprite rendering works for Villager.
- [ ] Sprite rendering works for Infantry.
- [ ] Sprite rendering works for Scout/Cavalry-style unit.
- [ ] Sprite rendering works for TradeCart.
- [ ] Sprite rendering works for Capital.
- [ ] Sprite rendering works for Wall.
- [ ] Missing sprites fall back to primitives.

## Interaction and Camera

- [ ] Selection still works.
- [ ] Selected units/buildings are clearly visible (selection ring/outline is obvious).
- [ ] Right-click movement still works.
- [ ] Unit movement is visually trackable.
- [ ] Gather/build/attack interactions still work.
- [ ] Camera movement still works.
- [ ] Debug overlay does not block central gameplay readability.

## Sign-off

- [ ] Visual smoke test passed end-to-end.
- Notes:
