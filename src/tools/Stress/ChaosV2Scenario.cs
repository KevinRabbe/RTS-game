using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Stress
{
    public sealed class ChaosV2Scenario : IStressScenario
    {
        public const string Name = "chaos-v2";
        public const int Version = 1;
        public const int ScenarioPlayerCount = 6;
        private const int EntitiesPerPlayer = 10;
        private const int FirstPreparedEntityId = 31;
        private const int FirstWallId = FirstPreparedEntityId + ScenarioPlayerCount * EntitiesPerPlayer;

        private readonly List<CommandEnvelope>[] _commandsByTick;
        private readonly uint[] _nextSequence;

        public int Ticks { get; }

        public string ScenarioName
        {
            get { return Name; }
        }

        public int ScenarioVersion
        {
            get { return Version; }
        }

        public int PlayerCount
        {
            get { return ScenarioPlayerCount; }
        }

        public ChaosV2Scenario(int ticks)
        {
            Ticks = ticks;
            _commandsByTick = new List<CommandEnvelope>[ticks];
            _nextSequence = new uint[ScenarioPlayerCount];
            BuildSchedule();
        }

        public IReadOnlyList<CommandEnvelope> GetCommandsForTick(int tick)
        {
            if (tick < 0 || tick >= _commandsByTick.Length || _commandsByTick[tick] == null)
            {
                return new CommandEnvelope[0];
            }

            return _commandsByTick[tick];
        }

        public void PrepareInitialState(GameState state)
        {
            for (int player = 0; player < ScenarioPlayerCount; player++)
            {
                PlayerState playerState = state.PlayerStates.Players[player];
                playerState.Resources.Food = 30000;
                playerState.Resources.Wood = 30000;
                playerState.Resources.Gold = 30000;
                playerState.PopulationCap = 200;

                AddCompletedCapital(state, player, CapitalPosition(player));
                EntityFactory.CreateTradePost(state, player, TradePostAPosition(player));
                EntityFactory.CreateTradePost(state, player, TradePostBPosition(player));
                EntityFactory.CreateUnit(state, player, UnitTypeId.TradeCart, TradePostAPosition(player));

                for (int unit = 0; unit < 4; unit++)
                {
                    EntityFactory.CreateUnit(state, player, UnitTypeId.Scout, MovementUnitPosition(player, unit));
                }

                int infantryId = EntityFactory.CreateUnit(state, player, UnitTypeId.Infantry, InfantryPosition(player));
                Unit infantry = state.EntityState.Units[state.EntityState.EntityLookup[infantryId].Index];
                infantry.HitPoints = GameData.InfantryAttackDamage;

                int siegeId = EntityFactory.CreateUnit(state, player, UnitTypeId.SiegeCannon, SiegePosition(player));
                Unit siege = state.EntityState.Units[state.EntityState.EntityLookup[siegeId].Index];
                siege.IsSiegeDeployed = true;
            }

            AddCompletedWall(state, WallId(0), FixedVector2.FromInts(64, 45));
            AddCompletedWall(state, WallId(1), FixedVector2.FromInts(64, 46));
            AddCompletedWall(state, WallId(2), FixedVector2.FromInts(64, 47));
        }

        private void BuildSchedule()
        {
            for (int player = 0; player < ScenarioPlayerCount; player++)
            {
                Add(0, player, CommandType.CreateTradeRoute, new CreateTradeRouteCommand(TradeCartId(player), TradePostAId(player), TradePostBId(player)));
                Add(1, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 0) }, FixedVector2.FromInts(64, 46)));
                Add(2, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 1), MovementUnitId(player, 2) }, SharedTarget(player)));
                Add(3, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 0) }, OppositeSideTarget(player)));
                Add(4, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 1) }, MovementUnitPosition(player, 2)));
                Add(4, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 2) }, MovementUnitPosition(player, 1)));
                Add(5, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 3) }, InfantryPosition((player + 1) % ScenarioPlayerCount)));
                Add(5, player, CommandType.Attack, new AttackCommand(new[] { InfantryId(player) }, InfantryId((player + 1) % ScenarioPlayerCount)));
                Add(6, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 3) }, FixedVector2.FromInts(66, 46)));
                Add(6, player, CommandType.Attack, new AttackCommand(new[] { SiegeId(player) }, WallId(1)));
                Add(40, player, CommandType.PlaceWall, new PlaceWallCommand(FixedVector2.FromInts(64, 46)));
                Add(100, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MovementUnitId(player, 0), MovementUnitId(player, 1), MovementUnitId(player, 2), MovementUnitId(player, 3) }, FixedVector2.FromInts(66, 46)));
            }

            Add(900, 0, CommandType.Resign, new ResignCommand());
            Add(950, 1, CommandType.Resign, new ResignCommand());
            Add(1000, 2, CommandType.Resign, new ResignCommand());
            Add(1050, 3, CommandType.Resign, new ResignCommand());
            Add(1100, 4, CommandType.Resign, new ResignCommand());
        }

        private void Add(int tick, int player, CommandType type, ICommand payload)
        {
            if (tick < 0 || tick >= _commandsByTick.Length)
            {
                return;
            }

            if (_commandsByTick[tick] == null)
            {
                _commandsByTick[tick] = new List<CommandEnvelope>();
            }

            _commandsByTick[tick].Add(new CommandEnvelope(new CommandHeader(tick, player, _nextSequence[player]++, type), payload));
        }

        private static void AddCompletedCapital(GameState state, int player, FixedVector2 position)
        {
            int id = EntityFactory.CreateTownCenter(state, player, position);
            Building capital = state.EntityState.Buildings[state.EntityState.EntityLookup[id].Index];
            capital.IsUnderConstruction = false;
            capital.BuildProgressTicks = GameData.TownCenterBuildTicks;
            capital.HitPoints = GameData.GetBuildingCompletedHitPoints(BuildingTypeId.TownCenter, capital.IsCapital);
            PlayerState playerState = state.PlayerStates.Players[player];
            playerState.CapitalStatus.IsCapitalAlive = true;
            playerState.CapitalStatus.CapitalBonusActive = true;
            playerState.PopulationCap += GameData.CapitalPopulationBonus;
        }

        private static void AddCompletedWall(GameState state, int expectedId, FixedVector2 position)
        {
            int id = EntityFactory.CreateWall(state, GameData.NeutralOwnerPlayerIndex, position);
            if (id != expectedId)
            {
                return;
            }

            Building wall = state.EntityState.Buildings[state.EntityState.EntityLookup[id].Index];
            wall.IsUnderConstruction = false;
            wall.BuildProgressTicks = GameData.WallBuildTicks;
            wall.HitPoints = GameData.SiegeCannonBuildingDamage;
        }

        private static int BaseId(int player)
        {
            return FirstPreparedEntityId + player * EntitiesPerPlayer;
        }

        public static int CapitalId(int player)
        {
            return BaseId(player);
        }

        private static int TradePostAId(int player)
        {
            return BaseId(player) + 1;
        }

        private static int TradePostBId(int player)
        {
            return BaseId(player) + 2;
        }

        private static int TradeCartId(int player)
        {
            return BaseId(player) + 3;
        }

        private static int MovementUnitId(int player, int offset)
        {
            return BaseId(player) + 4 + offset;
        }

        private static int InfantryId(int player)
        {
            return BaseId(player) + 8;
        }

        private static int SiegeId(int player)
        {
            return BaseId(player) + 9;
        }

        private static int WallId(int offset)
        {
            return FirstWallId + offset;
        }

        private static FixedVector2 CapitalPosition(int player)
        {
            return FixedVector2.FromInts(12 + player * 16, 8 + (player % 2) * 70);
        }

        private static FixedVector2 TradePostAPosition(int player)
        {
            return FixedVector2.FromInts(8 + player * 18, 20 + (player % 2) * 48);
        }

        private static FixedVector2 TradePostBPosition(int player)
        {
            return FixedVector2.FromInts(18 + player * 18, 20 + (player % 2) * 48);
        }

        private static FixedVector2 MovementUnitPosition(int player, int unit)
        {
            int side = player % 2 == 0 ? -1 : 1;
            return FixedVector2.FromInts(64 + side * (5 + unit), 42 + player + unit);
        }

        private static FixedVector2 SharedTarget(int player)
        {
            return FixedVector2.FromInts(58 + player, 55);
        }

        private static FixedVector2 OppositeSideTarget(int player)
        {
            return player % 2 == 0 ? FixedVector2.FromInts(68, 46) : FixedVector2.FromInts(60, 46);
        }

        private static FixedVector2 InfantryPosition(int player)
        {
            return FixedVector2.FromInts(30 + player * 3, 35);
        }

        private static FixedVector2 SiegePosition(int player)
        {
            return FixedVector2.FromInts(58 + player, 40);
        }
    }
}
