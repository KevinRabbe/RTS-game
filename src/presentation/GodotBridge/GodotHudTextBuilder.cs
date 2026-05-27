namespace RtsGame.Presentation.GodotBridge
{
    using RtsGame.Sim.Commands;
    using RtsGame.Sim.Data;
    using RtsGame.Sim.Determinism;

    public static class GodotHudTextBuilder
    {
        public static string[] BuildLines(
            GodotFrameDto frame,
            int[] selectedUnitIds,
            int selectedBuildingId,
            int hoveredResourceNodeId,
            bool paused,
            int selectedControlGroupIndex = 0,
            int[]? selectedControlGroupIndices = null,
            string inputModeLabel = "",
            string contextHintLabel = "",
            int hoveredBuildingId = 0,
            int hoveredUnitId = 0)
        {
            GodotLocalPlayerDto player = frame.LocalPlayer;
            string selected = selectedUnitIds.Length == 0 ? "none" : string.Join(",", selectedUnitIds);
            string selectedBuilding = selectedBuildingId == 0 ? "none" : selectedBuildingId.ToString();
            string hoveredResource = hoveredResourceNodeId == 0 ? "none" : hoveredResourceNodeId.ToString();
            string selectedCount = selectedUnitIds.Length.ToString();

            string lineA = "Tick " + frame.Tick
                + "  Map " + frame.MapName
                + "  Food " + player.Food
                + "  Wood " + player.Wood
                + "  Gold " + player.Gold
                + "  Pop " + player.PopulationUsed + "/" + player.PopulationCap
                + GetResearchStatusText(player)
                + GetModifierStatusText(player)
                + "  Rej " + frame.Match.RejectedCommandCount
                + GetLastCommandStatusText(frame.Match);

            string lineB = "Selected " + selected
                + " (" + selectedCount + ")"
                + GetSelectedControlGroupIndicatorText(selectedControlGroupIndex, selectedControlGroupIndices)
                + GetSelectedGroupSummaryText(selectedUnitIds)
                + GetSelectedTypeSummaryText(frame, selectedUnitIds)
                + GetSelectedUnitStatusText(frame, selectedUnitIds)
                + GetSelectedUnitActionHintText(frame, selectedUnitIds)
                + "  Building " + selectedBuilding
                + GetSelectedBuildingStatusText(frame, selectedBuildingId)
                + GetSelectedBuildingTrainActionText(frame, selectedBuildingId)
                + GetSelectedBuildingResearchActionText(frame, selectedBuildingId)
                + GetSelectedBuildingActionHintText(frame, selectedBuildingId)
                + "  Resource " + hoveredResource
                + GetHoveredTargetText(hoveredBuildingId, hoveredResourceNodeId, hoveredUnitId)
                + GetInputModeText(inputModeLabel)
                + GetContextHintText(contextHintLabel)
                + (paused ? "  Paused" : "")
                + GetControlHintText();

            return new[] { lineA, lineB };
        }

        public static string Build(
            GodotFrameDto frame,
            int[] selectedUnitIds,
            int selectedBuildingId,
            int hoveredResourceNodeId,
            bool paused,
            int selectedControlGroupIndex = 0,
            int[]? selectedControlGroupIndices = null,
            string inputModeLabel = "",
            string contextHintLabel = "",
            int hoveredBuildingId = 0,
            int hoveredUnitId = 0)
        {
            return string.Join("  ", BuildLines(frame, selectedUnitIds, selectedBuildingId, hoveredResourceNodeId, paused, selectedControlGroupIndex, selectedControlGroupIndices, inputModeLabel, contextHintLabel, hoveredBuildingId, hoveredUnitId));
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

            string prefix = "  U" + status.UnitId
                + ":" + ResolveUnitTypeLabel(status.UnitTypeId)
                + " HP " + status.CurrentHitPoints + "/" + status.MaxHitPoints
                + " Phase " + ResolveWorkerTaskPhaseLabel(status.TaskPhaseId);

            if (status.CurrentResourceNodeId != 0)
            {
                return prefix + " Gather " + status.CurrentResourceNodeId + " Carry " + status.CarriedAmount;
            }

            if (status.CurrentBuildTargetId != 0)
            {
                return prefix + " BuildTarget " + status.CurrentBuildTargetId;
            }

            if (status.AttackTargetId != 0)
            {
                string combat = prefix + " Attack " + status.AttackTargetId + " CD " + status.AttackCooldownTicksRemaining;
                if (!status.HasAttackMoveTarget)
                {
                    return combat;
                }

                int amTileX = new Fixed(status.AttackMoveTargetXRaw).FloorToInt();
                int amTileY = new Fixed(status.AttackMoveTargetYRaw).FloorToInt();
                return combat + " AM(" + amTileX + "," + amTileY + ")";
            }

            if (status.HasAttackMoveTarget)
            {
                int targetTileX = new Fixed(status.AttackMoveTargetXRaw).FloorToInt();
                int targetTileY = new Fixed(status.AttackMoveTargetYRaw).FloorToInt();
                string acquire = status.NextAttackMoveAcquireTick <= frame.Tick
                    ? "Acquire now"
                    : "Acquire @" + status.NextAttackMoveAcquireTick;
                return prefix + " AttackMove (" + targetTileX + "," + targetTileY + ") " + acquire;
            }

            if (status.AttackCooldownTicksRemaining > 0)
            {
                return prefix + " CD " + status.AttackCooldownTicksRemaining;
            }

            if (status.HasMoveTarget)
            {
                return prefix + " Moving";
            }

            if (status.CarriedAmount > 0)
            {
                return prefix + " Carry " + status.CarriedAmount;
            }

            return prefix;
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
                return "  " + ResolveBuildingTypeLabel(status.BuildingTypeId)
                    + " HP " + status.CurrentHitPoints + "/" + status.MaxHitPoints
                    + " Build " + status.BuildProgressTicks + "/" + status.RequiredBuildTicks;
            }

            if (status.TrainingQueueCount > 0)
            {
                return "  " + ResolveBuildingTypeLabel(status.BuildingTypeId)
                    + " HP " + status.CurrentHitPoints + "/" + status.MaxHitPoints
                    + " Train " + ResolveUnitTypeLabel(status.TrainingUnitTypeId)
                    + " " + status.TrainingProgressTicks + "/" + status.TrainingRequiredTicks;
            }

            return "  " + ResolveBuildingTypeLabel(status.BuildingTypeId)
                + " HP " + status.CurrentHitPoints + "/" + status.MaxHitPoints
                + " Ready";
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

        private static string GetSelectedControlGroupIndicatorText(int selectedControlGroupIndex, int[]? selectedControlGroupIndices)
        {
            if (selectedControlGroupIndices != null && selectedControlGroupIndices.Length > 0)
            {
                return " CG:" + string.Join(",", selectedControlGroupIndices);
            }

            if (selectedControlGroupIndex < 1 || selectedControlGroupIndex > 9)
            {
                return "";
            }

            return " CG:" + selectedControlGroupIndex;
        }

        private static string GetInputModeText(string inputModeLabel)
        {
            if (string.IsNullOrWhiteSpace(inputModeLabel))
            {
                return "";
            }

            return "  Mode " + inputModeLabel;
        }

        private static string GetContextHintText(string contextHintLabel)
        {
            if (string.IsNullOrWhiteSpace(contextHintLabel))
            {
                return "";
            }

            return "  " + contextHintLabel;
        }

        private static string GetHoveredTargetText(int hoveredBuildingId, int hoveredResourceNodeId, int hoveredUnitId)
        {
            if (hoveredBuildingId != 0)
            {
                return " HoverB " + hoveredBuildingId;
            }

            if (hoveredUnitId != 0)
            {
                return " HoverU " + hoveredUnitId;
            }

            if (hoveredResourceNodeId != 0)
            {
                return " HoverR " + hoveredResourceNodeId;
            }

            return "";
        }

        private static string GetSelectedGroupSummaryText(int[] selectedUnitIds)
        {
            if (selectedUnitIds.Length <= 1)
            {
                return "";
            }

            return " Group x" + selectedUnitIds.Length;
        }

        private static string GetSelectedTypeSummaryText(GodotFrameDto frame, int[] selectedUnitIds)
        {
            if (selectedUnitIds.Length <= 1)
            {
                return "";
            }

            int infantry = 0;
            int villager = 0;
            int scout = 0;
            int cavalry = 0;
            int siegeCannon = 0;
            int tradeCart = 0;
            int mangonel = 0;
            int unknown = 0;
            int counted = 0;

            for (int i = 0; i < selectedUnitIds.Length; i++)
            {
                GodotUnitStatusDto? status = FindUnitStatus(frame, selectedUnitIds[i]);
                if (status == null)
                {
                    continue;
                }

                counted++;
                switch ((UnitTypeId)status.UnitTypeId)
                {
                    case UnitTypeId.Infantry:
                        infantry++;
                        break;
                    case UnitTypeId.Villager:
                        villager++;
                        break;
                    case UnitTypeId.Scout:
                        scout++;
                        break;
                    case UnitTypeId.Cavalry:
                        cavalry++;
                        break;
                    case UnitTypeId.SiegeCannon:
                        siegeCannon++;
                        break;
                    case UnitTypeId.TradeCart:
                        tradeCart++;
                        break;
                    case UnitTypeId.Mangonel:
                        mangonel++;
                        break;
                    default:
                        unknown++;
                        break;
                }
            }

            if (counted == 0)
            {
                return "";
            }

            string parts = "";
            AppendTypeCount(ref parts, infantry, "Infantry");
            AppendTypeCount(ref parts, villager, "Villager");
            AppendTypeCount(ref parts, scout, "Scout");
            AppendTypeCount(ref parts, cavalry, "Cavalry");
            AppendTypeCount(ref parts, siegeCannon, "SiegeCannon");
            AppendTypeCount(ref parts, tradeCart, "TradeCart");
            AppendTypeCount(ref parts, mangonel, "Mangonel");
            AppendTypeCount(ref parts, unknown, "Other");

            return parts.Length == 0 ? "" : "  " + parts;
        }

        private static void AppendTypeCount(ref string parts, int count, string label)
        {
            if (count <= 0)
            {
                return;
            }

            if (parts.Length > 0)
            {
                parts += ", ";
            }

            parts += count + " " + label;
        }

        private static string GetSelectedUnitActionHintText(GodotFrameDto frame, int[] selectedUnitIds)
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

            if (status.UnitTypeId == (int)UnitTypeId.Villager)
            {
                return "  Hint: RMB ground=Move resource=Gather foundation=Build";
            }

            return "  Hint: RMB ground=Move target=Attack";
        }

        private static string GetSelectedBuildingActionHintText(GodotFrameDto frame, int selectedBuildingId)
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
                return "  Hint: Villagers can assist construction";
            }

            if (status.BuildingTypeId == (int)BuildingTypeId.TownCenter)
            {
                return "  Hint: V=Villager I=Infantry Y=Research";
            }

            if (status.BuildingTypeId == (int)BuildingTypeId.TradePost)
            {
                return "  Hint: K=TradeCart";
            }

            return "";
        }

        private static string GetLastCommandStatusText(GodotMatchDto match)
        {
            if (match.LastCommandTypeId == 0 && match.LastCommandReasonId == 0 && !match.LastCommandAccepted)
            {
                return "";
            }

            string command = ResolveCommandTypeLabel(match.LastCommandTypeId);
            string result = match.LastCommandAccepted ? "ok" : "rej";
            string readable = match.LastCommandAccepted ? "Accepted" : "Rejected";
            string text = "  Cmd " + command + " " + result
                + " r" + match.LastCommandReasonId
                + "(" + ResolveCommandReasonLabel(match.LastCommandReasonId) + ")"
                + " " + readable;
            if (!match.LastCommandAccepted
                && match.LastCommandTypeId == (int)CommandType.Attack
                && match.LastCommandReasonId == (int)CommandValidationReason.UnitCannotPerformAction)
            {
                text += " [NonCombatCannotAttack]";
            }

            if (!match.LastCommandAccepted
                && match.LastCommandTypeId == (int)CommandType.AttackMove
                && match.LastCommandReasonId == (int)CommandValidationReason.UnitCannotPerformAction)
            {
                text += " [NonCombatCannotAttackMove]";
            }

            return text;
        }

        private static string ResolveCommandTypeLabel(int commandTypeId)
        {
            switch (commandTypeId)
            {
                case (int)CommandType.NoOp:
                    return "NoOp";
                case (int)CommandType.MoveUnits:
                    return "MoveUnits";
                case (int)CommandType.PlaceTownCenter:
                    return "PlaceTownCenter";
                case (int)CommandType.PlaceWall:
                    return "PlaceWall";
                case (int)CommandType.AssignBuild:
                    return "AssignBuild";
                case (int)CommandType.GatherResource:
                    return "GatherResource";
                case (int)CommandType.TrainUnit:
                    return "TrainUnit";
                case (int)CommandType.Attack:
                    return "Attack";
                case (int)CommandType.AttackMove:
                    return "AttackMove";
                case (int)CommandType.Resign:
                    return "Resign";
                case (int)CommandType.PlaceTradePost:
                    return "PlaceTradePost";
                case (int)CommandType.CreateTradeRoute:
                    return "CreateTradeRoute";
                case (int)CommandType.ResearchTech:
                    return "ResearchTech";
            }

            return "#" + commandTypeId;
        }

        private static string ResolveCommandReasonLabel(int reasonId)
        {
            switch (reasonId)
            {
                case (int)CommandValidationReason.Accepted:
                    return "Accepted";
                case (int)CommandValidationReason.TemporaryCongestionAcceptedIntent:
                    return "TemporaryCongestionAcceptedIntent";
                case (int)CommandValidationReason.InvalidHeader:
                    return "InvalidHeader";
                case (int)CommandValidationReason.TargetMissing:
                    return "TargetMissing";
                case (int)CommandValidationReason.WrongOwner:
                    return "WrongOwner";
                case (int)CommandValidationReason.TargetComplete:
                    return "TargetComplete";
                case (int)CommandValidationReason.InvalidTargetType:
                    return "InvalidTargetType";
                case (int)CommandValidationReason.UnitCannotPerformAction:
                    return "UnitCannotPerformAction";
                case (int)CommandValidationReason.NoStaticPath:
                    return "NoStaticPath";
                case (int)CommandValidationReason.TargetBlockedByStaticGeometry:
                    return "TargetBlockedByStaticGeometry";
                case (int)CommandValidationReason.MissingResources:
                    return "MissingResources";
                case (int)CommandValidationReason.PopulationBlocked:
                    return "PopulationBlocked";
                case (int)CommandValidationReason.DuplicateUnitSelection:
                    return "DuplicateUnitSelection";
                case (int)CommandValidationReason.PlayerStateBlocked:
                    return "PlayerStateBlocked";
                case (int)CommandValidationReason.Unknown:
                    return "Unknown";
            }

            return "#" + reasonId;
        }

        private static string ResolveUnitTypeLabel(int unitTypeId)
        {
            switch (unitTypeId)
            {
                case (int)UnitTypeId.Villager:
                    return "Villager";
                case (int)UnitTypeId.Scout:
                    return "Scout";
                case (int)UnitTypeId.Infantry:
                    return "Infantry";
                case (int)UnitTypeId.Cavalry:
                    return "Cavalry";
                case (int)UnitTypeId.SiegeCannon:
                    return "SiegeCannon";
                case (int)UnitTypeId.TradeCart:
                    return "TradeCart";
                case (int)UnitTypeId.Mangonel:
                    return "Mangonel";
            }

            return "U#" + unitTypeId;
        }

        private static string ResolveBuildingTypeLabel(int buildingTypeId)
        {
            switch (buildingTypeId)
            {
                case (int)BuildingTypeId.TownCenter:
                    return "TownCenter";
                case (int)BuildingTypeId.Wall:
                    return "Wall";
                case (int)BuildingTypeId.TradePost:
                    return "TradePost";
            }

            return "B#" + buildingTypeId;
        }

        private static string ResolveWorkerTaskPhaseLabel(int taskPhaseId)
        {
            switch (taskPhaseId)
            {
                case (int)WorkerTaskPhase.Idle:
                    return "Idle";
                case (int)WorkerTaskPhase.MovingToResourceSlot:
                    return "MovingToResourceSlot";
                case (int)WorkerTaskPhase.Gathering:
                    return "Gathering";
                case (int)WorkerTaskPhase.MovingToDropoffSlot:
                    return "MovingToDropoffSlot";
                case (int)WorkerTaskPhase.Depositing:
                    return "Depositing";
                case (int)WorkerTaskPhase.MovingToBuildSlot:
                    return "MovingToBuildSlot";
                case (int)WorkerTaskPhase.Building:
                    return "Building";
                case (int)WorkerTaskPhase.BlockedWaiting:
                    return "BlockedWaiting";
            }

            return "Phase#" + taskPhaseId;
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
