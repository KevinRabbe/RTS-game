using System.Collections.Generic;

namespace RtsGame.Sim.Core
{
    public sealed class SpatialIndexService : ISpatialIndexService
    {
        public bool IsStaticBlocked(GameState state, int tileX, int tileY, int ignoredBuildingId = 0)
        {
            return !SpatialRules.IsTileInBounds(state, tileX, tileY)
                || state.SpatialTileIndex.IsBlockedByWall(tileX, tileY)
                || state.SpatialTileIndex.IsBlockedByBuilding(tileX, tileY, ignoredBuildingId)
                || state.SpatialTileIndex.IsBlockedByResource(tileX, tileY);
        }

        public bool IsOccupied(GameState state, int tileX, int tileY, int ignoredUnitId = 0)
        {
            state.SpatialTileIndex.EnsureWarm(state);
            return state.SpatialTileIndex.IsOccupiedByLiveUnit(tileX, tileY, ignoredUnitId);
        }

        public bool TryGetOccupiedUnitId(GameState state, int tileX, int tileY, int ignoredUnitId, out int unitId)
        {
            state.SpatialTileIndex.EnsureWarm(state);
            return state.SpatialTileIndex.TryGetOccupiedUnitId(tileX, tileY, ignoredUnitId, out unitId);
        }

        public bool IsReserved(GameState state, int tileX, int tileY, int ignoredUnitId = 0)
        {
            state.SpatialTileIndex.EnsureWarm(state);
            return state.SpatialTileIndex.IsReservedByLiveUnit(tileX, tileY, ignoredUnitId);
        }

        public void EnumerateInteractionRing(GameState state, List<SpatialRules.TileCoord> footprintTiles, int ignoredBuildingId, List<SpatialRules.TileCoord> output)
        {
            output.Clear();
            for (int i = 0; i < footprintTiles.Count; i++)
            {
                SpatialRules.TileCoord footprint = footprintTiles[i];
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                        {
                            continue;
                        }

                        int x = footprint.X + offsetX;
                        int y = footprint.Y + offsetY;
                        if (!SpatialRules.IsTileInBounds(state, x, y)
                            || Contains(footprintTiles, x, y)
                            || Contains(output, x, y)
                            || SpatialRules.IsTileBlockedForUnitMovement(state, x, y, ignoredBuildingId))
                        {
                            continue;
                        }

                        output.Add(new SpatialRules.TileCoord(x, y));
                    }
                }
            }

            output.Sort((left, right) =>
            {
                int yCompare = left.Y.CompareTo(right.Y);
                return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
            });
        }

        private static bool Contains(IReadOnlyList<SpatialRules.TileCoord> tiles, int x, int y)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].X == x && tiles[i].Y == y)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
