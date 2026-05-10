namespace RtsGame.Presentation.GodotBridge
{
    using RtsGame.Sim.Data;

    public static class GodotResearchActionEvaluator
    {
        public static GodotResearchActionState EvaluateInfantryAttack1(GodotFrameDto frame, int selectedBuildingId)
        {
            if (selectedBuildingId == 0)
            {
                return GodotResearchActionState.None;
            }

            GodotBuildingStatusDto? status = FindBuildingStatus(frame, selectedBuildingId);
            if (status == null)
            {
                return GodotResearchActionState.None;
            }

            if (status.IsUnderConstruction)
            {
                return GodotResearchActionState.BlockedConstruction;
            }

            if (status.BuildingTypeId != (int)BuildingTypeId.TownCenter)
            {
                return GodotResearchActionState.NotApplicable;
            }

            GodotLocalPlayerDto player = frame.LocalPlayer;
            if (ContainsTech(player.CompletedTechIds, (int)TechId.InfantryAttack1))
            {
                return GodotResearchActionState.Done;
            }

            if (ContainsQueuedResearch(player.ResearchQueue, (int)TechId.InfantryAttack1))
            {
                return GodotResearchActionState.Queued;
            }

            if (player.Food < GameData.InfantryAttack1FoodCost || player.Gold < GameData.InfantryAttack1GoldCost)
            {
                return GodotResearchActionState.MissingResources;
            }

            return GodotResearchActionState.Ready;
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

        private static bool ContainsTech(int[] completedTechIds, int techId)
        {
            for (int i = 0; i < completedTechIds.Length; i++)
            {
                if (completedTechIds[i] == techId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsQueuedResearch(GodotResearchStatusDto[] queue, int techId)
        {
            for (int i = 0; i < queue.Length; i++)
            {
                if (queue[i].TechId == techId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
