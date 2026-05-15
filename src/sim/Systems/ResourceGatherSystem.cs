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
                if (!SpatialRules.IsUnitInResourceInteractionRange(unit, node))
                {
                    if (ShouldKeepCurrentApproachTarget(state, unit, node))
                    {
                        reservedApproachTiles.Add(EncodeTile(SpatialRules.GetTileX(unit.MoveTarget), SpatialRules.GetTileY(unit.MoveTarget)));
                        continue;
                    }

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

        private static bool TryChooseResourceApproachTile(GameState state, Unit unit, ResourceNode node, HashSet<int> reservedApproachTiles, out int approachX, out int approachY)
        {
            approachX = 0;
            approachY = 0;
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);
            if (!SpatialRules.TryChooseNearestReachableInteractionTile(state, unit, interactionTiles, reservedApproachTiles, out SpatialRules.TileCoord selected))
            {
                return false;
            }

            approachX = selected.X;
            approachY = selected.Y;
            return true;
        }

        private static bool ShouldKeepCurrentApproachTarget(GameState state, Unit unit, ResourceNode node)
        {
            if (!unit.HasMoveTarget)
            {
                return false;
            }

            int targetX = SpatialRules.GetTileX(unit.MoveTarget);
            int targetY = SpatialRules.GetTileY(unit.MoveTarget);
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);
            if (!SpatialRules.ContainsInteractionTile(interactionTiles, targetX, targetY))
            {
                return false;
            }

            return true;
        }

        private static int EncodeTile(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }

        private static int Min(int a, int b, int c)
        {
            int result = a < b ? a : b;
            return result < c ? result : c;
        }

    }
}
