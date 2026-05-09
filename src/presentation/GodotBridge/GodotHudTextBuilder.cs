namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotHudTextBuilder
    {
        public static string Build(
            GodotFrameDto frame,
            int[] selectedUnitIds,
            int selectedBuildingId,
            int hoveredResourceNodeId,
            bool paused)
        {
            GodotLocalPlayerDto player = frame.LocalPlayer;
            string selected = selectedUnitIds.Length == 0 ? "-" : string.Join(",", selectedUnitIds);
            string selectedBuilding = selectedBuildingId == 0 ? "-" : selectedBuildingId.ToString();
            string hoveredResource = hoveredResourceNodeId == 0 ? "-" : hoveredResourceNodeId.ToString();

            return "Tick " + frame.Tick
                + "  Food " + player.Food
                + "  Wood " + player.Wood
                + "  Gold " + player.Gold
                + "  Pop " + player.PopulationUsed + "/" + player.PopulationCap
                + "  Selected " + selected
                + GetSelectedUnitStatusText(frame, selectedUnitIds)
                + "  Building " + selectedBuilding
                + GetSelectedBuildingStatusText(frame, selectedBuildingId)
                + "  Resource " + hoveredResource
                + (paused ? "  Paused" : "");
        }

        public static string GetSelectedUnitStatusText(GodotFrameDto frame, int[] selectedUnitIds)
        {
            if (selectedUnitIds.Length == 0)
            {
                return "";
            }

            GodotUnitStatusDto? status = FindUnitStatus(frame, selectedUnitIds[0]);
            if (status == null)
            {
                return "";
            }

            if (status.CurrentResourceNodeId != 0)
            {
                return "  Gather " + status.CurrentResourceNodeId + " Carry " + status.CarriedAmount;
            }

            if (status.CurrentBuildTargetId != 0)
            {
                return "  BuildTarget " + status.CurrentBuildTargetId;
            }

            if (status.AttackTargetId != 0)
            {
                return "  Attack " + status.AttackTargetId + " CD " + status.AttackCooldownTicksRemaining;
            }

            if (status.HasMoveTarget)
            {
                return "  Moving";
            }

            if (status.CarriedAmount > 0)
            {
                return "  Carry " + status.CarriedAmount;
            }

            return "";
        }

        public static string GetSelectedBuildingStatusText(GodotFrameDto frame, int selectedBuildingId)
        {
            if (selectedBuildingId == 0)
            {
                return "";
            }

            GodotBuildingStatusDto? status = FindBuildingStatus(frame, selectedBuildingId);
            if (status == null)
            {
                return "";
            }

            if (status.IsUnderConstruction)
            {
                return "  Build " + status.BuildProgressTicks + "/" + status.RequiredBuildTicks;
            }

            if (status.TrainingQueueCount > 0)
            {
                return "  Train " + status.TrainingUnitTypeId + " " + status.TrainingProgressTicks + "/" + status.TrainingRequiredTicks;
            }

            return "";
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

        private static GodotBuildingStatusDto? FindBuildingStatus(GodotFrameDto frame, int buildingId)
        {
            for (int i = 0; i < frame.BuildingStatuses.Length; i++)
            {
                if (frame.BuildingStatuses[i].BuildingId == buildingId)
                {
                    return frame.BuildingStatuses[i];
                }
            }

            return null;
        }
    }
}
