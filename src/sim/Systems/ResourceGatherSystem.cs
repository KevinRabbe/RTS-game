using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using System.Collections.Generic;

namespace RtsGame.Sim.Systems
{
    public sealed class ResourceGatherSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            var reservedApproachTiles = new HashSet<int>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.UnitTypeId != UnitTypeId.Villager || unit.CurrentResourceNodeId == 0)
                {
                    continue;
                }

                ResourceNode? node = FindNode(state, unit.CurrentResourceNodeId);
                if (node == null || node.IsDepleted)
                {
                    unit.CurrentResourceNodeId = 0;
                    continue;
                }

                if (unit.CarriedAmount >= GameData.VillagerCarryCapacity)
                {
                    continue;
                }

                int carryRoom = GameData.VillagerCarryCapacity - unit.CarriedAmount;
                if (!IsInGatherInteractionRange(unit, node))
                {
                    if (TryChooseResourceApproachTile(state, unit, node, reservedApproachTiles, out int approachX, out int approachY))
                    {
                        unit.HasMoveTarget = true;
                        unit.MoveTarget = FixedVector2.FromInts(approachX, approachY);
                        reservedApproachTiles.Add(EncodeTile(approachX, approachY));
                    }

                    continue;
                }

                unit.HasMoveTarget = false;
                int gathered = Min(GameData.VillagerGatherPerTick, carryRoom, node.RemainingAmount);
                if (gathered <= 0)
                {
                    continue;
                }

                if (unit.CarriedResourceType == ResourceType.None)
                {
                    unit.CarriedResourceType = node.ResourceType;
                }

                if (unit.CarriedResourceType != node.ResourceType)
                {
                    continue;
                }

                unit.CarriedAmount += gathered;
                node.RemainingAmount -= gathered;
                if (node.IsDepleted)
                {
                    unit.CurrentResourceNodeId = 0;
                }
            }
        }

        private static ResourceNode? FindNode(GameState state, int nodeId)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                if (state.EconomyState.ResourceNodes[i].Id == nodeId)
                {
                    return state.EconomyState.ResourceNodes[i];
                }
            }

            return null;
        }

        private static bool IsInGatherInteractionRange(Unit unit, ResourceNode node)
        {
            int tileX = SpatialRules.GetTileX(unit.Position);
            int tileY = SpatialRules.GetTileY(unit.Position);
            if (IsInsideResource(node, tileX, tileY))
            {
                return false;
            }

            return IsInsideResource(node, tileX + 1, tileY)
                || IsInsideResource(node, tileX - 1, tileY)
                || IsInsideResource(node, tileX, tileY + 1)
                || IsInsideResource(node, tileX, tileY - 1);
        }

        private static bool TryChooseResourceApproachTile(GameState state, Unit unit, ResourceNode node, HashSet<int> reservedApproachTiles, out int approachX, out int approachY)
        {
            approachX = 0;
            approachY = 0;
            int unitTileX = SpatialRules.GetTileX(unit.Position);
            int unitTileY = SpatialRules.GetTileY(unit.Position);
            int nodeTileX = SpatialRules.GetTileX(node.Position);
            int nodeTileY = SpatialRules.GetTileY(node.Position);
            int bestScore = int.MaxValue;
            bool found = false;

            for (int y = nodeTileY - 2; y <= nodeTileY + 2; y++)
            {
                for (int x = nodeTileX - 2; x <= nodeTileX + 2; x++)
                {
                    if (!SpatialRules.IsTileInBounds(state, x, y)
                        || !IsAdjacentToResource(node, x, y)
                        || IsInsideResource(node, x, y)
                        || reservedApproachTiles.Contains(EncodeTile(x, y))
                        || SpatialRules.IsTileBlockedByWall(state, x, y)
                        || SpatialRules.IsTileBlockedByBuildingFootprint(state, x, y)
                        || SpatialRules.IsTileBlockedByResource(state, x, y)
                        || SpatialRules.IsTileOccupiedByLiveUnit(state, x, y, unit.Id))
                    {
                        continue;
                    }

                    if (!DeterministicPathfinder.TryFindNextTile(state, unitTileX, unitTileY, x, y, out _, out _))
                    {
                        continue;
                    }

                    int score = Abs(unitTileX - x) + Abs(unitTileY - y);
                    if (!found || score < bestScore || (score == bestScore && (y < approachY || (y == approachY && x < approachX))))
                    {
                        approachX = x;
                        approachY = y;
                        bestScore = score;
                        found = true;
                    }
                }
            }

            return found;
        }

        private static bool IsAdjacentToResource(ResourceNode node, int tileX, int tileY)
        {
            return IsInsideResource(node, tileX + 1, tileY)
                || IsInsideResource(node, tileX - 1, tileY)
                || IsInsideResource(node, tileX, tileY + 1)
                || IsInsideResource(node, tileX, tileY - 1);
        }

        private static bool IsInsideResource(ResourceNode node, int tileX, int tileY)
        {
            long radiusRaw = Fixed.FromInt(GameData.ResourcePlacementRadiusTiles).Raw;
            long radiusSquaredRaw = checked(radiusRaw * radiusRaw);
            FixedVector2 tile = FixedVector2.FromInts(tileX, tileY);
            return (tile - node.Position).LengthSquaredRaw() < radiusSquaredRaw;
        }

        private static int Min(int a, int b, int c)
        {
            int result = a < b ? a : b;
            return result < c ? result : c;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }
    }
}
