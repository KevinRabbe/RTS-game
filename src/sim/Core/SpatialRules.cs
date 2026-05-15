using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Core
{
    public static class SpatialRules
    {
        public static int GetTileX(FixedVector2 position)
        {
            return position.X.FloorToInt();
        }

        public static int GetTileY(FixedVector2 position)
        {
            return position.Y.FloorToInt();
        }

        public static bool IsBlockedByWall(GameState state, FixedVector2 position)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || building.BuildingTypeId != BuildingTypeId.Wall)
                {
                    continue;
                }

                if (IsInsideRadius(position, building.Position, GameData.WallPlacementRadiusTiles))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileBlockedByWall(GameState state, int tileX, int tileY)
        {
            return IsBlockedByWall(state, FixedVector2.FromInts(tileX, tileY));
        }

        public static bool IsTileInsideBuildingFootprint(Building building, int tileX, int tileY)
        {
            int radiusTiles = GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId);
            return IsInsideRadius(FixedVector2.FromInts(tileX, tileY), building.Position, radiusTiles);
        }

        public static bool IsTileBlockedByBuildingFootprint(GameState state, int tileX, int tileY, int ignoredBuildingId = 0)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || building.Id == ignoredBuildingId)
                {
                    continue;
                }

                if (IsTileInsideBuildingFootprint(building, tileX, tileY))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileBlockedByResource(GameState state, int tileX, int tileY)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted)
                {
                    continue;
                }

                if (IsInsideRadius(FixedVector2.FromInts(tileX, tileY), node.Position, GameData.ResourcePlacementRadiusTiles))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileOccupiedByLiveUnit(GameState state, int tileX, int tileY, int ignoredUnitId = 0)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.Id == ignoredUnitId)
                {
                    continue;
                }

                if (GetTileX(unit.Position) == tileX && GetTileY(unit.Position) == tileY)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTileInBounds(GameState state, int tileX, int tileY)
        {
            return tileX >= 0 && tileY >= 0 && tileX < state.MapState.WidthTiles && tileY < state.MapState.HeightTiles;
        }

        private static bool IsInsideRadius(FixedVector2 position, FixedVector2 center, int radiusTiles)
        {
            long radiusRaw = Fixed.FromInt(radiusTiles).Raw;
            long radiusSquaredRaw = checked(radiusRaw * radiusRaw);
            return (position - center).LengthSquaredRaw() < radiusSquaredRaw;
        }
    }
}
