using RtsGame.Net.Lockstep;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Replay;
using RtsGame.Stress;

namespace RtsGame.Headless
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string mode = args.Length > 0 ? args[0] : "run-empty";
            int ticks = ReadInt(args, "--ticks", 1000);
            int players = ReadInt(args, "--players", 2);
            ulong seed = (ulong)ReadInt(args, "--seed", 1);

            if (mode == "run-lockstep")
            {
                return RunLockstep(ticks, players, seed);
            }

            if (mode == "run-replay")
            {
                return RunReplay(ticks, players, seed);
            }

            if (mode == "run-stress")
            {
                return RunStress(args, ticks, seed);
            }

            return RunEmpty(ticks, players, seed);
        }

        private static int RunEmpty(int ticks, int players, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(players);
            var state = new GameState(seed, players);
            var commands = new CommandBuffer();
            var runner = new TickRunner();

            for (int tick = 0; tick < ticks; tick++)
            {
                for (int player = 0; player < players; player++)
                {
                    commands.Add(new CommandEnvelope(new CommandHeader(tick, player, 0, CommandType.NoOp), new NoOpCommand()));
                }

                runner.AdvanceOneTick(state, rules, commands);
            }

            PrintResult(state.Tick, state.LastChecksum, ticks * players, false);
            return 0;
        }

        private static int RunReplay(int ticks, int players, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(players);
            var recorder = new ReplayRecorder(rules, seed, players);

            for (int tick = 0; tick < ticks; tick++)
            {
                for (int player = 0; player < players; player++)
                {
                    recorder.RecordCommand(new CommandEnvelope(new CommandHeader(tick, player, 0, CommandType.NoOp), new NoOpCommand()));
                }
            }

            ReplayResult result = new ReplayRunner().Run(recorder.Replay, ticks);
            PrintResult(result.FinalTick, result.FinalChecksum, ticks * players, result.ChecksumMismatches.Count > 0);
            return result.ChecksumMismatches.Count == 0 ? 0 : 1;
        }

        private static int RunLockstep(int ticks, int players, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(players);
            var session = new LockstepSession(rules, seed);
            uint sequence = 0;

            for (int tick = 0; tick < ticks; tick++)
            {
                for (int player = 0; player < players; player++)
                {
                    var header = new CommandHeader(tick, player, sequence++, CommandType.NoOp);
                    CommandEnvelope command = new CommandEnvelope(header, new NoOpCommand());
                    session.Broadcast(command);
                }

                session.TryAdvanceOneTick();
            }

            ulong checksum = session.Peers[0].LocalState.LastChecksum;
            PrintResult(session.CurrentTick, checksum, (int)sequence, session.DesyncReports.Count > 0);
            return session.DesyncReports.Count == 0 && session.CurrentTick >= ticks ? 0 : 1;
        }

        private static int RunStress(string[] args, int ticks, ulong seed)
        {
            string scenario = ReadString(args, "--scenario", ChaosV1Scenario.Name);
            StressScenarioRunner runner = new StressScenarioRunner();
            StressScenarioResult result;
            if (scenario == ChaosV1Scenario.Name)
            {
                result = runner.RunChaosV1(ticks, seed);
            }
            else if (scenario == ChaosV2Scenario.Name)
            {
                result = runner.RunChaosV2(ticks, seed);
            }
            else
            {
                Console.WriteLine("unknown_scenario=" + scenario);
                return 1;
            }

            Console.WriteLine("scenario=" + result.ScenarioName);
            Console.WriteLine("scenario_version=" + result.ScenarioVersion);
            PrintResult(result.FinalTick, result.FinalChecksum, result.CommandCount, result.DesyncCount > 0);
            Console.WriteLine("desync_count=" + result.DesyncCount);
            Console.WriteLine("invariant_failures=" + result.InvariantFailureCount);
            for (int i = 0; i < result.InvariantFailures.Count; i++)
            {
                Console.WriteLine("invariant_failure=" + result.InvariantFailures[i]);
            }

            return result.Passed ? 0 : 1;
        }

        private static int ReadInt(string[] args, string name, int fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name && int.TryParse(args[i + 1], out int value))
                {
                    return value;
                }
            }

            return fallback;
        }

        private static string ReadString(string[] args, string name, string fallback)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }

        private static void PrintResult(int finalTick, ulong checksum, int commandCount, bool desync)
        {
            Console.WriteLine("final_tick=" + finalTick);
            Console.WriteLine("final_checksum=" + checksum);
            Console.WriteLine("command_count=" + commandCount);
            Console.WriteLine("desync=" + desync);
        }
    }
}
