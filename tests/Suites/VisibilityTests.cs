using RtsGame.Net.Lockstep;
using RtsGame.Presentation.ClientInput;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Presentation.LocalPlay;
using RtsGame.Presentation.Snapshots;
using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Sim.Systems;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void VisibilityRevealsInitialScoutArea()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, IsVisible(state, 0, 0, 2), "scout tile should be visible");
            AssertEqual(true, IsVisible(state, 0, 8, 2), "scout radius should reveal eight tiles horizontally");
            AssertEqual(false, IsVisible(state, 0, 20, 20), "far tile should not be visible");
        }

        private static void VisibilityUpdatesAfterMovement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(20, 2))));

            for (int tick = 0; tick < 22; tick++)
            {
                if (tick > 0)
                {
                    AddNoOp(buffer, tick, 0, (uint)tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(true, IsVisible(state, 0, 28, 2), "moved scout should reveal around new location");
            AssertEqual(false, IsVisible(state, 0, 8, 2), "old scout-only tile should no longer be currently visible");
        }

        private static void ExploredVisibilityPersists()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            runner.AdvanceOneTick(state, rules, buffer);
            AssertEqual(true, IsExplored(state, 0, 8, 2), "initial scout-only tile should be explored");

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(20, 2))));
            for (int tick = 1; tick <= 24; tick++)
            {
                if (tick > 1)
                {
                    AddNoOp(buffer, tick, 0, (uint)tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(false, IsVisible(state, 0, 8, 2), "old scout-only tile should leave current visibility");
            AssertEqual(true, IsExplored(state, 0, 8, 2), "old scout-only tile should remain explored");
        }

        private static void VisibilityReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var recorder = new ReplayRecorder(rules, 101, 1, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(10, 2))));
            for (int tick = 1; tick < 8; tick++)
            {
                recorder.RecordCommand(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
            }

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 8);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 8);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "visibility replay should be deterministic");
        }

        private static void VisibilityLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 55, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(10, 2))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 10 }, FixedVector2.FromInts(50, 2))));
            AssertEqual(true, session.TryAdvanceOneTick(), "visibility movement tick should advance");

            for (int tick = 1; tick < 8; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "visibility continuation tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "visibility lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "visibility peer checksums should match");
        }

    }
}
