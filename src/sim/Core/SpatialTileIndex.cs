using System.Collections.Generic;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public sealed class SpatialTileIndex
    {
        private readonly HashSet<int> _wallTiles = new HashSet<int>();
        private readonly HashSet<int> _resourceBlockedTiles = new HashSet<int>();
        private readonly Dictionary<int, int> _buildingBlockedCounts = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _unitOccupancyCounts = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _reservedTileCounts = new Dictionary<int, int>();
        private bool _isWarm;
        private int _warmTick = int.MinValue;

        public void Rebuild(GameState state)
        {
            _wallTiles.Clear();
            _resourceBlockedTiles.Clear();
            _buildingBlockedCounts.Clear();
            _unitOccupancyCounts.Clear();
            _reservedTileCounts.Clear();

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead)
                {
                    continue;
                }

                List<SpatialRules.TileCoord> footprintTiles = SpatialRules.EnumerateBuildingFootprintTiles(state, building);
                for (int tileIndex = 0; tileIndex < footprintTiles.Count; tileIndex++)
                {
                    int key = SpatialRules.EncodeTileKey(footprintTiles[tileIndex].X, footprintTiles[tileIndex].Y);
                    AddCount(_buildingBlockedCounts, key);
                    if (building.BuildingTypeId == BuildingTypeId.Wall)
                    {
                        _wallTiles.Add(key);
                    }
                }
            }

            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted)
                {
                    continue;
                }

                GatherProfile profile = GameData.GetGatherProfile(node.GatherProfileId);
                if (!profile.BlocksMovement)
                {
                    continue;
                }

                List<SpatialRules.TileCoord> footprintTiles = SpatialRules.EnumerateResourceFootprintTiles(state, node);
                for (int tileIndex = 0; tileIndex < footprintTiles.Count; tileIndex++)
                {
                    int key = SpatialRules.EncodeTileKey(footprintTiles[tileIndex].X, footprintTiles[tileIndex].Y);
                    _resourceBlockedTiles.Add(key);
                }
            }

            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead)
                {
                    continue;
                }

                int occupiedKey = SpatialRules.EncodeTileKey(SpatialRules.GetTileX(unit.Position), SpatialRules.GetTileY(unit.Position));
                AddCount(_unitOccupancyCounts, occupiedKey);

                if (unit.ReservedInteractionKind != InteractionReservationKind.None)
                {
                    int reservedKey = SpatialRules.EncodeTileKey(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                    AddCount(_reservedTileCounts, reservedKey);
                }
            }

            _isWarm = true;
            _warmTick = state.Tick;
        }

        public void EnsureWarm(GameState state)
        {
            if (!_isWarm || _warmTick != state.Tick)
            {
                Rebuild(state);
            }
        }

        public bool IsBlockedByWall(int tileX, int tileY)
        {
            return _wallTiles.Contains(SpatialRules.EncodeTileKey(tileX, tileY));
        }

        public bool IsBlockedByBuilding(int tileX, int tileY, int ignoredBuildingId)
        {
            int key = SpatialRules.EncodeTileKey(tileX, tileY);
            if (!_buildingBlockedCounts.TryGetValue(key, out int count) || count <= 0)
            {
                return false;
            }

            if (ignoredBuildingId <= 0)
            {
                return true;
            }

            return count > 1;
        }

        public bool IsBlockedByResource(int tileX, int tileY)
        {
            return _resourceBlockedTiles.Contains(SpatialRules.EncodeTileKey(tileX, tileY));
        }

        public bool IsOccupiedByLiveUnit(int tileX, int tileY, int ignoredUnitId)
        {
            int key = SpatialRules.EncodeTileKey(tileX, tileY);
            if (!_unitOccupancyCounts.TryGetValue(key, out int count) || count <= 0)
            {
                return false;
            }

            if (ignoredUnitId <= 0)
            {
                return true;
            }

            return count > 1;
        }

        public bool IsReservedByLiveUnit(int tileX, int tileY, int ignoredUnitId)
        {
            int key = SpatialRules.EncodeTileKey(tileX, tileY);
            if (!_reservedTileCounts.TryGetValue(key, out int count) || count <= 0)
            {
                return false;
            }

            if (ignoredUnitId <= 0)
            {
                return true;
            }

            return count > 1;
        }

        public void ApplyReservationChange(int previousTileKey, bool hadReservation, int nextTileKey, bool hasReservation)
        {
            _isWarm = true;
            if (hadReservation)
            {
                RemoveCount(_reservedTileCounts, previousTileKey);
            }

            if (hasReservation)
            {
                AddCount(_reservedTileCounts, nextTileKey);
            }
        }

        private static void AddCount(Dictionary<int, int> counts, int key)
        {
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }

        private static void RemoveCount(Dictionary<int, int> counts, int key)
        {
            if (!counts.TryGetValue(key, out int count))
            {
                return;
            }

            if (count <= 1)
            {
                counts.Remove(key);
                return;
            }

            counts[key] = count - 1;
        }
    }
}
