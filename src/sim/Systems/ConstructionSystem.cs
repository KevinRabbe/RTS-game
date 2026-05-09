using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    public sealed class ConstructionSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsUnderConstruction || building.IsDead)
                {
                    continue;
                }

                int builderCount = CountValidBuilders(state, building);
                if (builderCount <= 0)
                {
                    continue;
                }

                building.BuildProgressTicks += builderCount;
                int requiredTicks = GetRequiredBuildTicks(building.BuildingTypeId);
                if (building.BuildProgressTicks >= requiredTicks)
                {
                    building.BuildProgressTicks = requiredTicks;
                    building.IsUnderConstruction = false;
                    building.HitPoints = GameData.GetBuildingCompletedHitPoints(building.BuildingTypeId, building.IsCapital);
                    ClearBuilderOrders(state, building);
                    building.AssignedBuilderIds.Clear();
                }
            }
        }

        private static int CountValidBuilders(GameState state, Building building)
        {
            int count = 0;
            for (int i = 0; i < building.AssignedBuilderIds.Count; i++)
            {
                int builderId = building.AssignedBuilderIds[i];
                if (!state.EntityState.EntityLookup.TryGetValue(builderId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
                {
                    continue;
                }

                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
                {
                    continue;
                }

                Unit unit = state.EntityState.Units[entityRef.Index];
                if (!unit.IsDead && unit.OwnerPlayerIndex == building.OwnerPlayerIndex && unit.UnitTypeId == UnitTypeId.Villager && unit.CurrentBuildTargetId == building.Id)
                {
                    count++;
                }
            }

            return count;
        }

        private static void ClearBuilderOrders(GameState state, Building building)
        {
            for (int i = 0; i < building.AssignedBuilderIds.Count; i++)
            {
                int builderId = building.AssignedBuilderIds[i];
                if (!state.EntityState.EntityLookup.TryGetValue(builderId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
                {
                    continue;
                }

                if (entityRef.Index >= 0 && entityRef.Index < state.EntityState.Units.Count)
                {
                    state.EntityState.Units[entityRef.Index].CurrentBuildTargetId = 0;
                }
            }
        }

        private static int GetRequiredBuildTicks(BuildingTypeId buildingTypeId)
        {
            switch (buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    return GameData.TownCenterBuildTicks;
                case BuildingTypeId.Wall:
                    return GameData.WallBuildTicks;
                case BuildingTypeId.TradePost:
                    return GameData.TradePostBuildTicks;
                default:
                    return 1;
            }
        }
    }
}
