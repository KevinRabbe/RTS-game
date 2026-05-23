using RtsGame.Net.Lockstep;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Sim.Systems;
using RtsGame.Stress;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void GatherEngineV2FlagDefaultsOn()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            AssertEqual(true, rules.EnableGatherEngineV2, "gather engine v2 should default on after staged rollout");
        }

        private static void GatherEngineV2ModeRemainsDeterministic()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1).WithGatherEngineV2(true);
            GameState first = CreateSingleNodeResourceAreaState(2013, GatherProfileId.BerryBush, out int nodeId, out _);
            GameState second = CreateSingleNodeResourceAreaState(2013, GatherProfileId.BerryBush, out _, out _);
            Unit firstWorker = first.EntityState.Units[0];
            Unit secondWorker = second.EntityState.Units[0];

            FixedVector2 tcPosition = FixedVector2.FromInts(8, 8);
            EntityFactory.CreateTownCenter(first, 0, tcPosition);
            EntityFactory.CreateTownCenter(second, 0, tcPosition);
            first.EntityState.Buildings[0].IsUnderConstruction = false;
            second.EntityState.Buildings[0].IsUnderConstruction = false;

            var command = new GatherResourceCommand(nodeId, new[] { firstWorker.Id });
            var header = new CommandHeader(first.Tick, 0, 0, CommandType.GatherResource);
            command.Execute(first, rules, header);
            var command2 = new GatherResourceCommand(nodeId, new[] { secondWorker.Id });
            command2.Execute(second, rules, header);

            var runner = new TickRunner();
            for (int i = 0; i < 40; i++)
            {
                runner.AdvanceOneTick(first, rules, new CommandBuffer());
                runner.AdvanceOneTick(second, rules, new CommandBuffer());
            }

            ulong firstChecksum = StateChecksum.Compute(first, rules);
            ulong secondChecksum = StateChecksum.Compute(second, rules);
            AssertEqual(firstChecksum, secondChecksum, "gather engine v2 mode must remain deterministic");
        }

    }
}
