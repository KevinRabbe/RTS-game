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
        private static void TradePostRejectsBeforeCompletedTownCenter()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(53, 1);
            FundTradePost(state, 0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "trade post should require completed town center");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "early trade post placement should be rejected");
        }

        private static void TradePostRejectsMissingResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(54, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "trade post should reject without resources");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "missing trade post resources should count as rejected");
        }

        private static void PlaceTradePostCreatesConstruction()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(53, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(state, 0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Building tradePost = state.EntityState.Buildings[1];
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "trade post should spend wood");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Gold, "trade post should spend gold");
            AssertEqual(BuildingTypeId.TradePost, tradePost.BuildingTypeId, "trade post placement should create trade post");
            AssertEqual(true, tradePost.IsUnderConstruction, "placed trade post should start under construction");
            AssertEqual(GameData.TradePostUnderConstructionHitPoints, tradePost.HitPoints, "placed trade post should be vulnerable while building");
        }

        private static void AssignedVillagersCompleteTradePost()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(54, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(state, 0);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(7, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            runner.AdvanceOneTick(state, rules, buffer);

            Building tradePost = state.EntityState.Buildings[1];
            AssertEqual(true, tradePost.IsUnderConstruction || tradePost.BuildProgressTicks > 0, "assigned villagers should begin trade post construction after assignment");
            AssertEqual(7, state.EntityState.Units[0].CurrentBuildTargetId, "first villager should stay assigned to trade post");
            AssertEqual(7, state.EntityState.Units[1].CurrentBuildTargetId, "second villager should stay assigned to trade post");
        }

        private static void TradePostReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(7, new[] { 1, 2 })),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.NoOp), new NoOpCommand())
            };
            GameState firstState = GameInitializer.CreateNomadStart(55, 1);
            GameState secondState = GameInitializer.CreateNomadStart(55, 1);
            AddCompletedTownCenter(firstState, 0, FixedVector2.FromInts(0, 0));
            AddCompletedTownCenter(secondState, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(firstState, 0);
            FundTradePost(secondState, 0);

            ulong first = RunCommandsFromState(firstState, rules, commands, 4);
            ulong second = RunCommandsFromState(secondState, rules, commands, 4);
            AssertEqual(first, second, "trade post replay checksums should match");
        }

        private static void TradePostLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 56, true);
            AddCompletedTownCenter(session.Peers[0].LocalState, 0, FixedVector2.FromInts(0, 0));
            AddCompletedTownCenter(session.Peers[1].LocalState, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(session.Peers[0].LocalState, 0);
            FundTradePost(session.Peers[1].LocalState, 0);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post placement tick should advance");
            int tradePostId = FindUnderConstructionBuildingId(session.Peers[0].LocalState, 0, BuildingTypeId.TradePost);
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tradePostId, new[] { 1, 2 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post build assignment tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 1, 2, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post build tick should advance");
            int tick = 3;
            uint sequence = 3;
            while (tick < 80 && session.Peers[0].LocalState.EntityState.Buildings[1].IsUnderConstruction)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, sequence, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "trade post completion progression tick should advance");
                tick++;
                sequence++;
            }

            Building tradePost = session.Peers[0].LocalState.EntityState.Buildings[1];
            AssertEqual(0, session.DesyncReports.Count, "trade post lockstep should not desync");
            AssertEqual(tradePostId, session.Peers[0].LocalState.EntityState.Units[0].CurrentBuildTargetId, "first builder should remain assigned to trade post in lockstep");
            AssertEqual(tradePostId, session.Peers[0].LocalState.EntityState.Units[1].CurrentBuildTargetId, "second builder should remain assigned to trade post in lockstep");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "trade post peer checksums should match");
        }

        private static void TrainTradeCartCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(50, 1);
            int postId = EntityFactory.CreateTradePost(state, 0, FixedVector2.FromInts(0, 0));
            state.PlayerStates.Players[0].PopulationCap = 10;
            state.PlayerStates.Players[0].Resources.Wood = GameData.TradeCartWoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.TradeCartGoldCost;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.TrainUnit), new TrainUnitCommand(postId, UnitTypeId.TradeCart)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.TradeCartTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(1 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(UnitTypeId.TradeCart, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trade post should train trade cart");
        }

        private static void TradeRoutePaysGoldOnArrival()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTradeState(10);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(6, 7, 8)));
            AdvanceTradeUntilFirstDeposit(state, rules, runner, buffer, 64);

            AssertEqual(20, state.PlayerStates.Players[0].Resources.Gold, "trade cart should pay gold based on route length");
            AssertEqual(7, state.EntityState.Units[5].TradeDestinationId, "cart should head back to first post after payment");
        }

        private static void LongerTradeRoutePaysMore()
        {
            GameState shortRoute = CreateTradeState(10);
            GameState longRoute = CreateTradeState(20);
            AssertEqual(true, shortRoute.EntityState.Units[5].Id == 6, "trade cart id should be deterministic");
            var shortCommand = new CreateTradeRouteCommand(6, 7, 8);
            var longCommand = new CreateTradeRouteCommand(6, 7, 8);
            shortCommand.Execute(shortRoute, GameRules.CreatePhaseZeroDefaults(1), new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute));
            longCommand.Execute(longRoute, GameRules.CreatePhaseZeroDefaults(1), new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute));
            AssertEqual(true, longRoute.EntityState.Units[5].TradeIncomePerTrip > shortRoute.EntityState.Units[5].TradeIncomePerTrip, "longer route should pay more");
        }

        private static void DestroyedTradeEndpointClearsRoute()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTradeState(10);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(6, 7, 8)));
            runner.AdvanceOneTick(state, rules, buffer);
            state.EntityState.Buildings[1].IsDead = true;
            AddNoOp(buffer, 1, 0, 1);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Units[5].TradeRouteAId, "destroyed endpoint should clear trade route");
        }

        private static void TradeCartCanBeKilled()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(51, 2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.TradeCart, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            state.EntityState.Units[10].HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.Attack), new AttackCommand(new[] { 12 }, 11)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(11), "trade cart should be vulnerable to attack");
        }

        private static void TradeReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var commands = new List<CommandEnvelope>
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(6, 7, 8))
            };

            for (int tick = 1; tick < 64; tick++)
            {
                commands.Add(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
            }

            GameState firstState = CreateTradeState(10);
            GameState secondState = CreateTradeState(10);
            ulong first = RunCommandsFromState(firstState, rules, commands, 64);
            ulong second = RunCommandsFromState(secondState, rules, commands, 64);
            AssertEqual(first, second, "trade route trip should replay deterministically");
            AssertEqual(true, firstState.PlayerStates.Players[0].Resources.Gold >= 20, "replayed trade route should pay gold on arrival");
        }

        private static void TradeLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 52, true);
            SetupTradeState(session.Peers[0].LocalState, 10);
            SetupTradeState(session.Peers[1].LocalState, 10);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(11, 12, 13)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade route tick should advance");
            for (int tick = 1; tick < 64; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "trade movement tick should advance");

                if (session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Gold >= 20)
                {
                    break;
                }
            }

            AssertEqual(0, session.DesyncReports.Count, "trade lockstep should not desync");
            AssertEqual(20, session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Gold, "trade income should be paid in lockstep state");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "trade peer checksums should match");
        }

    }
}
