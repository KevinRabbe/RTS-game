# Phase 5 Minimal Visual Prototype Audit

Status: Green

## Added Boundary

`VisualFrameBuilder` converts read-only snapshots into presentation primitives.

The output is deliberately ugly and mechanical:

- Unit squares.
- Building rectangles.
- Larger Capital rectangles.
- Wall rectangles.
- Trade route lines.
- Health bars.
- Fog overlay marker.

## Architecture Rules

- No simulation behavior changed.
- No `src/sim` files changed.
- No Godot dependency was added to simulation.
- Visual primitives are presentation data only.
- Visual frame building does not mutate checksum-covered state.
- Trade route lines are emitted only when both endpoints are visible in the snapshot.

## Godot Direction

Godot should consume `GameSnapshot` and `VisualFrame` output.

Godot remains responsible for:

- Drawing.
- Camera.
- Selection UI.
- Input collection.
- Visual interpolation.

Godot remains forbidden from:

- Mutating `GameState`.
- Owning gameplay authority.
- Running simulation physics.
- Using delta time for gameplay logic.

## Verification

- Full test suite: 141 tests, 0 failures.
- Visual frame emits core prototype primitives.
- Capital visual primitive is larger than normal Town Center.
- Visible trade route produces route line.
- Visual frame building does not mutate checksum.
- ChaosV1-V4 smoke tests remain green.
