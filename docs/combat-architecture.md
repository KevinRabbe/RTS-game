# Scalable Combat Hotspot Architecture (Phase 9A.0)

## Purpose

Define a deterministic combat foundation that scales from small fights to Wonder-style hotspot battles without rewrite.

Target envelopes:

- 20 vs 20 normal engagements
- 150 vs 150 large army engagements
- Hotspot objective fights with 3+ attackers converging
- 300+ knights or 600+ mixed units in one area
- Full match envelope stays 6 players / ~1200 active units

This document is architecture-only. No gameplay behavior changes are introduced here.

## 1) Combat Command Model

### Explicit Attack Command (v1)

- `AttackCommand` remains explicit and target-id based.
- Payload: selected unit ids + `AttackTargetEntityId`.
- Command is validated deterministically at enqueue tick.
- Mixed selection semantics stay deterministic (invalid units ignored or rejected by stable policy).

### Out-of-range semantics

- Combat does not pathfind.
- If attacker is out of range, movement owns approach and slot acquisition.
- Attack intent remains active while movement positions unit.

### Target invalidation semantics

- If target dies/invalidates, explicit attack clears or transitions by a bounded deterministic rule.
- No unbounded auto-retarget loop in v1.
- Attack-move/auto-acquire is a separate future command model.

## 2) Combat State Model

Per-unit persistent combat state (deterministic, checksum-covered where persistent):

- `AttackTargetId`
- `AttackCooldownTicksRemaining`
- `AttackRange`
- `AttackDamage`
- `AttackPhase` (only if phase adds deterministic value)
- Optional bounded diagnostics:
  - `LastAttackAttemptTick`
  - `LastAttackBlockReason`

Resolution contracts:

- Damage application is deterministic and ordered.
- Death is marked in combat/death-mark stage.
- Cleanup remains centralized in `CleanupSystem` only.

## 3) Movement Integration Contracts

- Combat does not implement pathfinding and does not bypass movement.
- Combat requests deterministic desired attack positions (slots/rings) around target.
- Movement service resolves travel/traffic/reservations toward those positions.
- No combat-specific path hacks or ad hoc direct teleports.

## 4) Attack Slot / Ring Model

### Primary design

- Melee attackers reserve deterministic attack slots on an interaction ring around target footprint.
- Only bounded direct slots can be occupied concurrently.
- Overflow attackers wait/pressure in outer rings instead of collapsing onto one tile.
- Large footprints (castles/wonders/objectives) expose larger contest surfaces.

### Deterministic rules

- Slot candidate generation order is deterministic.
- Slot assignment tie-breakers are deterministic (unit id / owner index / stable order).
- Reservation ownership is explicit; stale reservations timeout with deterministic cadence.

### Scale rationale

This prevents 300 attackers from all targeting a single tile and stabilizes hotspot traffic under load.

## 5) Targeting and Query Model

- Explicit targets are O(1)-style lookup by id.
- No per-tick `units x enemies` scanning.
- Future auto-target/attack-move uses:
  - spatial indexes
  - bounded query cadence
  - deterministic tie-break contracts

## 6) Combat Resolution Model

- Cooldowns are integer tick-based only.
- Damage ordering is stable and deterministic.
- Death marking and cleanup remain separated.
- Target invalidation policy is explicit and bounded.
- No random crits and no nondeterministic float behavior.

## 7) Hotspot Scale Rules

Hard constraints for implementation slices:

- No O(units x enemies) loops per tick in hotspot paths.
- No per-tick path query spam from combat intent churn.
- No unbounded retarget loops after deaths.
- No unordered iteration in target selection or damage application.
- Under extreme concentration, visual quality may degrade, but simulation must not:
  - stack incorrectly
  - desync
  - collapse with runaway CPU

## 8) Required Scenario Pack (Before Combat Expansion)

### Basic

1. 1 attacker vs 1 target
2. 10 attackers vs 1 target
3. out-of-range attacker moves into range then attacks
4. target death clears attacker intent

### Scale

5. 50 vs 50 melee pressure
6. 150 vs 150 melee pressure
7. hotspot: 1 defender vs 3 attackers around objective
8. 300 knight pressure test
9. 600 mixed-unit objective pressure test

### Determinism

10. replay determinism across combat scenarios
11. lockstep agreement across combat scenarios
12. bounded path-query and reservation-churn checks

## 9) Non-Goals for First Combat Slice

- No formations
- No attack-move implementation (architecture stub only allowed)
- No broad auto-acquire scanning
- No armor/counter-depth system
- No balance tuning pass
- No projectile simulation expansion (unless already covered safely)
- No bot/networking feature expansion

## Planned Implementation Sequence

1. **9A.1** Explicit attack target slice
2. **9A.2** Attack slot/ring integration
3. **9A.3** Combat hotspot pressure scenarios
4. **9A.4** Combat HUD/debug status

Gate required after each slice:

1. `dotnet build GodotClient\RtsGame.GodotClient.csproj --no-restore`
2. `dotnet build tests\RtsGame.Tests.csproj --no-restore`
3. `dotnet run --project tests\RtsGame.Tests.csproj --no-build -- --fail-fast`
4. `dotnet run --project src\tools\Headless\RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v4 --ticks 5000 --seed 77`

---

# Scalable Attack-Move and Auto-Target Architecture (Phase 9C.0)

## Purpose

Extend the combat foundation with attack-move and auto-targeting without introducing hotspot CPU blowups or deterministic drift.

This section is planning-only. No gameplay behavior is changed by this document.

Target envelopes remain:

- 20 vs 20 normal engagements
- 150 vs 150 large engagements
- hotspot objective fights with 300+ knights / 600+ mixed units
- 6 players / ~1200 active units

## 1) Attack-Move Command Model

### New command type

- Add `AttackMoveCommand` with ground destination tile/vector payload.
- Command stores long-term intent distinct from explicit `AttackCommand`.
- Explicit attack remains target-id authoritative and separate.

### Intent behavior

- Unit advances toward attack-move destination through movement-owned pathing.
- If a valid hostile enters acquisition envelope, unit may pause destination progress and enter combat positioning.
- After target death/invalid release, unit resumes attack-move destination if command intent is still active.
- Attack-move clears only by deterministic command replacement/cancel rules.

## 2) Auto-Target Query Model

### Query source

- Auto-target acquisition must use spatial index/query service only.
- No global enemy scans and no per-unit full-enemy loops.

### Bounded cadence

- Acquisition runs on bounded cadence (`N` tick window), not every tick for every unit.
- Re-acquisition cadence is deterministic and unit-state-driven.
- Optional per-player/per-region budget may cap acquisitions per tick.

### Stable ordering contract

When multiple candidates exist, resolve with deterministic ordering:

1. shortest deterministic distance metric
2. optional deterministic category/threat tier (future-safe, not required in v1)
3. lowest target entity id

No unordered set iteration is permitted in candidate selection.

## 3) Target Retention and Release Model

- Retain current auto-target while target is alive, valid hostile, and within leash/range policy.
- Release if dead, invalid ownership, or outside deterministic leash policy.
- Reacquire only on bounded cadence to prevent retarget thrash.
- Preserve cooldown/attack-slot state contracts from 9A/9B.

## 4) Movement Integration Contracts

- Movement continues to own all pathing and congestion behavior.
- Combat/attack-move requests desired positioning only (destination path or attack slot/ring).
- When auto-target is active, unit transitions to attack-slot positioning flow.
- On release, unit deterministically returns to attack-move destination progression.
- No combat-side pathfinder or direct position mutation.

## 5) Hotspot Scale Rules

Hard constraints for implementation:

- No O(units x enemies) targeting loops.
- Bounded acquisitions per tick (global or scoped budget).
- Bounded reservation churn under death/retarget storms.
- Deterministic overflow behavior when budget is exhausted (defer, do not thrash).
- In dense hotspots, visual imperfection is acceptable; desync/CPU runaway is not.

## 6) Required Scenario Pack for 9C

1. one unit attack-moves past enemy and acquires target
2. unit resumes attack-move destination after target death
3. 10 units attack-move through enemy cluster
4. 50 vs 50 attack-move pressure
5. 150 vs 150 attack-move pressure
6. 3-player hotspot attack-move pressure
7. acquisition-budget saturation test (bounded fallback, no thrash)
8. replay determinism for attack-move scenarios
9. lockstep determinism for attack-move scenarios

## 7) Non-Goals for First Attack-Move Implementation

- no formations
- no smart focus-fire heuristics
- no advanced threat scoring
- no kiting/patrol/hold-position systems
- no projectile rewrite
- no balance tuning
- no bot/networking feature expansion

## Planned 9C Implementation Sequence

1. **9C.1** `AttackMoveCommand` + persistent state only (no auto-target yet)
2. **9C.2** bounded indexed auto-target acquisition
3. **9C.3** resume-after-kill/invalidation behavior
4. **9C.4** attack-move pressure scenario pack
5. **9C.5** HUD/command feedback for attack-move
