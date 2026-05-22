using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private sealed class SimScenarioHarness
        {
            private readonly TickRunner runner = new TickRunner();
            private readonly CommandBuffer buffer = new CommandBuffer();
            private readonly Queue<string> traces = new Queue<string>();
            private readonly int players;
            private uint nextSequence;

            public SimScenarioHarness(GameState state, GameRules rules, int players, uint sequenceSeed)
            {
                State = state;
                Rules = rules;
                this.players = players;
                nextSequence = sequenceSeed;
            }

            public GameState State { get; }

            public GameRules Rules { get; }

            public void Step(params CommandEnvelope[] commands)
            {
                for (int i = 0; i < commands.Length; i++)
                {
                    buffer.Add(commands[i]);
                }

                runner.AdvanceOneTick(State, Rules, buffer);
            }

            public void StepNoOps()
            {
                for (int player = 0; player < players; player++)
                {
                    AddNoOp(buffer, State.Tick, player, nextSequence++);
                }

                runner.AdvanceOneTick(State, Rules, buffer);
            }

            public void RunTicks(int maxTicks, Func<bool> stopWhen)
            {
                for (int i = 0; i < maxTicks; i++)
                {
                    if (stopWhen())
                    {
                        return;
                    }

                    StepNoOps();
                }
            }

            public void CaptureWorkerTrace(int[] unitIds, int maxEntries)
            {
                CaptureWorkerTraceTick(State, unitIds, traces, maxEntries);
            }

            public void AssertCoreInvariants(string header)
            {
                AssertNoLiveUnitStacking(State, Fail(header + " stacking"));
                AssertNoDuplicateFinalPurposeReservations(State, Fail(header + " duplicate reservations"));
                AssertEqual(true, State.DebugCounters.RejectedCommandCount <= 64, Fail(header + " suspicious command reject volume"));
            }

            public string Fail(string header)
            {
                return BuildTraceFailureMessage(header, traces);
            }
        }
    }
}
