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

            var footprintTiles = SpatialRules.EnumerateBuildingFootprintTiles(state, buildingTypeId, position);
            if (footprintTiles.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < footprintTiles.Count; i++)
            {
                SpatialRules.TileCoord tile = footprintTiles[i];
                if (!SpatialRules.IsTileInBounds(state, tile.X, tile.Y))
                {
                    return false;
                }

                if (SpatialRules.IsTileBlockedByWall(state, tile.X, tile.Y))
                {
                    return false;
                }

                if (SpatialRules.IsTileBlockedByBuildingFootprint(state, tile.X, tile.Y))
                {
                    return false;
                }

                if (SpatialRules.IsTileBlockedByResource(state, tile.X, tile.Y))
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

    }
}
