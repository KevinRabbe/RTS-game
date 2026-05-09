# Phase 0 Code Skeleton Blueprint

Phase 0 turns the architecture into executable foundation code without adding real gameplay complexity.

Goal:

- Prove deterministic tick execution.
- Prove replay from command stream.
- Prove checksum stability.
- Prove minimal two-peer lockstep behavior.
- Keep Simulation Core engine-agnostic.

Do not add units, combat, pathfinding, economy, rendering, or UI in Phase 0 except for placeholder data needed to validate the pipeline.

## Phase 0 Deliverables

1. Simulation module.
2. Deterministic math module.
3. Command model and command buffer.
4. Fixed tick runner.
5. Canonical state serialization for checksums.
6. Replay record and replay runner.
7. Minimal lockstep harness.
8. Headless tests.

## Recommended Folder Layout

```text
src/
  sim/
    Core/
      GameState.*
      GameRules.*
      TickRunner.*
      SimTypes.*
    Commands/
      ICommand.*
      CommandHeader.*
      CommandEnvelope.*
      CommandBuffer.*
      NoOpCommand.*
      CommandSerializer.*
    Data/
      EntityState.*
      PlayerState.*
      EconomyState.*
      PopulationState.*
      MapState.*
      VisibilityState.*
    Determinism/
      Fixed.*
      DeterministicRandom.*
      StableSort.*
      CanonicalWriter.*
    Systems/
      ISimSystem.*
      CommandValidationSystem.*
      CommandExecutionSystem.*
      CleanupSystem.*
      ChecksumSystem.*
    Replay/
      ReplayFile.*
      ReplayRecorder.*
      ReplayRunner.*
    Checksums/
      StateChecksum.*
      ChecksumHistory.*
  net/
    Lockstep/
      LockstepPeer.*
      LockstepSession.*
      InputDelay.*
      PeerCommandInbox.*
      DesyncReport.*
  tools/
    Headless/
      HeadlessSimRunner.*
      TwoPeerLockstepRunner.*
tests/
  sim/
    EmptyTickDeterminismTests.*
    CommandBufferTests.*
    ReplayTests.*
    ChecksumTests.*
    PlatformReproducibilityTests.*
  net/
    LockstepHarnessTests.*
```

Use the file extensions and project structure appropriate to the chosen language, but keep these ownership boundaries.

## Core Types

### Sim Scalar Types

Define simulation scalar aliases or wrappers.

Required:

```text
Tick: int32
PlayerIndex: int32
EntityId: int32
CommandSequence: uint32
ChecksumValue: uint64
```

Rules:

- IDs are never reused.
- Ticks never use wall-clock time.
- Serialized values use explicit byte order.
- Gameplay math does not use platform-dependent float behavior.

### Fixed-Point Type

Create a project-owned fixed-point wrapper before movement exists.

Minimum API:

```text
Fixed
  Raw: int64
  FromInt(value: int32): Fixed
  FromRatio(numerator: int32, denominator: int32): Fixed
  Add(a, b): Fixed
  Subtract(a, b): Fixed
  Multiply(a, b): Fixed
  Divide(a, b): Fixed
  Compare(a, b): int32
```

Phase 0 tests should prove basic operations are deterministic.

Do not use fixed-point for everything. Use integers where integers are enough.

### GameRules

`GameRules` is immutable match configuration.

Fields:

```text
GameRules
  RulesVersion: uint32
  TickRate: int32 = 20
  MaxPlayers: int32
  InputDelayTicks: int32
  ChecksumIntervalTicks: int32
```

`GameRules` is read by simulation but not mutated by systems.

### GameState

Phase 0 `GameState` should be intentionally sparse.

```text
GameState
  Tick: int32
  MatchSeed: uint64
  RngState: DeterministicRandomState
  EntityState: EntityState
  PlayerStates: PlayerStateContainer
  EconomyState: EconomyState
  PopulationState: PopulationState
  MapState: MapState
  VisibilityState: VisibilityState
  DebugCounters: SimDebugCounters
```

`DebugCounters` may exist only if it is deterministic and serialized into checksums when it can affect test expectations.

## Empty Sub-State Definitions

Create the modular sub-states immediately, even if mostly empty.

```text
EntityState
  NextEntityId: int32
  Units: List<Unit>
  Buildings: List<Building>
  EntityLookup: Dictionary<int32, EntityRef>

PlayerStateContainer
  Players: List<PlayerState>

PlayerState
  PlayerIndex: int32
  IsConnected: bool
  IsDefeated: bool

EconomyState
  PlaceholderVersion: uint32

PopulationState
  PlaceholderVersion: uint32

MapState
  MapSeed: uint64
  PlaceholderVersion: uint32

VisibilityState
  PlaceholderVersion: uint32
```

The placeholder fields prevent ambiguous empty serialization. Replace them as real data arrives.

## Command Model

All commands are wrapped in an envelope.

```text
CommandEnvelope
  Header: CommandHeader
  Payload: ICommand

CommandHeader
  Tick: int32
  PlayerIndex: int32
  Sequence: uint32
  CommandType: uint16
```

Phase 0 command types:

```text
NoOpCommand
DebugIncrementCounterCommand
```

`DebugIncrementCounterCommand` exists only to test command execution, ordering, replay, and lockstep. It should be removed or isolated from gameplay builds after real commands exist.

Command ordering for a tick:

1. Tick ascending.
2. PlayerIndex ascending.
3. Sequence ascending.
4. CommandType ascending.

Invalid commands must be rejected deterministically.

## Command Buffer

`CommandBuffer` stores commands by tick.

Required operations:

```text
Add(command)
GetCommandsForTick(tick)
HasAllRequiredPlayerInputs(tick, playerCount)
ClearBeforeTick(tick)
```

Rules:

- Returned commands must be in deterministic order.
- Internal maps or dictionaries may be used for lookup only if never iterated without sorting.
- Missing player input blocks lockstep advancement.
- A player with no action still submits `NoOpCommand` for the tick or tick range required by the lockstep harness.

## Tick Runner

`TickRunner` is the only normal way to advance simulation.

```text
TickRunner.AdvanceOneTick(GameState state, GameRules rules, CommandBuffer commands)
```

Phase 0 system order:

1. CommandValidationSystem
2. CommandExecutionSystem
3. CleanupSystem
4. ChecksumSystem

`AdvanceOneTick`:

1. Reads commands for `state.Tick`.
2. Validates commands.
3. Executes accepted commands.
4. Runs cleanup.
5. Computes checksum.
6. Increments `state.Tick`.

If the codebase prefers checksum after tick increment, choose once and document it. Do not mix both.

## Canonical Serialization

Checksums and replay validation must use canonical serialization.

Create `CanonicalWriter`.

Required methods:

```text
WriteInt32(value)
WriteUInt32(value)
WriteInt64(value)
WriteUInt64(value)
WriteBool(value)
WriteFixed(value)
WriteListCount(count)
```

Rules:

- Fixed byte order.
- No object memory layout hashing.
- No reflection-based field order unless explicitly controlled.
- Lists serialize in deterministic list order.
- Dictionaries serialize only through sorted explicit keys when absolutely required.

## Checksum

`StateChecksum` computes a `uint64` hash from canonical state bytes.

Phase 0 checksum includes:

- Rules version.
- Current tick.
- Match seed.
- RNG state.
- Entity state placeholder data.
- Player states.
- Economy state.
- Population state.
- Map state.
- Visibility state.
- Deterministic debug counters.

Do not include:

- Presentation state.
- Network connection state.
- Wall-clock time.
- Object addresses.
- Log messages.

## Replay

Replay file structure:

```text
ReplayFile
  FormatVersion: uint32
  RulesVersion: uint32
  MatchSeed: uint64
  PlayerCount: int32
  InitialRules: GameRules
  Commands: List<CommandEnvelope>
  OptionalChecksums: List<ReplayChecksum>
```

Replay runner:

```text
ReplayRunner.Run(replay): ReplayResult
```

Replay result:

```text
ReplayResult
  FinalTick: int32
  FinalChecksum: uint64
  ChecksumMismatches: List<ChecksumMismatch>
```

Phase 0 replay test:

- Record 100 ticks of `NoOpCommand`.
- Replay it twice.
- Final checksum must match both times.

## Minimal Lockstep Harness

Build a local headless lockstep harness before real networking.

```text
LockstepSession
  Rules
  Peers: List<LockstepPeer>
  CurrentTick
  InputDelayTicks
  SharedCommandBuffer
```

Each peer:

```text
LockstepPeer
  PlayerIndex
  LocalState
  Inbox
  ChecksumHistory
```

Harness behavior:

1. Each peer schedules commands for `currentTick + inputDelay`.
2. Commands are delivered through deterministic in-memory inboxes.
3. Session advances a tick only when all required player inputs exist.
4. Each peer advances its own local `GameState`.
5. Checksums are compared at configured intervals.
6. Any mismatch creates `DesyncReport`.

Phase 0 lockstep tests:

- Two peers run 500 ticks with only NoOp commands.
- Two peers run 500 ticks with deterministic debug counter commands.
- Missing command for one peer stalls both peers.
- Same commands delivered in different arrival order still produce matching checksums.

## Headless Runner

Create a simple headless executable or test utility.

Required modes:

```text
run-empty --ticks 10000 --players 2 --seed 1
run-replay --path replay-file
run-lockstep --ticks 1000 --players 2 --seed 1
```

The runner prints:

- Final tick.
- Final checksum.
- Command count.
- Desync status.

The runner must not require a renderer or game engine editor.

## Phase 0 Tests

### Empty Tick Determinism

Run the same empty simulation twice.

Expected:

- Same final tick.
- Same final checksum.

### Fresh Process Reproducibility

Run the headless empty simulation in two fresh processes.

Expected:

- Same final checksum.

### Command Ordering

Insert commands for the same tick in shuffled order.

Expected:

- Execution order is player index, sequence, command type.
- Final checksum matches canonical ordered insertion.

### Replay Determinism

Record a command stream and replay it.

Expected:

- Replay final checksum equals original final checksum.

### Lockstep Empty Stream

Run two peers with NoOp commands.

Expected:

- Checksums match at every interval.

### Lockstep Arrival Reordering

Deliver the same commands in different arrival order.

Expected:

- Commands execute in deterministic order.
- Checksums match.

### Missing Input Stall

Withhold one peer's command for a future tick.

Expected:

- Session does not advance past that tick.
- No guessed input is created.

### Canonical Serialization

Serialize the same state twice.

Expected:

- Byte output is identical.
- Checksum is identical.

## Phase 0 Done Definition

Phase 0 is complete when:

- Simulation can run headlessly for 10,000 ticks.
- Empty replay reproduces final checksum.
- Debug command replay reproduces final checksum.
- Two-peer lockstep harness matches checksums.
- Missing input stalls lockstep.
- Command arrival order does not affect result.
- Simulation module has no engine, presentation, or networking imports.
- All simulation math uses explicit integer widths or project fixed-point.

Do not proceed to Capital placement until this definition is met.
