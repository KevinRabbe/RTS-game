# Phase 1 Long-Run Drift Audit

Date: 2026-05-09

This audit records the Stage 1.3 long-run deterministic drift gate after Phase 1.1 tile occupancy resolution and ChaosV2 spatial stress were added.

## Commands

```powershell
dotnet build src/tools/Headless/RtsGame.Headless.csproj --no-restore
dotnet run --project src/tools/Headless/RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v1 --ticks 10000 --seed 77
dotnet run --project src/tools/Headless/RtsGame.Headless.csproj --no-build -- run-stress --scenario chaos-v2 --ticks 10000 --seed 78
```

## ChaosV1 Result

- Scenario: `chaos-v1`
- Scenario version: `1`
- Final tick: `10000`
- Final checksum: `12757226426589041677`
- Command count: `60060`
- Desync: `False`
- Desync count: `0`
- Invariant failures: `0`

## ChaosV2 Result

- Scenario: `chaos-v2`
- Scenario version: `1`
- Final tick: `10000`
- Final checksum: `2125748326682007096`
- Command count: `60018`
- Desync: `False`
- Desync count: `0`
- Invariant failures: `0`

## Audit Conclusion

The deterministic core passes long-run drift checks for both macro lifecycle stress and spatial congestion stress.

This confirms:

- ChaosV1 remains stable after tile occupancy changes.
- ChaosV2 remains stable under deterministic congestion and blocking.
- Long-run replay agreement matches lockstep agreement.
- No invariant drift was detected over 10,000 ticks.

Before adding area damage, pathfinding, pushing, sliding, or any other nonlinear spatial/combat mechanic, rerun both 10k gates.
