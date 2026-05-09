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

        private static bool IsInsideRadius(FixedVector2 position, FixedVector2 center, int radiusTiles)
        {
            long radiusRaw = Fixed.FromInt(radiusTiles).Raw;
            long radiusSquaredRaw = checked(radiusRaw * radiusRaw);
            return (position - center).LengthSquaredRaw() < radiusSquaredRaw;
        }
    }
}
