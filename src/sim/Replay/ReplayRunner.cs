using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Replay
{
    public sealed class ReplayRunner
    {
        public ReplayResult Run(ReplayFile replay, int ticksToRun)
        {
            GameState state = replay.InitialState == ReplayInitialState.Nomad
                ? GameInitializer.CreateNomadStart(replay.MatchSeed, replay.PlayerCount)
                : new GameState(replay.MatchSeed, replay.PlayerCount);
            var buffer = new CommandBuffer();
            foreach (CommandEnvelope command in replay.Commands)
            {
                buffer.Add(command);
            }

            var runner = new TickRunner();
            var mismatches = new List<ChecksumMismatch>();

            for (int i = 0; i < ticksToRun; i++)
            {
                runner.AdvanceOneTick(state, replay.InitialRules, buffer);
                for (int c = 0; c < replay.OptionalChecksums.Count; c++)
                {
                    ReplayChecksum expected = replay.OptionalChecksums[c];
                    if (expected.Tick == state.Tick - 1 && expected.Checksum != state.LastChecksum)
                    {
                        mismatches.Add(new ChecksumMismatch(expected.Tick, expected.Checksum, state.LastChecksum));
                    }
                }
            }

            return new ReplayResult(state.Tick, state.LastChecksum, mismatches);
        }
    }
}
