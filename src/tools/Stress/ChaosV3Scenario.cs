using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Stress
{
    public sealed class ChaosV3Scenario : IStressScenario
    {
        public const string Name = "chaos-v3";
        public const int Version = 1;
        public const int ScenarioPlayerCount = 6;
        private const int EntitiesPerPlayer = 7;
        private const int FirstPreparedEntityId = 31;

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

        public ChaosV3Scenario(int ticks)
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
                EntityFactory.CreateUnit(state, player, UnitTypeId.Mangonel, MangonelPosition(player));
                for (int unit = 0; unit < 5; unit++)
                {
                    int unitId = EntityFactory.CreateUnit(state, player, UnitTypeId.Infantry, InfantryPosition(player, unit));
                    Unit infantry = state.EntityState.Units[state.EntityState.EntityLookup[unitId].Index];
                    infantry.HitPoints = GameData.MangonelAreaDamage * 2;
                }
            }
        }

        private void BuildSchedule()
        {
            for (int player = 0; player < ScenarioPlayerCount; player++)
            {
                int targetPlayer = (player + 1) % ScenarioPlayerCount;
                Add(0, player, CommandType.Attack, new AttackCommand(new[] { MangonelId(player) }, InfantryId(targetPlayer, 0)));
                Add(9, player, CommandType.Attack, new AttackCommand(new[] { MangonelId(player) }, InfantryId(targetPlayer, 0)));
                Add(20, player, CommandType.Attack, new AttackCommand(new[] { MangonelId(player) }, InfantryId(targetPlayer, 3)));
                Add(29, player, CommandType.Attack, new AttackCommand(new[] { MangonelId(player) }, InfantryId(targetPlayer, 3)));
                Add(60, player, CommandType.MoveUnits, new MoveUnitsCommand(new[] { MangonelId(player) }, MangonelFallbackPosition(player)));
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

        private static int BaseId(int player)
        {
            return FirstPreparedEntityId + player * EntitiesPerPlayer;
        }

        private static int CapitalId(int player)
        {
            return BaseId(player);
        }

        private static int MangonelId(int player)
        {
            return BaseId(player) + 1;
        }

        private static int InfantryId(int player, int unit)
        {
            return BaseId(player) + 2 + unit;
        }

        private static FixedVector2 CapitalPosition(int player)
        {
            return FixedVector2.FromInts(10 + player * 18, 10 + (player % 2) * 70);
        }

        private static FixedVector2 MangonelPosition(int player)
        {
            return FixedVector2.FromInts(20 + player * 14, 44 + (player % 2) * 8);
        }

        private static FixedVector2 MangonelFallbackPosition(int player)
        {
            return FixedVector2.FromInts(20 + player * 14, 56 + (player % 2) * 8);
        }

        private static FixedVector2 InfantryPosition(int player, int unit)
        {
            int baseX = 24 + ((player + 5) % ScenarioPlayerCount) * 14;
            int baseY = 44 + (((player + 5) % ScenarioPlayerCount) % 2) * 8;
            switch (unit)
            {
                case 0:
                    return FixedVector2.FromInts(baseX, baseY);
                case 1:
                    return FixedVector2.FromInts(baseX + 1, baseY);
                case 2:
                    return FixedVector2.FromInts(baseX, baseY + 1);
                case 3:
                    return FixedVector2.FromInts(baseX + 3, baseY);
                default:
                    return FixedVector2.FromInts(baseX + 4, baseY);
            }
        }
    }
}
