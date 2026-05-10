using System.Collections.Generic;
using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Data;

namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotSelectedStatusBuilder
    {
        public static string[] BuildLines(GodotFrameDto? frame, IReadOnlyCollection<int> selectedUnitIds, int selectedBuildingId, int hoveredResourceNodeId)
        {
            string selectedUnits = selectedUnitIds.Count == 0 ? "-" : string.Join(",", selectedUnitIds);
            string selectedBuilding = selectedBuildingId == 0 ? "-" : selectedBuildingId.ToString();
            string hoveredResource = hoveredResourceNodeId == 0 ? "-" : hoveredResourceNodeId.ToString();

            string status = "SelectedUnits " + selectedUnits + "  SelectedBuilding " + selectedBuilding + "  HoveredResource " + hoveredResource;
            if (frame == null || selectedUnitIds.Count == 0)
            {
                return new[] { status, "UnitStatus -" };
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
                    bool isFullCarry = unit.CarriedAmount >= GameData.VillagerCarryCapacity;
                    string carry = unit.CarriedAmount == 0
                        ? "Carry -"
                        : "Carry " + unit.CarriedAmount + " " + ResolveResourceLabel(unit.CarriedResourceTypeId) + (isFullCarry ? " (Full)" : "");
                    string depositHint = "";
                    if (unit.CarriedAmount > 0 && !HasCompletedTownCenter(frame, frame.LocalPlayerIndex))
                    {
                        depositHint = "  DepositNeedsCompletedTC";
                    }

                    return new[] { status, move + "  " + build + "  " + resource + "  " + attack + "  " + carry + depositHint };
                }
            }

            return new[] { status, "UnitStatus -" };
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
