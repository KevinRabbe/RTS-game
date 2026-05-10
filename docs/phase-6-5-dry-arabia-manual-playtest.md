# DryArabia Phase 6.5 Manual Playtest Checklist

This document outlines the manual verification steps for the Phase 6.5 gameplay experience on the DryArabiaTest01 map.

## Test Procedure

### Startup & Camera
- [ ] Press **F1** to start DryArabiaTest01 (local 1v1).
- [ ] Verify **camera edge-pan** works by moving mouse to screen edges.
- [ ] Verify **camera bounds**: Camera should stay near the map area and not drift infinitely away.
- [ ] Verify **middle-mouse drag** camera panning works.

### Town Center Placement
- [ ] Press **C** to enter TC placement mode.
- [ ] Verify **placement ghost** follows mouse and turns green (valid) or red (invalid, e.g., over resources).
- [ ] Verify **left click** places the TC foundation.
- [ ] Verify **right click** or **Escape** cancels placement mode.

### Construction & Basic Economy
- [ ] **Select a villager** (Left click).
- [ ] **Assign to build** the TC foundation (Right click the foundation).
- [ ] Verify **TC completes** after builders work on it.
- [ ] Verify **Population** changes from `5/0` to `5/10` upon completion.
- [ ] **Gather Food** (Right click berry bushes).
- [ ] **Gather Wood** (Right click trees).
- [ ] **Gather Gold** (Right click gold mines).
- [ ] Verify **carrying resource** status appears in the selected unit status panel.
- [ ] Verify **deposit/stockpile** increase when villagers return resources to the TC.

### Production & Combat
- [ ] **Select Town Center**.
- [ ] Press **V** to train a villager.
- [ ] Press **I** to train an infantry unit.
- [ ] Verify **selected building** shows training queue and progress bar in debug UI.
- [ ] **Select infantry** and verify they can move (Right click ground).
- [ ] Verify **infantry can attack** an enemy target if one is reachable.

---

## Known Current Rough Spots
- **Placeholder Art:** Assets are not final and represent technical placeholders.
- **Asset Backgrounds:** Some transparency/background cleanup may be performed manually in later passes.
- **UI State:** Currently using debug/playtest UI overlays; final production UI is not yet implemented.
- **No Networking/Bots:** This phase is focused on local human playability and core simulation verification.
