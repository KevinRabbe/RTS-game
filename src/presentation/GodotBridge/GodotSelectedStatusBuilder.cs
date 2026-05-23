using System.Collections.Generic;
using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Data;

namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotSelectedStatusBuilder
    {
        public static string[] BuildLines(GodotFrameDto? frame, IReadOnlyCollection<int> selectedUnitIds, int selectedBuildingId, int hoveredResourceNodeId)
        {
            string selectedUnits = selectedUnitIds.Count == 0 ? "none" : string.Join(",", selectedUnitIds);
            string selectedBuilding = selectedBuildingId == 0 ? "none" : selectedBuildingId.ToString();
            string hoveredResource = hoveredResourceNodeId == 0 ? "none" : hoveredResourceNodeId.ToString();

            string status = "SelectedUnits " + selectedUnits + "  SelectedBuilding " + selectedBuilding + "  HoveredResource " + hoveredResource;
            if (frame == null || selectedUnitIds.Count == 0)
            {
                return new[] { status, "Unit Status: none" };
            }

            foreach (int unitId in selectedUnitIds)
            {
                GodotUnitStatusDto? unit = FindUnitStatus(frame, unitId);
                if (unit != null)
                {
                    string move = unit.HasMoveTarget ? "MoveTargetRaw(" + unit.MoveTargetXRaw + "," + unit.MoveTargetYRaw + ")" : "MoveTarget -";
                    string build = unit.CurrentBuildTargetId == 0 ? "BuildTarget -" : "BuildTarget " + unit.CurrentBuildTargetId;
                    string resource = unit.CurrentResourceNodeId == 0 ? "ResourceTarget -" : "ResourceTarget " + unit.CurrentResourceNodeId;
                    string attack = unit.AttackTargetId == 0 ? "AttackTarget -" : "AttackTarget " + unit.AttackTargetId;
                    string cooldown = unit.AttackCooldownTicksRemaining <= 0 ? "AttackCooldown -" : "AttackCooldown " + unit.AttackCooldownTicksRemaining;
                    string health = unit.MaxHitPoints <= 0 ? "HP -" : "HP " + unit.CurrentHitPoints + "/" + unit.MaxHitPoints;
                    string position = "Tile(" + unit.PositionTileX + "," + unit.PositionTileY + ") PosRaw(" + unit.PositionXRaw + "," + unit.PositionYRaw + ")";
                    string phase = "Phase " + ResolveTaskPhaseLabel(unit.TaskPhaseId);
                    string reservation = unit.ReservedInteractionKindId == 0
                        ? "Reserve -"
                        : "Reserve " + ResolveReservationLabel(unit.ReservedInteractionKindId) + " " + unit.ReservedInteractionTargetId + " Tile(" + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY + ")";
                    string ranges = "Range R:" + FormatBool(unit.InResourceInteractionRange)
                        + " D:" + FormatBool(unit.InDropoffInteractionRange)
                        + " B:" + FormatBool(unit.InBuildInteractionRange);
                    bool isActiveMovementOrWaitPhase =
                        unit.TaskPhaseId == (int)WorkerTaskPhase.MovingToResourceSlot
                        || unit.TaskPhaseId == (int)WorkerTaskPhase.MovingToDropoffSlot
                        || unit.TaskPhaseId == (int)WorkerTaskPhase.MovingToBuildSlot
                        || unit.TaskPhaseId == (int)WorkerTaskPhase.MovingToCommandMove
                        || unit.TaskPhaseId == (int)WorkerTaskPhase.BlockedWaiting;
                    string noProgress = !isActiveMovementOrWaitPhase || unit.LastMovedTick < 0
                        ? "NoProgress -"
                        : "NoProgress " + (frame.Tick - unit.LastMovedTick);
                    bool isFullCarry = unit.CarriedAmount >= GameData.VillagerCarryCapacity;
                    string carry = unit.CarriedAmount == 0
                        ? "Carry -"
                        : "Carry " + unit.CarriedAmount + " " + ResolveResourceLabel(unit.CarriedResourceTypeId) + (isFullCarry ? " (Full)" : "");
                    string depositHint = "";
                    if (unit.CarriedAmount > 0 && !HasCompletedTownCenter(frame, frame.LocalPlayerIndex))
                    {
                        depositHint = "  DepositNeedsCompletedTC";
                    }

                    return new[] { status, position + "  " + move + "  " + phase + "  " + reservation + "  " + ranges + "  " + noProgress + "  " + build + "  " + resource + "  " + attack + "  " + cooldown + "  " + health + "  " + carry + depositHint };
                }
            }

            return new[] { status, "Unit Status: none" };
        }

        private static string ResolveTaskPhaseLabel(int taskPhaseId)
        {
            if (System.Enum.IsDefined(typeof(WorkerTaskPhase), taskPhaseId))
            {
                return ((WorkerTaskPhase)taskPhaseId).ToString();
            }

            return "Unknown(" + taskPhaseId + ")";
        }

        private static string ResolveReservationLabel(int reservationKindId)
        {
            if (System.Enum.IsDefined(typeof(InteractionReservationKind), reservationKindId))
            {
                return ((InteractionReservationKind)reservationKindId).ToString();
            }

            return "Unknown(" + reservationKindId + ")";
        }

        private static string FormatBool(bool value)
        {
            return value ? "Y" : "N";
        }

        private static string ResolveResourceLabel(int carriedResourceTypeId)
        {
            if (carriedResourceTypeId == (int)ResourceType.Food)
            {
                return "Food";
            }

            if (carriedResourceTypeId == (int)ResourceType.Wood)
            {
                return "Wood";
            }

            if (carriedResourceTypeId == (int)ResourceType.Gold)
            {
                return "Gold";
            }

            return "Unknown(" + carriedResourceTypeId + ")";
        }

        private static GodotUnitStatusDto? FindUnitStatus(GodotFrameDto frame, int unitId)
        {
            for (int i = 0; i < frame.UnitStatuses.Length; i++)
            {
                if (frame.UnitStatuses[i].UnitId == unitId)
                {
                    return frame.UnitStatuses[i];
                }
            }

            return null;
        }

        private static bool HasCompletedTownCenter(GodotFrameDto frame, int ownerPlayerIndex)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.OwnerPlayerIndex == ownerPlayerIndex
                    && primitive.TypeId == (int)BuildingTypeId.TownCenter
                    && primitive.Kind == (int)VisualPrimitiveKind.BuildingRectangle)
                {
                    for (int j = 0; j < frame.BuildingStatuses.Length; j++)
                    {
                        GodotBuildingStatusDto building = frame.BuildingStatuses[j];
                        if (building.BuildingId == primitive.EntityId && !building.IsUnderConstruction)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
