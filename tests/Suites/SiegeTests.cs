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
        private static void TrainSiegeCannonCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(31, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Wood = GameData.SiegeCannonWoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.SiegeCannonGoldCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.SiegeCannon)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.SiegeCannonTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "siege cannon should complete training");
            AssertEqual(UnitTypeId.SiegeCannon, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be siege cannon");
            AssertEqual(8, state.PlayerStates.Players[0].PopulationUsed, "siege cannon should reserve three population");
        }

        private static void SiegeSetupDelaysFirstShot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            runner.AdvanceOneTick(state, rules, buffer);
            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus, state.EntityState.Buildings[0].HitPoints, "siege should not fire on first setup tick");
            AssertEqual(false, state.EntityState.Units[10].IsSiegeDeployed, "siege should not deploy immediately");

            AddNoOp(buffer, 1, 0, 1);
            AddNoOp(buffer, 1, 1, 1);
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            AddNoOp(buffer, 2, 1, 2);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.EntityState.Units[10].IsSiegeDeployed, "siege should deploy after setup ticks");
            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus - GameData.SiegeCannonBuildingDamage, state.EntityState.Buildings[0].HitPoints, "siege should fire after setup completes");
        }

        private static void SiegeReloadDelaysSecondShot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            AdvanceSiegeUntilFirstShot(rules, state, buffer, runner);
            int hitPointsAfterFirstShot = state.EntityState.Buildings[0].HitPoints;

            AddNoOp(buffer, state.Tick, 0, 3);
            AddNoOp(buffer, state.Tick, 1, 3);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(hitPointsAfterFirstShot, state.EntityState.Buildings[0].HitPoints, "reload should prevent immediate second shot");
            AssertEqual(GameData.SiegeCannonReloadTicks - 1, state.EntityState.Units[10].SiegeReloadTicksRemaining, "reload should count down after first shot");
        }

        private static void MovingSiegeCancelsDeployment()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            AdvanceSiegeUntilFirstShot(rules, state, buffer, runner);
            AssertEqual(true, state.EntityState.Units[10].IsSiegeDeployed, "siege should be deployed before move");

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 11 }, FixedVector2.FromInts(1, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 3, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Units[10].IsSiegeDeployed, "moving siege should cancel deployment");
            AssertEqual(0, state.EntityState.Units[10].SiegeSetupTicksRemaining, "moving siege should clear setup");
            AssertEqual(0, state.EntityState.Units[10].SiegeReloadTicksRemaining, "moving siege should clear reload");
        }

        private static void SiegeRejectsUnitTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            EntityFactory.CreateUnit(state, 0, UnitTypeId.SiegeCannon, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 13 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "siege should reject unit target in first slice");
        }

        private static void SiegeDestroysCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            state.EntityState.Buildings[0].HitPoints = GameData.SiegeCannonBuildingDamage;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            AdvanceSiegeUntilFirstShot(rules, state, buffer, runner);

            AssertEqual(false, state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "siege-destroyed capital should remove bonus");
            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(12), "siege-destroyed capital should be cleaned up");
        }

        private static void SiegeReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(2, 1, 2, CommandType.NoOp), new NoOpCommand())
            };

            ulong first = RunCommandsFromState(CreateSiegeCapitalState(), rules, commands, 3);
            ulong second = RunCommandsFromState(CreateSiegeCapitalState(), rules, commands, 3);
            AssertEqual(first, second, "siege replay should be deterministic");
        }

        private static void SiegeLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 32, true);
            SetupSiegeCapitalState(session.Peers[0].LocalState);
            SetupSiegeCapitalState(session.Peers[1].LocalState);

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "siege attack command tick should advance");
            for (int tick = 1; tick <= 2; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "siege setup tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "siege lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "siege peer checksums should match");
            AssertEqual(true, session.Peers[0].LocalState.EntityState.Units[10].IsSiegeDeployed, "siege should deploy in lockstep");
        }

        private static void TrainMangonelCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(33, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Wood = GameData.MangonelWoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.MangonelGoldCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Mangonel)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.MangonelTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "mangonel should complete training");
            AssertEqual(UnitTypeId.Mangonel, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be mangonel");
            AssertEqual(8, state.PlayerStates.Players[0].PopulationUsed, "mangonel should reserve three population");
        }

        private static void MangonelAreaHitsMultipleEnemies()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.InfantryHitPoints - GameData.MangonelAreaDamage, state.EntityState.Units[11].HitPoints, "target enemy should take area damage");
            AssertEqual(GameData.InfantryHitPoints - GameData.MangonelAreaDamage, state.EntityState.Units[12].HitPoints, "nearby enemy should take area damage");
            AssertEqual(GameData.InfantryHitPoints, state.EntityState.Units[13].HitPoints, "enemy outside radius should not take area damage");
            AssertEqual(GameData.MangonelAreaCooldownTicks, state.EntityState.Units[10].AttackCooldownTicksRemaining, "mangonel cooldown should be set after firing");
        }

        private static void MangonelAreaIgnoresFriendlyUnits()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.InfantryHitPoints, state.EntityState.Units[14].HitPoints, "friendly unit inside radius should not take area damage");
        }

        private static void MangonelSimultaneousDeathsCleanup()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            state.EntityState.Units[11].HitPoints = GameData.MangonelAreaDamage;
            state.EntityState.Units[12].HitPoints = GameData.MangonelAreaDamage;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(12), "area-killed target should be cleaned up");
            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(13), "area-killed nearby unit should be cleaned up");
            AssertEqual(6, state.PlayerStates.Players[1].PopulationUsed, "population should remove both area-killed infantry");
        }

        private static void MangonelAreaDoesNotDamageCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            int capitalId = EntityFactory.CreateTownCenter(state, 1, FixedVector2.FromInts(5, 0));
            Building capital = state.EntityState.Buildings[state.EntityState.EntityLookup[capitalId].Index];
            capital.IsUnderConstruction = false;
            capital.HitPoints = GameData.GetBuildingCompletedHitPoints(BuildingTypeId.TownCenter, true);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus, capital.HitPoints, "mangonel area should not damage buildings in first slice");
        }

        private static void MangonelReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12))
            };

            ulong first = RunCommandsFromState(CreateMangonelAreaState(), rules, commands, 1);
            ulong second = RunCommandsFromState(CreateMangonelAreaState(), rules, commands, 1);
            AssertEqual(first, second, "mangonel area damage should replay deterministically");
        }

        private static void MangonelLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 34, true);
            SetupMangonelAreaState(session.Peers[0].LocalState);
            SetupMangonelAreaState(session.Peers[1].LocalState);

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "mangonel attack tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "mangonel lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "mangonel peer checksums should match");
            AssertEqual(GameData.InfantryHitPoints - GameData.MangonelAreaDamage, session.Peers[0].LocalState.EntityState.Units[12].HitPoints, "nearby enemy should take area damage in lockstep");
        }

    }
}
