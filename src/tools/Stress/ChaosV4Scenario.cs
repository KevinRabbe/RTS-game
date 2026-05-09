using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Stress
{
    public sealed class ChaosV4Scenario : IStressScenario
    {
        public const string Name = "chaos-v4";
        public const int Version = 1;
        public const int ScenarioPlayerCount = 6;
        private const int EntitiesPerPlayer = 6;
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

        public ChaosV4Scenario(int ticks)
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
                EntityFactory.CreateUnit(state, player, UnitTypeId.Scout, MoverPosition(player));
                int siegeId = EntityFactory.CreateUnit(state, player, UnitTypeId.SiegeCannon, SiegePosition(player));
                Unit siege = state.EntityState.Units[state.EntityState.EntityLookup[siegeId].Index];
                siege.IsSiegeDeployed = true;
            }

            AddCompletedWall(state, FixedVector2.FromInts(64, 42), GameData.WallHitPoints);
            AddCompletedWall(state, FixedVector2.FromInts(64, 43), GameData.WallHitPoints);
            AddCompletedWall(state, FixedVector2.FromInts(64, 44), GameData.SiegeCannonBuildingDamage);
            AddCompletedWall(state, FixedVector2.FromInts(64, 45), GameData.WallHitPoints);
            AddCompletedWall(state, FixedVector2.FromInts(64, 46), GameData.WallHitPoints);
        }

        private void BuildSchedule()
        {
            for (int player = 0; player < ScenarioPlayerCount; player++)
            {
                Add(0, player, CommandType.CreateTradeRoute, new CreateTradeRouteCommand(TradeCartId(player), TradePostAId(player), TradePostBId(player)));
                Add(1, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MoverId(player) }, AcrossBarrierTarget(player)));
                Add(15, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { TradeCartId(player) }, TradePostBPosition(player)));
                Add(30, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MoverId(player) }, MoverPosition(player)));
                Add(45, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MoverId(player) }, AcrossBarrierTarget(player)));
            }

            Add(20, 0, CommandType.Attack, new AttackCommand(new[] { SiegeId(0) }, WallId(2)));
            Add(55, 1, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MoverId(1), MoverId(3) }, FixedVector2.FromInts(64, 44)));
            Add(56, 2, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MoverId(2), MoverId(4) }, FixedVector2.FromInts(64, 44)));

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

        private static void AddCompletedWall(GameState state, FixedVector2 position, int hitPoints)
        {
            int id = EntityFactory.CreateWall(state, GameData.NeutralOwnerPlayerIndex, position);
            Building wall = state.EntityState.Buildings[state.EntityState.EntityLookup[id].Index];
            wall.IsUnderConstruction = false;
            wall.BuildProgressTicks = GameData.WallBuildTicks;
            wall.HitPoints = hitPoints;
        }

        private static int BaseId(int player)
        {
            return FirstPreparedEntityId + player * EntitiesPerPlayer;
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

        private static int MoverId(int player)
        {
            return BaseId(player) + 4;
        }

        private static int SiegeId(int player)
        {
            return BaseId(player) + 5;
        }

        private static int WallId(int offset)
        {
            return FirstWallId + offset;
        }

        private static FixedVector2 CapitalPosition(int player)
        {
            return FixedVector2.FromInts(8 + player * 18, 8 + (player % 2) * 76);
        }

        private static FixedVector2 TradePostAPosition(int player)
        {
            return FixedVector2.FromInts(20 + player * 12, 24 + (player % 2) * 42);
        }

        private static FixedVector2 TradePostBPosition(int player)
        {
            return FixedVector2.FromInts(28 + player * 12, 24 + (player % 2) * 42);
        }

        private static FixedVector2 MoverPosition(int player)
        {
            return player % 2 == 0 ? FixedVector2.FromInts(58, 40 + player) : FixedVector2.FromInts(70, 40 + player);
        }

        private static FixedVector2 AcrossBarrierTarget(int player)
        {
            return player % 2 == 0 ? FixedVector2.FromInts(70, 40 + player) : FixedVector2.FromInts(58, 40 + player);
        }

        private static FixedVector2 SiegePosition(int player)
        {
            return FixedVector2.FromInts(60 + player, 38);
        }
    }
}
