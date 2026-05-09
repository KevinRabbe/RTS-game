using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    internal static class PlacementRules
    {
        public static bool CanPlaceBuilding(GameState state, BuildingTypeId buildingTypeId, FixedVector2 position)
        {
            if (!IsInsideMap(position))
            {
                return false;
            }

            int radiusTiles = GameData.GetBuildingPlacementRadiusTiles(buildingTypeId);
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead)
                {
                    continue;
                }

                int otherRadiusTiles = GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId);
                if (IsWithinCombinedRadius(position, radiusTiles, building.Position, otherRadiusTiles))
                {
                    return false;
                }
            }

            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted)
                {
                    continue;
                }

                if (IsWithinCombinedRadius(position, radiusTiles, node.Position, GameData.ResourcePlacementRadiusTiles))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsInsideMap(FixedVector2 position)
        {
            return position.X.Raw >= 0
                && position.Y.Raw >= 0
                && position.X < Fixed.FromInt(GameData.MapWidthTiles)
                && position.Y < Fixed.FromInt(GameData.MapHeightTiles);
        }

        private static bool IsWithinCombinedRadius(FixedVector2 position, int radiusTiles, FixedVector2 otherPosition, int otherRadiusTiles)
        {
            long combinedRaw = Fixed.FromInt(radiusTiles + otherRadiusTiles).Raw;
            long combinedSquaredRaw = checked(combinedRaw * combinedRaw);
            return (position - otherPosition).LengthSquaredRaw() < combinedSquaredRaw;
        }
    }
}
