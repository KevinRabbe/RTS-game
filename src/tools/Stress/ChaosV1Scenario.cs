using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Stress
{
    public sealed class ChaosV1Scenario : IStressScenario
    {
        public const string Name = "chaos-v1";
        public const int Version = 1;
        public const int PlayerCount = 6;
        public const int ExtraUnitsPerPlayer = 14;
        public const int ExtraInfantryPerPlayer = 10;
        public const int ExtraSiegePerPlayer = 4;

        private readonly List<CommandEnvelope>[] _commandsByTick;
        private readonly uint[] _nextSequence;

        public int Ticks { get; }
        public int CommandCount { get; private set; }
        public string ScenarioName
        {
            get { return Name; }
        }

        public int ScenarioVersion
        {
            get { return Version; }
        }

        int IStressScenario.PlayerCount
        {
            get { return PlayerCount; }
        }

        public ChaosV1Scenario(int ticks)
        {
            Ticks = ticks;
            _commandsByTick = new List<CommandEnvelope>[ticks];
            _nextSequence = new uint[PlayerCount];
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
            for (int player = 0; player < PlayerCount; player++)
            {
                PlayerState playerState = state.PlayerStates.Players[player];
                playerState.Resources.Food = 50000;
                playerState.Resources.Wood = 50000;
                playerState.Resources.Gold = 50000;
                playerState.PopulationCap = 200;

                FixedVector2 basePosition = CapitalPosition(player);
                for (int i = 0; i < ExtraInfantryPerPlayer; i++)
                {
                    int id = EntityFactory.CreateUnit(state, player, UnitTypeId.Infantry, new FixedVector2(basePosition.X + Fixed.FromInt(i), basePosition.Y + Fixed.FromInt(18)));
                    Unit unit = state.EntityState.Units[state.EntityState.EntityLookup[id].Index];
                    unit.HitPoints = GameData.InfantryAttackDamage;
                }

                for (int i = 0; i < ExtraSiegePerPlayer; i++)
                {
                    EntityFactory.CreateUnit(state, player, UnitTypeId.SiegeCannon, new FixedVector2(basePosition.X + Fixed.FromInt(i), basePosition.Y + Fixed.FromInt(22)));
                }
            }
        }

        private void BuildSchedule()
        {
            for (int player = 0; player < PlayerCount; player++)
            {
                Add(0, player, CommandType.PlaceTownCenter, new PlaceTownCenterCommand(CapitalPosition(player)));
                Add(1, player, CommandType.AssignBuild, new AssignBuildCommand(CapitalId(player), StartingVillagers(player)));
                Add(5, player, CommandType.PlaceTownCenter, new PlaceTownCenterCommand(SecondTownCenterPosition(player)));
                Add(6, player, CommandType.AssignBuild, new AssignBuildCommand(SecondTownCenterId(player), StartingVillagers(player)));
                Add(8, player, CommandType.TrainUnit, new TrainUnitCommand(CapitalId(player), UnitTypeId.Infantry));
                Add(8, player, CommandType.TrainUnit, new TrainUnitCommand(SecondTownCenterId(player), UnitTypeId.SiegeCannon));
                Add(20, player, CommandType.PlaceTradePost, new PlaceTradePostCommand(TradePostAPosition(player)));
                Add(21, player, CommandType.PlaceTradePost, new PlaceTradePostCommand(TradePostBPosition(player)));
                Add(22, player, CommandType.AssignBuild, new AssignBuildCommand(TradePostAId(player), new[] { StartingVillagerId(player, 0), StartingVillagerId(player, 1) }));
                Add(23, player, CommandType.AssignBuild, new AssignBuildCommand(TradePostBId(player), new[] { StartingVillagerId(player, 2), StartingVillagerId(player, 3) }));
                Add(26, player, CommandType.TrainUnit, new TrainUnitCommand(TradePostAId(player), UnitTypeId.TradeCart));
                Add(30, player, CommandType.CreateTradeRoute, new CreateTradeRouteCommand(TradeCartId(player), TradePostAId(player), TradePostBId(player)));
                Add(40, player, InvalidPlacementForPlayer(player));
                Add(50, player, CommandType.PlaceWall, new PlaceWallCommand(WallPosition(player)));

                for (int i = 0; i < ExtraInfantryPerPlayer; i++)
                {
                    Add(100, player, CommandType.Attack, new AttackCommand(new[] { ExtraInfantryId(player, i) }, ExtraInfantryId((player + 1) % PlayerCount, i)));
                }

                Add(200, player, CommandType.Attack, new AttackCommand(ExtraSiegeIds(player), CapitalId((player + 1) % PlayerCount)));
                Add(260, player, CommandType.Attack, new AttackCommand(ExtraSiegeIds(player), TradePostBId((player + 1) % PlayerCount)));
            }

            Add(1000, 0, CommandType.Resign, new ResignCommand());
            Add(1000, 1, CommandType.Resign, new ResignCommand());
            Add(1001, 2, CommandType.Resign, new ResignCommand());
            Add(1001, 3, CommandType.Resign, new ResignCommand());
            Add(1002, 4, CommandType.Resign, new ResignCommand());
        }

        private CommandEnvelope InvalidPlacementForPlayer(int player)
        {
            switch (player)
            {
                case 0:
                    return Create(40, player, CommandType.PlaceWall, new PlaceWallCommand(CapitalPosition(player)));
                case 1:
                    return Create(40, player, CommandType.PlaceTownCenter, new PlaceTownCenterCommand(FixedVector2.FromInts(46, 0)));
                case 2:
                    return Create(40, player, CommandType.PlaceTradePost, new PlaceTradePostCommand(TradePostAPosition(player)));
                case 3:
                    return Create(40, player, CommandType.PlaceWall, new PlaceWallCommand(FixedVector2.FromInts(-1, 0)));
                case 4:
                    return Create(40, player, CommandType.PlaceTownCenter, new PlaceTownCenterCommand(SecondTownCenterPosition(player)));
                default:
                    return Create(40, player, CommandType.PlaceTradePost, new PlaceTradePostCommand(FixedVector2.FromInts(86, 40)));
            }
        }

        private void Add(int tick, int player, CommandType type, ICommand payload)
        {
            AddEnvelope(tick, Create(tick, player, type, payload));
        }

        private void Add(int tick, int player, CommandEnvelope command)
        {
            AddEnvelope(tick, command);
        }

        private void AddEnvelope(int tick, CommandEnvelope command)
        {
            if (tick < 0 || tick >= _commandsByTick.Length)
            {
                return;
            }

            if (_commandsByTick[tick] == null)
            {
                _commandsByTick[tick] = new List<CommandEnvelope>();
            }

            _commandsByTick[tick].Add(command);
            CommandCount++;
        }

        private CommandEnvelope Create(int tick, int player, CommandType type, ICommand payload)
        {
            return new CommandEnvelope(new CommandHeader(tick, player, _nextSequence[player]++, type), payload);
        }

        private static int StartingVillagerId(int player, int offset)
        {
            return player * 5 + 1 + offset;
        }

        private static int[] StartingVillagers(int player)
        {
            return new[] { StartingVillagerId(player, 0), StartingVillagerId(player, 1), StartingVillagerId(player, 2), StartingVillagerId(player, 3) };
        }

        public static int ExtraInfantryId(int player, int offset)
        {
            return 31 + player * ExtraUnitsPerPlayer + offset;
        }

        private static int[] ExtraSiegeIds(int player)
        {
            return new[]
            {
                31 + player * ExtraUnitsPerPlayer + ExtraInfantryPerPlayer,
                31 + player * ExtraUnitsPerPlayer + ExtraInfantryPerPlayer + 1,
                31 + player * ExtraUnitsPerPlayer + ExtraInfantryPerPlayer + 2,
                31 + player * ExtraUnitsPerPlayer + ExtraInfantryPerPlayer + 3
            };
        }

        public static int CapitalId(int player)
        {
            return 115 + player;
        }

        private static int SecondTownCenterId(int player)
        {
            return 121 + player;
        }

        private static int TradePostAId(int player)
        {
            return 139 + player;
        }

        private static int TradePostBId(int player)
        {
            return 145 + player;
        }

        private static int TradeCartId(int player)
        {
            return 151 + player;
        }

        private static FixedVector2 CapitalPosition(int player)
        {
            switch (player)
            {
                case 0: return FixedVector2.FromInts(10, 10);
                case 1: return FixedVector2.FromInts(50, 10);
                case 2: return FixedVector2.FromInts(90, 10);
                case 3: return FixedVector2.FromInts(10, 50);
                case 4: return FixedVector2.FromInts(50, 60);
                default: return FixedVector2.FromInts(90, 50);
            }
        }

        private static FixedVector2 SecondTownCenterPosition(int player)
        {
            FixedVector2 capital = CapitalPosition(player);
            return new FixedVector2(capital.X + Fixed.FromInt(8), capital.Y + Fixed.FromInt(8));
        }

        private static FixedVector2 TradePostAPosition(int player)
        {
            FixedVector2 capital = CapitalPosition(player);
            return new FixedVector2(capital.X + Fixed.FromInt(14), capital.Y);
        }

        private static FixedVector2 TradePostBPosition(int player)
        {
            FixedVector2 capital = CapitalPosition(player);
            return new FixedVector2(capital.X + Fixed.FromInt(24), capital.Y);
        }

        private static FixedVector2 WallPosition(int player)
        {
            FixedVector2 capital = CapitalPosition(player);
            return new FixedVector2(capital.X + Fixed.FromInt(4), capital.Y + Fixed.FromInt(14));
        }
    }
}
