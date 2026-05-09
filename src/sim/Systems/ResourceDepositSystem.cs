using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class ResourceDepositSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.CarriedAmount < GameData.VillagerCarryCapacity || unit.CarriedResourceType == ResourceType.None)
                {
                    continue;
                }

                if (!HasCompletedTownCenter(state, unit.OwnerPlayerIndex))
                {
                    continue;
                }

                state.PlayerStates.Players[unit.OwnerPlayerIndex].Resources.Add(unit.CarriedResourceType, unit.CarriedAmount);
                unit.CarriedAmount = 0;
                unit.CarriedResourceType = ResourceType.None;
            }
        }

        private static bool HasCompletedTownCenter(GameState state, int ownerPlayerIndex)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsDead
                    && !building.IsUnderConstruction
                    && building.OwnerPlayerIndex == ownerPlayerIndex
                    && building.BuildingTypeId == BuildingTypeId.TownCenter)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
