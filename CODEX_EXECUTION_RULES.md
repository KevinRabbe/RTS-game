# Codex Execution Rules

These rules govern all future Codex work on this RTS project.

## Always

- Work one phase at a time.
- Keep simulation engine-agnostic.
- Keep all player actions command-driven.
- Add focused tests for every new mechanic.
- Add replay and lockstep coverage for deterministic mechanics.
- Update checksum coverage for every new mutable simulation field.
- Run ChaosV1 after core lifecycle changes.
- Run ChaosV2 after spatial or movement changes.
- Run the latest frozen chaos scenario after adding a new stress domain.
- Prefer flat data plus systems over inheritance-heavy models.
- Keep systems stateless.
- Preserve fixed system order unless explicitly reviewed.
- Use deterministic tie-breakers for any conflict resolution.
- Document every frozen chaos scenario and version it.

## Stop Before

- Changing tick order.
- Changing cleanup rules.
- Changing command validation or execution semantics.
- Modifying ChaosV1 behavior.
- Modifying ChaosV2 behavior.
- Adding a new simulation state field without checksum coverage.
- Adding any movement priority, pushing, sliding, pathfinding, or alternate routing.
- Adding area damage or multi-target damage.
- Adding Godot or client references to simulation.

## Never

- Add Godot references to simulation.
- Add engine types to simulation.
- Add gameplay logic to presentation.
- Use physics engine behavior for simulation.
- Use delta time in simulation.
- Add casual float or double gameplay math.
- Iterate unordered collections for gameplay decisions.
- Add hidden mutable state inside systems.
- Add async simulation mutation.
- Add random behavior without seeded deterministic RNG and stable ordering.
- Add inheritance-heavy unit or building models.
- Modify a frozen chaos scenario to make a failing test pass.

## Required Gates

Before merging core simulation changes:

```powershell
dotnet run --project tests/RtsGame.Tests.csproj --no-build
dotnet run --project src/tools/Headless/RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v1 --ticks 5000 --seed 77
```

Before merging movement or spatial changes:

```powershell
dotnet run --project src/tools/Headless/RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v1 --ticks 5000 --seed 77
dotnet run --project src/tools/Headless/RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v2 --ticks 5000 --seed 78
```

Before adding area damage:

```powershell
dotnet run --project src/tools/Headless/RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v1 --ticks 10000 --seed 77
dotnet run --project src/tools/Headless/RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v2 --ticks 10000 --seed 78
```
