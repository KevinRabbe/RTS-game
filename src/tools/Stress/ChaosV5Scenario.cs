using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Stress
{
    public sealed class ChaosV5Scenario : IStressScenario
    {
        public const string Name = "chaos-v5";
        public const int Version = 1;
        public const int ScenarioPlayerCount = 6;

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

        public ChaosV5Scenario(int ticks)
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
            }
        }

        private void BuildSchedule()
        {
            for (int player = 0; player < ScenarioPlayerCount; player++)
            {
                int[] allVillagers = VillagerGroup(player, 4);
                int[] gatherA = VillagerGroup(player, 2);
                int[] gatherB = VillagerGroup(player, 3);

                Add(0, player, CommandType.MoveUnits, new MoveUnitsCommand(allVillagers, MidLaneTarget(player, 0)));
                Add(20, player, CommandType.MoveUnits, new MoveUnitsCommand(allVillagers, MidLaneTarget(player, 1)));
                Add(40, player, CommandType.MoveUnits, new MoveUnitsCommand(allVillagers, MidLaneTarget(player, 2)));
                Add(60, player, CommandType.MoveUnits, new MoveUnitsCommand(allVillagers, MidLaneTarget(player, 0)));

                Add(90, player, CommandType.GatherResource, new GatherResourceCommand(FoodNodeId(player), gatherA));
                Add(120, player, CommandType.GatherResource, new GatherResourceCommand(WoodNodeId(player), gatherB));

                Add(190, player, CommandType.MoveUnits, new MoveUnitsCommand(allVillagers, MidLaneTarget(player, 3)));
                Add(220, player, CommandType.GatherResource, new GatherResourceCommand(GoldNodeId(player), allVillagers));
                Add(260, player, CommandType.MoveUnits, new MoveUnitsCommand(allVillagers, MidLaneTarget(player, 1)));
                Add(300, player, CommandType.GatherResource, new GatherResourceCommand(FoodNodeId(player), gatherB));
                Add(340, player, CommandType.GatherResource, new GatherResourceCommand(GoldNodeId(player), gatherA));
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

        private static int[] VillagerGroup(int player, int count)
        {
            int[] ids = new int[count];
            int firstVillagerId = 1 + player * 5;
            for (int i = 0; i < count; i++)
            {
                ids[i] = firstVillagerId + i;
            }

            return ids;
        }

        private static int FoodNodeId(int player)
        {
            return player * 3 + 1;
        }

        private static int WoodNodeId(int player)
        {
            return player * 3 + 2;
        }

        private static int GoldNodeId(int player)
        {
            return player * 3 + 3;
        }

        private static FixedVector2 CapitalPosition(int player)
        {
            int x = (player % 3) * 40 + 14;
            int y = (player / 3) * 40 + 14;
            return FixedVector2.FromInts(x, y);
        }

        private static FixedVector2 MidLaneTarget(int player, int variant)
        {
            int baseX = 56 + ((player + variant) % 3) * 4;
            int baseY = 30 + (player / 3) * 22 + (variant % 2) * 4;
            return FixedVector2.FromInts(baseX, baseY);
        }
    }
}
