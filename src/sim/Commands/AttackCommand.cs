using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class AttackCommand : ICommand
    {
        public IReadOnlyList<int> AttackerUnitIds { get; }
        public int TargetEntityId { get; }

        public AttackCommand(IReadOnlyList<int> attackerUnitIds, int targetEntityId)
        {
            AttackerUnitIds = attackerUnitIds;
            TargetEntityId = targetEntityId;
        }

        public CommandType Type
        {
            get { return CommandType.Attack; }
        }

        public void WritePayload(CanonicalWriter writer)
        {
            writer.WriteListCount(AttackerUnitIds.Count);
            for (int i = 0; i < AttackerUnitIds.Count; i++)
            {
                writer.WriteInt32(AttackerUnitIds[i]);
            }

            writer.WriteInt32(TargetEntityId);
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

            if (AttackerUnitIds.Count == 0 || !TryGetTargetInfo(state, TargetEntityId, out TargetInfo targetInfo))
            {
                return CommandValidationReason.TargetMissing;
            }

            if (targetInfo.OwnerPlayerIndex == header.PlayerIndex)
            {
                return CommandValidationReason.WrongOwner;
            }

            var seen = new HashSet<int>();
            for (int i = 0; i < AttackerUnitIds.Count; i++)
            {
                int unitId = AttackerUnitIds[i];
                if (!seen.Add(unitId) || !TryGetUnit(state, unitId, out Unit? unit))
                {
                    return CommandValidationReason.DuplicateUnitSelection;
                }

                if (unit.OwnerPlayerIndex != header.PlayerIndex || unit.IsDead || !CanAttackTarget(unit.UnitTypeId, targetInfo.Kind))
                {
                    return CommandValidationReason.UnitCannotPerformAction;
                }
            }

            return CommandValidationReason.Accepted;
        }

        public void Execute(GameState state, GameRules rules, CommandHeader header)
        {
            List<int> sortedUnitIds = StableSort.Sorted(AttackerUnitIds, (left, right) => left.CompareTo(right));
            for (int i = 0; i < sortedUnitIds.Count; i++)
            {
                Unit unit = GetUnit(state, sortedUnitIds[i]);
                ClearBuildAssignment(state, unit);
                SpatialRules.ClearInteractionReservation(state, unit);
                unit.CurrentResourceAreaId = 0;
                unit.CurrentResourceNodeId = 0;
                unit.AssignedResourceNodeId = 0;
                unit.TaskPhase = WorkerTaskPhase.Idle;
                unit.HasMoveTarget = false;
                unit.AttackTargetId = TargetEntityId;
                if (!GameData.IsSiege(unit.UnitTypeId))
                {
                    unit.IsSiegeDeployed = false;
                    unit.SiegeSetupTicksRemaining = 0;
                    unit.SiegeReloadTicksRemaining = 0;
                }
            }
        }

        private static bool CanAttackTarget(UnitTypeId unitTypeId, EntityKind targetKind)
        {
            if (GameData.IsSiege(unitTypeId))
            {
                return targetKind == EntityKind.Building && GameData.GetSiegeBuildingDamage(unitTypeId) > 0;
            }

            if (GameData.IsAreaDamage(unitTypeId))
            {
                return GameData.GetAreaDamage(unitTypeId) > 0
                    && ((targetKind == EntityKind.Unit && GameData.CanAreaDamageHitUnits(unitTypeId))
                        || (targetKind == EntityKind.Building && GameData.CanAreaDamageHitBuildings(unitTypeId)));
            }

            return GameData.GetUnitAttackDamage(unitTypeId) > 0;
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

        private static bool TryGetTargetInfo(GameState state, int entityId, out TargetInfo targetInfo)
        {
            targetInfo = default(TargetInfo);
            if (!state.EntityState.EntityLookup.TryGetValue(entityId, out EntityRef entityRef))
            {
                return false;
            }

            if (entityRef.Kind == EntityKind.Unit)
            {
                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
                {
                    return false;
                }

                Unit unit = state.EntityState.Units[entityRef.Index];
                if (unit.Id != entityId || unit.IsDead)
                {
                    return false;
                }

                targetInfo = new TargetInfo(unit.OwnerPlayerIndex, EntityKind.Unit);
                return true;
            }

            if (entityRef.Kind == EntityKind.Building)
            {
                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Buildings.Count)
                {
                    return false;
                }

                Building building = state.EntityState.Buildings[entityRef.Index];
                if (building.Id != entityId || building.IsDead)
                {
                    return false;
                }

                targetInfo = new TargetInfo(building.OwnerPlayerIndex, EntityKind.Building);
                return true;
            }

            return false;
        }

        private readonly struct TargetInfo
        {
            public int OwnerPlayerIndex { get; }
            public EntityKind Kind { get; }

            public TargetInfo(int ownerPlayerIndex, EntityKind kind)
            {
                OwnerPlayerIndex = ownerPlayerIndex;
                Kind = kind;
            }
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
    }
}

