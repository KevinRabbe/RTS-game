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
        private static void TrainInfantryCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = GameData.InfantryFoodCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Infantry)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.InfantryTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "infantry should complete training");
            AssertEqual(UnitTypeId.Infantry, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be infantry");
        }

        private static void TrainCavalryCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = GameData.CavalryFoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.CavalryGoldCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Cavalry)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.CavalryTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "cavalry should complete training");
            AssertEqual(UnitTypeId.Cavalry, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be cavalry");
            AssertEqual(7, state.PlayerStates.Players[0].PopulationUsed, "cavalry should reserve two population");
        }

        private static void CavalryMovesFasterThanInfantry()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(61);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Cavalry, FixedVector2.FromInts(0, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(5, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 2 }, FixedVector2.FromInts(5, 1))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromRatio(2, 5).Raw, state.EntityState.Units[0].Position.X.Raw, "infantry should move at infantry speed");
            AssertEqual(Fixed.FromRatio(4, 5).Raw, state.EntityState.Units[1].Position.X.Raw, "cavalry should move at cavalry speed");
        }

        private static void CavalryDamagesEnemyUnit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateCavalryCombatState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.InfantryHitPoints - GameData.CavalryAttackDamage, state.EntityState.Units[11].HitPoints, "cavalry should damage enemy infantry");
            AssertEqual(GameData.CavalryAttackCooldownTicks, state.EntityState.Units[10].AttackCooldownTicksRemaining, "cavalry cooldown should be set");
        }

    }
}
