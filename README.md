# RTS Game

Competitive 2D RTS focused on strategic superiority, decisive execution, and survival under pressure.

Primary target mode:

- 6-player free-for-all.
- Nomad start.
- First Town Center becomes the Capital.
- Deterministic lockstep multiplayer.
- Replay-friendly command-stream simulation.

## Architecture Law

Simulation must be deterministic, engine-agnostic, and command-driven.

The simulation core must not depend on rendering, UI, audio, engine physics, networking callbacks, wall-clock time, or unordered iteration.

## Project Docs

- [Current Vertical Slice](docs/current-vertical-slice.md)
- [Development Roadmap](docs/development-roadmap.md)
- [Prototype Architecture](docs/prototype-architecture.md)
- [Worker Resource Economy v1](docs/worker-resource-economy-v1.md)
- [High-Pop Simulation Architecture](docs/high-pop-simulation-architecture.md)
- [Playable Prototype Spec](docs/playable-prototype-spec.md)
- [Phase 0 Code Skeleton Blueprint](docs/phase-0-code-skeleton-blueprint.md)
- [Testing Workflow](docs/testing-workflow.md)

## Core Pillars

- Core gameplay loop.
- Capital system.
- Map control.
- Economy and trade.
- Combat and siege.
- Multiplayer lockstep architecture.

## Prototype Priority

First prove:

1. Deterministic simulation.
2. Replay from command stream.
3. Checksums and desync detection.
4. Nomad Capital placement.
5. Basic economy.
6. Basic combat.
7. Capital destruction as a turning point.

Only expand factions, trade depth, AI, spectator tools, and ranked infrastructure after the deterministic foundation is stable.
