using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class EliminationSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int playerIndex = 0; playerIndex < state.PlayerStates.Players.Count; playerIndex++)
            {
                PlayerState player = state.PlayerStates.Players[playerIndex];
                if (player.IsDefeated)
                {
                    continue;
                }

                if (!HasLivingTownCenter(state, playerIndex) && !HasLivingVillager(state, playerIndex))
                {
                    player.IsDefeated = true;
                    player.PopulationUsed = CountLivingPopulation(state, playerIndex);
                    player.PopulationCap = 0;
                    player.CapitalStatus.IsCapitalAlive = false;
                    player.CapitalStatus.CapitalBonusActive = false;
                }
            }
        }

        private static bool HasLivingTownCenter(GameState state, int playerIndex)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsDead
                    && building.OwnerPlayerIndex == playerIndex
                    && building.BuildingTypeId == BuildingTypeId.TownCenter)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLivingVillager(GameState state, int playerIndex)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!unit.IsDead
                    && unit.OwnerPlayerIndex == playerIndex
                    && unit.UnitTypeId == UnitTypeId.Villager)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountLivingPopulation(GameState state, int playerIndex)
        {
            int population = 0;
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!unit.IsDead && unit.OwnerPlayerIndex == playerIndex)
                {
                    population += GameData.GetUnitPopulation(unit.UnitTypeId);
                }
            }

            return population;
        }
    }
}
