# Architecture Load Audit (Phase 8C)

## Scope

Audit whether the current deterministic architecture can realistically carry the full target envelope:

- 6 players
- 200+ pop each
- ~1200 active units
- up to ~720 villagers

This audit is classification-only. No gameplay behavior changes are included.

## Current Baseline

- Movement/Gather V2: default-on, accepted as prototype-ready
- Legacy movement/gather toggles: retained for rollback/parity
- HUD foundation: implemented via read-only snapshot/frame DTO path
- RtsClientRoot split: complete
- Test-suite split: complete
- Sim-first regression workflow: mandatory
- Gate status at audit time:
  - tests `472/472 PASS`
  - chaos-v4 `5000 ticks`, `desync=False`, `invariant_failures=0`

## Hot-Path Audit

### 1) Movement Loops

Assessment: **Medium risk**

Strengths:

- deterministic conflict ordering
- bounded retry/no-progress behavior
- alternate-step deterministic fallback
- pressure scenarios and chaos coverage active

Risks:

- high-density conflict chains still sensitive to budget pressure
- additional combat/bot path demand can amplify path query load

Status: **Accepted for prototype**, track before full-game launch.

---

### 2) Gather/Deposit Loops

Assessment: **Medium risk**

Strengths:

- assigned-vs-current node semantics explicit
- fallback reasons explicit
- stale-slot timeout and retarget cadence bounded

Risks:

- extreme TC corridor contention remains complex under mixed workloads

Status: **Accepted for prototype**, keep scenario packs as regression gate.

---

### 3) Reservation Checks and Ownership

Assessment: **Low to Medium risk**

Strengths:

- centralized reservation service
- deterministic shortlist and tie-break path
- timeout eviction is per-unit/per-slot

Risks:

- service must remain single mutation authority; bypass risk rises with new features

Status: **Low risk if contract preserved**.

---

### 4) Spatial Index Usage

Assessment: **Medium risk**

Strengths:

- tick-scoped warm rebuild contract is explicit
- occupancy/blocker queries routed through index service path

Risks:

- rebuild and query cost can rise with larger entity counts and more systems consuming lookups

Status: **Accepted for prototype**, monitor during bot/network phase.

---

### 5) Path Query Budget

Assessment: **Medium risk**

Strengths:

- bounded shortlist strategy exists
- pressure budget tests exist

Risks:

- adding AI and heavier combat command churn can shift budget envelope

Status: **Needs follow-up issue before full game**.

---

### 6) Combat Targeting / Future Combat Risks

Assessment: **Medium risk**

Strengths:

- deterministic single-target and siege/aoe baseline
- replay/lockstep tests are present

Risks:

- future advanced targeting/formation logic can introduce order-sensitive complexity

Status: **Accepted for prototype**, pre-scope deterministic target priority contracts before expansion.

---

### 7) Visibility/Fog Risks

Assessment: **Low to Medium risk**

Strengths:

- visibility is simulation-owned and tested
- snapshot read-only boundary is enforced

Risks:

- high-unit visibility recompute budgets under larger matches should be profiled

Status: **Accepted for prototype**.

---

### 8) Bot/AI Loop Risks (Future)

Assessment: **High risk (future-facing)**

Why high:

- bot command generation can flood path/reservation systems
- AI cadence and command churn can hide deterministic starvation patterns

Needed before full game:

- strict bot command cadence budget contracts
- deterministic scenario packs with mixed AI load

Status: **Not acceptable for full game until addressed**.

---

### 9) Networking/Reconnect Risks (Future)

Assessment: **High risk (future-facing)**

Why high:

- reconnect semantics in lockstep environments are operationally sensitive
- desync diagnostics and recovery tooling requirements increase with player count

Needed before full game:

- reconnect v1 contract tests
- operational playbook for desync/reconnect handling

Status: **Not acceptable for full game until addressed**.

## Risk Matrix Summary

### High Risk

- Bot/AI command loop scaling
- Networking/reconnect operational correctness

### Medium Risk

- Movement conflict density at full load
- Gather/deposit congestion under mixed workloads
- Spatial index and path-budget headroom
- Future combat targeting expansion

### Low Risk

- Determinism/replay core contract integrity (current baseline)
- Presentation read-only boundary enforcement

### Accepted for Prototype

- Current movement/gather stability profile
- Current combat/siege baseline
- Current visibility + snapshot pipeline

### Requires Follow-Up Before Full Game

- AI load contracts + scenarios
- Reconnect/operational networking contracts
- Additional movement/path budget profiling at target-scale mixed scenarios

## Recommended Follow-Up Issue Pack

1. `SCALE-01`: Mixed-load path/reservation budget profiling at 1200 active units.
2. `SCALE-02`: Bot command cadence contract + deterministic stress scenarios.
3. `NET-01`: Reconnect v1 contract tests for lockstep sessions.
4. `VIS-01`: Visibility budget profiling under full-load scenarios.
5. `COMBAT-01`: Deterministic targeting priority contract for future combat depth.

## Final Verdict

The architecture is **strong enough for prototype progression** and has the right deterministic boundaries.

For full-game readiness, the main remaining risk is not base determinism correctness; it is **operational scale behavior** under bots/networking/full mixed-load loops.

In short:

- Prototype target: **Yes, structurally supported**.
- Full-game target: **Supported in direction, but requires the follow-up issue pack above before claiming readiness**.
