namespace RtsGame.Presentation.GodotBridge
{
    using RtsGame.Sim.Data;

    public static class GodotHudTextBuilder
    {
        public static string[] BuildLines(
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

            string lineA = "Tick " + frame.Tick
                + "  Food " + player.Food
                + "  Wood " + player.Wood
                + "  Gold " + player.Gold
                + "  Pop " + player.PopulationUsed + "/" + player.PopulationCap
                + GetResearchStatusText(player)
                + GetModifierStatusText(player)
                + "  Rej " + frame.Match.RejectedCommandCount;

            string lineB = "Selected " + selected
                + GetSelectedUnitStatusText(frame, selectedUnitIds)
                + "  Building " + selectedBuilding
                + GetSelectedBuildingStatusText(frame, selectedBuildingId)
                + GetSelectedBuildingTrainActionText(frame, selectedBuildingId)
                + GetSelectedBuildingResearchActionText(frame, selectedBuildingId)
                + "  Resource " + hoveredResource
                + (paused ? "  Paused" : "")
                + GetControlHintText();

            return new[] { lineA, lineB };
        }

        public static string Build(
            GodotFrameDto frame,
            int[] selectedUnitIds,
            int selectedBuildingId,
            int hoveredResourceNodeId,
            bool paused)
        {
            return string.Join("  ", BuildLines(frame, selectedUnitIds, selectedBuildingId, hoveredResourceNodeId, paused));
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

        public static string GetSelectedBuildingResearchActionText(GodotFrameDto frame, int selectedBuildingId)
        {
            if (selectedBuildingId == 0)
            {
                return "";
            }

            GodotResearchActionState state = GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, selectedBuildingId);
            switch (state)
            {
                case GodotResearchActionState.BlockedConstruction:
                    return "  Y:Research Blocked(Build)";
                case GodotResearchActionState.NotApplicable:
                    return "  Y:Research N/A";
                case GodotResearchActionState.Done:
                    return "  Y:Research Done";
                case GodotResearchActionState.Queued:
                    return "  Y:Research Queued";
                case GodotResearchActionState.MissingResources:
                    return "  Y:Research Cost " + GameData.InfantryAttack1FoodCost + "F/" + GameData.InfantryAttack1GoldCost + "G";
                case GodotResearchActionState.Ready:
                    return "  Y:Research Ready";
                default:
                    return "";
            }
        }

        public static string GetSelectedBuildingTrainActionText(GodotFrameDto frame, int selectedBuildingId)
        {
            if (selectedBuildingId == 0)
            {
                return "";
            }

            string villager = ToTrainActionLabel(GodotTrainActionEvaluator.Evaluate(frame, selectedBuildingId, (int)UnitTypeId.Villager));
            string infantry = ToTrainActionLabel(GodotTrainActionEvaluator.Evaluate(frame, selectedBuildingId, (int)UnitTypeId.Infantry));
            string tradeCart = ToTrainActionLabel(GodotTrainActionEvaluator.Evaluate(frame, selectedBuildingId, (int)UnitTypeId.TradeCart));

            return "  V:" + villager + " I:" + infantry + " K:" + tradeCart;
        }

        private static string GetResearchStatusText(GodotLocalPlayerDto player)
        {
            if (player.ResearchQueue.Length > 0)
            {
                GodotResearchStatusDto research = player.ResearchQueue[0];
                string text = "  Research " + GodotTechLabelResolver.ResolveTechLabel(research.TechId) + " " + research.ProgressTicks + "/" + research.RequiredTicks;
                if (player.ResearchQueue.Length > 1)
                {
                    text += " +" + (player.ResearchQueue.Length - 1);
                }

                return text;
            }

            if (player.CompletedTechIds.Length > 0)
            {
                int techId = player.CompletedTechIds[player.CompletedTechIds.Length - 1];
                string text = "  Tech " + GodotTechLabelResolver.ResolveTechLabel(techId);
                if (player.CompletedTechIds.Length > 1)
                {
                    text += " (" + player.CompletedTechIds.Length + ")";
                }

                return text;
            }

            return "";
        }

        private static string GetModifierStatusText(GodotLocalPlayerDto player)
        {
            if (player.Modifiers.Length == 0)
            {
                return "";
            }

            GodotModifierStatusDto modifier = player.Modifiers[0];
            string text = "  Mod " + GodotTechLabelResolver.ResolveModifierLabel(modifier.ModifierId) + "=" + modifier.Value;
            if (player.Modifiers.Length > 1)
            {
                text += " +" + (player.Modifiers.Length - 1);
            }

            return text;
        }

        private static string GetControlHintText()
        {
            return "  Press H for hotkeys";
        }

        private static string ToTrainActionLabel(GodotTrainActionState state)
        {
            switch (state)
            {
                case GodotTrainActionState.Ready:
                    return "Ready";
                case GodotTrainActionState.MissingResources:
                    return "Cost";
                case GodotTrainActionState.PopulationCapped:
                    return "Cap";
                case GodotTrainActionState.BlockedConstruction:
                    return "Build";
                case GodotTrainActionState.NotApplicable:
                    return "N/A";
                default:
                    return "-";
            }
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
