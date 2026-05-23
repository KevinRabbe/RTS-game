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
        private static void ResignNeutralizesAssetsAndAssignsPlacement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(12, 2);
            int buildingId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(1, 0));
            state.EntityState.Buildings[state.EntityState.EntityLookup[buildingId].Index].IsUnderConstruction = false;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.PlayerStates.Players[0].IsResigned, "player should be resigned");
            AssertEqual(true, state.PlayerStates.Players[0].IsDefeated, "resigned player should be defeated for placement");
            AssertEqual(2, state.PlayerStates.Players[0].Placement, "first defeated player in 2-player match should get second place");
            AssertEqual(0, state.RankingState.NextPlacement, "next placement should be exhausted after winner assignment");
            AssertEqual(true, state.MatchResultState.IsFinished, "match should finish after one player resigns in a two-player match");
            AssertEqual(1, state.PlayerStates.Players[1].Placement, "remaining player should receive first place");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, state.EntityState.Units[0].OwnerPlayerIndex, "resigned unit should become neutral");
            AssertEqual(GameData.ResignedAssetDespawnTicks, state.EntityState.Units[0].DespawnTicksRemaining, "resigned unit should get despawn timer");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, state.EntityState.Buildings[0].OwnerPlayerIndex, "resigned building should become neutral");
            AssertEqual(GameData.ResignedAssetDespawnTicks, state.EntityState.Buildings[0].DespawnTicksRemaining, "resigned building should get despawn timer");
        }

        private static void ResignedAssetsDespawnAfterTimer()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(13, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int i = 0; i < GameData.ResignedAssetDespawnTicks; i++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(0, state.EntityState.Units.Count, "resigned units should despawn after timer");
        }

        private static void ResignedPlayerNonNoOpCommandsReject()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(14, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(10, 0))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "resigned non-noop command should be rejected");
            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "neutral resigned unit should not receive move target");
        }

        private static void ResignationReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var recorder = new ReplayRecorder(rules, 15, 2, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 10);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 10);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "resignation replay should be deterministic");
        }

        private static void ResignationLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 16, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "resignation tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "resignation lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "resignation peer checksums should match");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, session.Peers[0].LocalState.EntityState.Units[0].OwnerPlayerIndex, "resigned player unit should be neutral in lockstep");
        }

        private static void PlayerEliminatedWithNoTownCenterAndNoVillagers()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(21, 2);
            KillPlayerVillagers(state, 0);
            var buffer = new CommandBuffer();

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.PlayerStates.Players[0].IsDefeated, "player with no TC and no villagers should be defeated");
            AssertEqual(2, state.PlayerStates.Players[0].Placement, "first eliminated player should receive last place");
        }

        private static void PlayerSurvivesWithVillagerAfterTownCenterLoss()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(22, 2);
            EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(1, 0));
            state.EntityState.Buildings[0].IsUnderConstruction = false;
            state.EntityState.Buildings[0].IsDead = true;
            var buffer = new CommandBuffer();

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.PlayerStates.Players[0].IsDefeated, "player should survive TC loss if villagers remain");
        }

        private static void EliminationReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState first = GameInitializer.CreateNomadStart(23, 2);
            GameState second = GameInitializer.CreateNomadStart(23, 2);
            KillPlayerVillagers(first, 0);
            KillPlayerVillagers(second, 0);
            ulong firstChecksum = RunCommandsFromState(first, rules, new CommandEnvelope[0], 1);
            ulong secondChecksum = RunCommandsFromState(second, rules, new CommandEnvelope[0], 1);
            AssertEqual(firstChecksum, secondChecksum, "elimination should be deterministic");
        }

        private static void EliminationLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 24, true);
            KillPlayerVillagers(session.Peers[0].LocalState, 0);
            KillPlayerVillagers(session.Peers[1].LocalState, 0);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            AssertEqual(true, session.TryAdvanceOneTick(), "elimination tick should advance");
            AssertEqual(0, session.DesyncReports.Count, "elimination lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "elimination peer checksums should match");
            AssertEqual(true, session.Peers[0].LocalState.PlayerStates.Players[0].IsDefeated, "player should be defeated in lockstep");
        }

        private static void MatchEndsWhenOnePlayerRemains()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(25, 2);
            KillPlayerVillagers(state, 0);

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            AssertEqual(true, state.MatchResultState.IsFinished, "match should finish when one player remains");
            AssertEqual(1, state.MatchResultState.WinnerPlayerIndex, "remaining player should be winner");
            AssertEqual(1, state.PlayerStates.Players[1].Placement, "winner should receive first place");
            AssertEqual(2, state.PlayerStates.Players[0].Placement, "eliminated player should receive second place");
        }

        private static void MatchEndReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState first = GameInitializer.CreateNomadStart(26, 2);
            GameState second = GameInitializer.CreateNomadStart(26, 2);
            KillPlayerVillagers(first, 0);
            KillPlayerVillagers(second, 0);

            ulong firstChecksum = RunCommandsFromState(first, rules, new CommandEnvelope[0], 1);
            ulong secondChecksum = RunCommandsFromState(second, rules, new CommandEnvelope[0], 1);
            AssertEqual(firstChecksum, secondChecksum, "match end should be deterministic");
        }

        private static void MatchEndLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 27, true);
            KillPlayerVillagers(session.Peers[0].LocalState, 0);
            KillPlayerVillagers(session.Peers[1].LocalState, 0);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            AssertEqual(true, session.TryAdvanceOneTick(), "match end tick should advance");
            AssertEqual(0, session.DesyncReports.Count, "match end lockstep should not desync");
            AssertEqual(true, session.Peers[0].LocalState.MatchResultState.IsFinished, "match should finish in lockstep");
            AssertEqual(1, session.Peers[0].LocalState.PlayerStates.Players[1].Placement, "winner placement should be assigned in lockstep");
        }

    }
}
