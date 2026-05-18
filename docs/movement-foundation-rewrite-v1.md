# Movement Foundation Rewrite v1 (Scale-Safe)

## Scope Freeze
- Rewrite window focuses on movement/traffic only.
- Target scale is mandatory: 6 players, 1200+ active units.
- HUD/polish/features outside movement are blocked until movement gates are green.

## Stable Contracts
- `IPathQueryService`: all path queries and path-cost scoring go through this service.
- `ITrafficReservationService`: all reservation writes/releases/revalidations go through this service.
- `ISpatialIndexService`: authoritative blocker/occupancy/reservation/ring queries.
- `IMovementProgressPolicy`: deterministic no-progress and recovery policy.

No hot-path bypasses are allowed.

## Step A (v1) Delivery
- Add checksum-covered movement-v2 state on units.
- Keep v2 solver feature-flagged and disabled by default.
- Keep current gameplay semantics while contract boundaries are hardened.
- Add guard tests for no-bypass rules and checksum coverage.

## Step B (v2+) Upgrade Path
- Enable deterministic continuous kinematics and steering in `MovementSolverV2`.
- Add corridor commitment and retarget cadence/hysteresis.
- Add congestion-aware avoidance upgrades through service implementations only.
- No API rewrites; upgrades must remain additive.

## Merge Gates
- Deterministic replay/checksum stability.
- No stacking.
- No duplicate final-purpose reservations.
- No stale move reservation on idle units.
- No endless `MovingTo*` loops under scenario pressure.
- Path query and reservation churn budgets must remain bounded.
