using RtsGame.Sim.Data;
using RtsGame.Presentation.Visuals;

namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotBuildingDebugStatusBuilder
    {
        public static string[] BuildLines(GodotFrameDto? frame, int selectedBuildingId)
        {
            if (frame == null || selectedBuildingId == 0)
            {
                return new[] { "Building Status: none", "Training: none" };
            }

            GodotBuildingStatusDto? status = FindBuildingStatus(frame, selectedBuildingId);
            GodotPrimitiveDto? primitive = FindBuildingPrimitive(frame, selectedBuildingId);
            if (status == null || primitive == null)
            {
                return new[] { "Building Status: none", "Training: none" };
            }

            string typeLabel = ResolveBuildingTypeLabel(status.BuildingTypeId);
            string completionLabel = status.IsUnderConstruction ? "BUILDING" : "Complete";
            string capitalLabel = primitive.IsCapital ? " Capital" : "";
            bool canTrain = CanTrain(status.BuildingTypeId);
            string line1 = "Building " + status.BuildingId
                + " " + typeLabel
                + " Owner " + primitive.OwnerPlayerIndex
                + " " + completionLabel
                + " " + status.BuildProgressTicks + "/" + status.RequiredBuildTicks
                + capitalLabel
                + " CanTrain " + canTrain;

            string line2;
            if (status.IsUnderConstruction)
            {
                line2 = "Training: unavailable until complete";
            }
            else if (!canTrain)
            {
                line2 = "Training: not supported";
            }
            else if (status.TrainingQueueCount > 0 && status.TrainingRequiredTicks > 0)
            {
                line2 = "Training: " + ResolveUnitTypeLabel(status.TrainingUnitTypeId)
                    + " " + status.TrainingProgressTicks + "/" + status.TrainingRequiredTicks
                    + " Queue " + status.TrainingQueueCount;
            }
            else
            {
                line2 = "Training: idle Queue " + status.TrainingQueueCount;
            }

            return new[] { line1, line2 };
        }

        public static string ResolveTrainBlockedReason(GodotTrainActionState state)
        {
            switch (state)
            {
                case GodotTrainActionState.Ready:
                    return "ready";
                case GodotTrainActionState.BlockedConstruction:
                    return "building under construction";
                case GodotTrainActionState.NotApplicable:
                    return "cannot train this unit type";
                case GodotTrainActionState.MissingResources:
                    return "not enough resources";
                case GodotTrainActionState.PopulationCapped:
                    return "population blocked";
                default:
                    return "not available";
            }
        }

        public static string BuildTrainIntentText(int buildingId, int unitTypeId)
        {
            return "train building=" + buildingId + " unit=" + ResolveUnitTypeLabel(unitTypeId);
        }

        public static string ResolveUnitTypeLabel(int unitTypeId)
        {
            switch ((UnitTypeId)unitTypeId)
            {
                case UnitTypeId.Villager:
                    return "Villager";
                case UnitTypeId.Infantry:
                    return "Infantry";
                case UnitTypeId.TradeCart:
                    return "TradeCart";
                case UnitTypeId.Scout:
                    return "Scout";
                case UnitTypeId.Cavalry:
                    return "Cavalry";
                default:
                    return "UnitType" + unitTypeId;
            }
        }

        private static string ResolveBuildingTypeLabel(int buildingTypeId)
        {
            switch ((BuildingTypeId)buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    return "TownCenter";
                case BuildingTypeId.Wall:
                    return "Wall";
                case BuildingTypeId.TradePost:
                    return "TradePost";
                default:
                    return "BuildingType" + buildingTypeId;
            }
        }

        private static bool CanTrain(int buildingTypeId)
        {
            return buildingTypeId == (int)BuildingTypeId.TownCenter
                || buildingTypeId == (int)BuildingTypeId.TradePost;
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

        private static GodotPrimitiveDto? FindBuildingPrimitive(GodotFrameDto frame, int buildingId)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.EntityId == buildingId
                    && (primitive.Kind == (int)VisualPrimitiveKind.BuildingRectangle
                        || primitive.Kind == (int)VisualPrimitiveKind.WallRectangle))
                {
                    return primitive;
                }
            }

            return null;
        }
    }
}
