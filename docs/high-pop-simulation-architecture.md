# High-Pop Simulation Architecture

## Purpose

Define the architecture constraints that let the deterministic core scale from prototype reliability toward full-match load.

## Target Load Envelope

Design and evaluate simulation behavior for:

- 6 players
- 200+ pop each
- ~1200 active units
- up to ~720 villagers

This is an architecture target, not a promise that every frame is already production-optimized.

## Current Foundation

Already in place:

- Deterministic command-driven tick model (20 TPS)
- Replay/lockstep-first workflow
- Explicit reservation ownership for final-purpose slots
- Movement/gather V2 default-on with deterministic fallback behavior
- Bounded pressure scenarios (`30/50/120` workers, mixed 6-player pressure)
- Chaos stress suites (`chaos-v1`..`chaos-v5`)

## Core Contracts

### 1. Intent Retention Under Congestion

Temporary congestion does not erase long-term intent.
Workers and movers may wait/retry deterministically; they do not randomly clear objective state.

### 2. Deterministic Slot Ownership

Final-purpose slots (resource/dropoff/build/move destination) are single-owner unless explicitly allowed.
Tie-breaking is deterministic and stable.

### 3. Movement Conflict Rules

- Shared-destination conflict: deterministic winner.
- Swap conflict: both retain intent and retry.
- Stationary-block conflicts: bounded alternate selection or wait.

### 4. Gather Semantics

- `AssignedResourceNodeId` is sticky player intent.
- `CurrentResourceNodeId` may rotate for continuation/availability.
- Fallback reasons are explicit and checksum-covered.

### 5. Checksum Integrity

Persistent diagnostic movement/gather state is checksum-covered by design so drift surfaces as desync.

## Hot Path Notes

### Movement

Risk driver: repeated path checks + conflict resolution under dense crowds.
Current mitigation: shortlist candidates, deterministic alternates, no-progress gating, pressure tests.

### Gather/Deposit

Risk driver: repeated reservation churn near TC/resource corridors.
Current mitigation: stale slot cadence, timeout eviction, assigned-vs-current semantics, bounded retry policy.

### Reservation Service

Risk driver: slot contention under multi-worker/multi-front traffic.
Current mitigation: deterministic candidate sorting, path-cost shortlist cap, per-unit per-slot timeout eviction.

### Spatial Index

Risk driver: expensive blocker/occupancy checks if uncached.
Current mitigation: tick-scoped warm index rebuild + per-tick coherent query contract.

## Future Extensions (Non-Blocking for Prototype)

- Corridor-level lane preference improvements
- Additional bounded path budgets by scenario profile
- Bot-side command pacing contracts
- Visibility perf guardrails under large unit counts
- Reconnect/resync operational tooling for lockstep sessions

## Acceptance Boundary

Architecture is acceptable for prototype progression when:

- full deterministic test suite stays green,
- chaos-v4 remains desync-free,
- pressure windows remain bounded,
- no invariant regressions appear in worker/traffic scenarios.

Detailed risk classification and follow-up priorities are tracked in [architecture-load-audit.md](architecture-load-audit.md).
