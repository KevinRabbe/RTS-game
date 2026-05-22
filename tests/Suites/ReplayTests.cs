using RtsGame.Net.Lockstep;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void ReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var recorder = new ReplayRecorder(rules, 77, 2);
            for (int tick = 0; tick < 100; tick++)
            {
                recorder.RecordCommand(new CommandEnvelope(new CommandHeader(tick, 0, 0, CommandType.NoOp), new NoOpCommand()));
                recorder.RecordCommand(new CommandEnvelope(new CommandHeader(tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            }

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 100);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 100);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "replay checksum must be stable");
        }

    }
}
