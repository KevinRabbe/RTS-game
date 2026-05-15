using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class AssignBuildCommand : ICommand
    {
        public int TargetBuildingId { get; }
        public IReadOnlyList<int> UnitIds { get; }

        public AssignBuildCommand(int targetBuildingId, IReadOnlyList<int> unitIds)
        {
            TargetBuildingId = targetBuildingId;
            UnitIds = unitIds;
        }

        public CommandType Type
        {
            get { return CommandType.AssignBuild; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(TargetBuildingId);
            writer.WriteListCount(UnitIds.Count);
            for (int i = 0; i < UnitIds.Count; i++)
            {
                writer.WriteInt32(UnitIds[i]);
            }
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return false;
            }

            if (UnitIds.Count == 0 || !TryGetBuilding(state, TargetBuildingId, out Building? building))
            {
                return false;
            }

            if (building.OwnerPlayerIndex != header.PlayerIndex || building.IsDead || !building.IsUnderConstruction)
            {
                return false;
            }

            List<TileCoord> interactionTiles = EnumerateBuildInteractionTiles(state, building);
            if (interactionTiles.Count == 0)
            {
                return false;
            }

            var seen = new HashSet<int>();
            for (int i = 0; i < UnitIds.Count; i++)
            {
                int unitId = UnitIds[i];
                if (!seen.Add(unitId) || !TryGetUnit(state, unitId, out Unit? unit))
                {
                    return false;
                }

                if (unit.OwnerPlayerIndex != header.PlayerIndex || unit.IsDead || unit.UnitTypeId != UnitTypeId.Villager)
                {
                    return false;
                }

                if (!CanUnitReachAnyInteractionTile(state, unit, interactionTiles))
                {
                    return false;
                }
            }

            return true;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            Building building = GetBuilding(state, TargetBuildingId);
            List<int> sortedUnitIds = StableSort.Sorted(UnitIds, (left, right) => left.CompareTo(right));
            List<TileCoord> availableTiles = EnumerateBuildInteractionTiles(state, building);
            var reservedTiles = new HashSet<TileCoord>();

            for (int i = 0; i < sortedUnitIds.Count; i++)
            {
                int unitId = sortedUnitIds[i];
                Unit unit = GetUnit(state, unitId);
                ClearPreviousBuildAssignment(state, unit);
                unit.CurrentBuildTargetId = TargetBuildingId;

                if (!building.AssignedBuilderIds.Contains(unitId))
                {
                    building.AssignedBuilderIds.Add(unitId);
                }

                unit.CurrentResourceNodeId = 0;
                unit.AttackTargetId = 0;
                unit.IsSiegeDeployed = false;
                unit.SiegeSetupTicksRemaining = 0;
                unit.SiegeReloadTicksRemaining = 0;

                if (TryChooseBuildApproachTile(state, unit, availableTiles, reservedTiles, out TileCoord approachTile))
                {
                    unit.HasMoveTarget = true;
                    unit.MoveTarget = FixedVector2.FromInts(approachTile.X, approachTile.Y);
                    reservedTiles.Add(approachTile);
                }
            }

            building.AssignedBuilderIds.Sort();
        }

        private static bool TryChooseBuildApproachTile(
            GameState state,
            Unit unit,
            List<TileCoord> availableTiles,
            HashSet<TileCoord> reservedTiles,
            out TileCoord selected)
        {
            selected = default;
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            bool found = false;
            int bestScore = int.MaxValue;

            for (int i = 0; i < availableTiles.Count; i++)
            {
                TileCoord tile = availableTiles[i];
                if (reservedTiles.Contains(tile))
                {
                    continue;
                }

                if (SpatialRules.IsTileOccupiedByLiveUnit(state, tile.X, tile.Y, unit.Id))
                {
                    continue;
                }

                if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, tile.X, tile.Y, out _, out _))
                {
                    continue;
                }

                int score = Abs(unitTileX - tile.X) + Abs(unitTileY - tile.Y);
                if (!found || score < bestScore || (score == bestScore && CompareTiles(tile, selected) < 0))
                {
                    selected = tile;
                    bestScore = score;
                    found = true;
                }
            }

            return found;
        }

        private static bool CanUnitReachAnyInteractionTile(GameState state, Unit unit, List<TileCoord> interactionTiles)
        {
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                TileCoord tile = interactionTiles[i];
                if (SpatialRules.IsTileOccupiedByLiveUnit(state, tile.X, tile.Y, unit.Id))
                {
                    continue;
                }

                if (DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, tile.X, tile.Y, out _, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<TileCoord> EnumerateBuildInteractionTiles(GameState state, Building building)
        {
            int centerX = SpatialRules.GetTileX(building.Position);
            int centerY = SpatialRules.GetTileY(building.Position);
            int radius = GameData.GetBuildingPlacementRadiusTiles(building.BuildingTypeId);
            var tiles = new List<TileCoord>();

            for (int y = centerY - radius - 1; y <= centerY + radius + 1; y++)
            {
                for (int x = centerX - radius - 1; x <= centerX + radius + 1; x++)
                {
                    if (!SpatialRules.IsTileInBounds(state, x, y))
                    {
                        continue;
                    }

                    if (SpatialRules.IsTileInsideBuildingFootprint(building, x, y))
                    {
                        continue;
                    }

                    if (!IsAdjacentToBuildingFootprint(building, x, y))
                    {
                        continue;
                    }

                    if (SpatialRules.IsTileBlockedByBuildingFootprint(state, x, y, building.Id)
                        || SpatialRules.IsTileBlockedByWall(state, x, y)
                        || SpatialRules.IsTileBlockedByResource(state, x, y))
                    {
                        continue;
                    }

                    tiles.Add(new TileCoord(x, y));
                }
            }

            tiles.Sort((left, right) => CompareTiles(left, right));
            return tiles;
        }

        private static bool IsAdjacentToBuildingFootprint(Building building, int tileX, int tileY)
        {
            return SpatialRules.IsTileInsideBuildingFootprint(building, tileX + 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX - 1, tileY)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX, tileY + 1)
                || SpatialRules.IsTileInsideBuildingFootprint(building, tileX, tileY - 1);
        }

        private static int CompareTiles(TileCoord left, TileCoord right)
        {
            int yCompare = left.Y.CompareTo(right.Y);
            return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static void ClearPreviousBuildAssignment(GameState state, Unit unit)
        {
            int previousTargetId = unit.CurrentBuildTargetId;
            if (previousTargetId == 0 || !TryGetBuilding(state, previousTargetId, out Building? previous))
            {
                return;
            }

            previous.AssignedBuilderIds.Remove(unit.Id);
        }

        private static bool TryGetBuilding(GameState state, int buildingId, [NotNullWhen(true)] out Building? building)
        {
            building = null;
            if (!state.EntityState.EntityLookup.TryGetValue(buildingId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Building)
            {
                return false;
            }

            if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Buildings.Count)
            {
                return false;
            }

            building = state.EntityState.Buildings[entityRef.Index];
            return building.Id == buildingId;
        }

        private static bool TryGetUnit(GameState state, int unitId, [NotNullWhen(true)] out Unit? unit)
        {
            unit = null;
            if (!state.EntityState.EntityLookup.TryGetValue(unitId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
            {
                return false;
            }

            if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
            {
                return false;
            }

            unit = state.EntityState.Units[entityRef.Index];
            return unit.Id == unitId;
        }

        private static Building GetBuilding(GameState state, int buildingId)
        {
            TryGetBuilding(state, buildingId, out Building? building);
            return building!;
        }

        private static Unit GetUnit(GameState state, int unitId)
        {
            TryGetUnit(state, unitId, out Unit? unit);
            return unit!;
        }

        private readonly struct TileCoord
        {
            public int X { get; }
            public int Y { get; }

            public TileCoord(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
