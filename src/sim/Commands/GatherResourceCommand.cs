using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class GatherResourceCommand : ICommand
    {
        public int ResourceNodeId { get; }
        public IReadOnlyList<int> UnitIds { get; }

        public GatherResourceCommand(int resourceNodeId, IReadOnlyList<int> unitIds)
        {
            ResourceNodeId = resourceNodeId;
            UnitIds = unitIds;
        }

        public CommandType Type
        {
            get { return CommandType.GatherResource; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteInt32(ResourceNodeId);
            writer.WriteListCount(UnitIds.Count);
            for (int i = 0; i < UnitIds.Count; i++)
            {
                writer.WriteInt32(UnitIds[i]);
            }
        }

        public bool IsValid(GameState state, GameRules rules, CommandHeader header)
        {
            return CommandValidationInspector.IsAccepted(GetValidationReason(state, rules, header));
        }

        public CommandValidationReason GetValidationReason(GameState state, GameRules rules, CommandHeader header)
        {
            if (header.CommandType != Type || header.Tick != state.Tick || header.PlayerIndex < 0 || header.PlayerIndex >= rules.MaxPlayers)
            {
                return CommandValidationReason.InvalidHeader;
            }

            if (UnitIds.Count == 0)
            {
                return CommandValidationReason.UnitCannotPerformAction;
            }

            if (!TryGetResourceNode(state, ResourceNodeId, out ResourceNode? node))
            {
                return CommandValidationReason.TargetMissing;
            }

            if (node.IsDepleted)
            {
                return CommandValidationReason.TargetComplete;
            }

            var seen = new HashSet<int>();
            for (int i = 0; i < UnitIds.Count; i++)
            {
                int unitId = UnitIds[i];
                if (!seen.Add(unitId) || !TryGetUnit(state, unitId, out Unit? unit))
                {
                    return CommandValidationReason.DuplicateUnitSelection;
                }

                if (unit.OwnerPlayerIndex != header.PlayerIndex || unit.IsDead || unit.UnitTypeId != UnitTypeId.Villager)
                {
                    return CommandValidationReason.UnitCannotPerformAction;
                }

                if (unit.CarriedResourceType != ResourceType.None && unit.CarriedResourceType != node.ResourceType)
                {
                    return CommandValidationReason.UnitCannotPerformAction;
                }
            }

            return CommandValidationReason.Accepted;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            ResourceNode clickedNode = GetResourceNode(state, ResourceNodeId);
            List<int> sortedUnitIds = StableSort.Sorted(UnitIds, (left, right) => left.CompareTo(right));
            bool preferClickedNodeFirst = sortedUnitIds.Count == 1;
            for (int i = 0; i < sortedUnitIds.Count; i++)
            {
                Unit unit = GetUnit(state, sortedUnitIds[i]);
                ClearBuildAssignment(state, unit);
                SpatialRules.ClearInteractionReservation(state, unit);
                unit.CurrentResourceAreaId = clickedNode.ResourceAreaId;
                unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
                unit.AttackTargetId = 0;
                unit.IsSiegeDeployed = false;
                unit.SiegeSetupTicksRemaining = 0;
                unit.SiegeReloadTicksRemaining = 0;
                unit.LastGatherFallbackReason = GatherFallbackReason.ExplicitRetarget;
                unit.CurrentResourceNodeId = clickedNode.Id;
                unit.AssignedResourceNodeId = clickedNode.Id;
                if (ResourceGatherTargeting.TryChooseResourceNodeAndReserveSlot(
                    state,
                    unit,
                    clickedNode.ResourceAreaId,
                    clickedNode.Id,
                    preferClickedNodeFirst,
                    true,
                    out ResourceNode? selectedNode))
                {
                    unit.CurrentResourceNodeId = selectedNode!.Id;
                    unit.HasMoveTarget = true;
                    unit.MoveTarget = FixedVector2.FromInts(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                }
                else
                {
                    unit.TaskPhase = WorkerTaskPhase.BlockedWaiting;
                    unit.HasMoveTarget = false;
                }
            }
        }

        private static void ClearBuildAssignment(GameState state, Unit unit)
        {
            int previousTargetId = unit.CurrentBuildTargetId;
            unit.CurrentBuildTargetId = 0;
            if (previousTargetId == 0 || !TryGetBuilding(state, previousTargetId, out Building? building))
            {
                return;
            }

            building.AssignedBuilderIds.Remove(unit.Id);
        }

        private static bool TryGetResourceNode(GameState state, int resourceNodeId, [NotNullWhen(true)] out ResourceNode? node)
        {
            node = null;
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                if (state.EconomyState.ResourceNodes[i].Id == resourceNodeId)
                {
                    node = state.EconomyState.ResourceNodes[i];
                    return true;
                }
            }

            return false;
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

        private static Unit GetUnit(GameState state, int unitId)
        {
            TryGetUnit(state, unitId, out Unit? unit);
            return unit!;
        }

        private static ResourceNode GetResourceNode(GameState state, int nodeId)
        {
            TryGetResourceNode(state, nodeId, out ResourceNode? node);
            return node!;
        }
    }
}

