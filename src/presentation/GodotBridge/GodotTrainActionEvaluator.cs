namespace RtsGame.Presentation.GodotBridge
{
    using RtsGame.Sim.Data;

    public static class GodotTrainActionEvaluator
    {
        public static GodotTrainActionState Evaluate(GodotFrameDto frame, int selectedBuildingId, int unitTypeId)
        {
            if (selectedBuildingId == 0)
            {
                return GodotTrainActionState.None;
            }

            GodotBuildingStatusDto? status = FindBuildingStatus(frame, selectedBuildingId);
            if (status == null)
            {
                return GodotTrainActionState.None;
            }

            if (status.IsUnderConstruction)
            {
                return GodotTrainActionState.BlockedConstruction;
            }

            UnitTypeId typedUnit = (UnitTypeId)unitTypeId;
            if (!GameData.CanTrain((BuildingTypeId)status.BuildingTypeId, typedUnit))
            {
                return GodotTrainActionState.NotApplicable;
            }

            GodotLocalPlayerDto player = frame.LocalPlayer;
            int populationCost = GameData.GetUnitPopulation(typedUnit);
            if (player.PopulationUsed + populationCost > player.PopulationCap)
            {
                return GodotTrainActionState.PopulationCapped;
            }

            ResourceStockpile cost = GameData.GetUnitCost(typedUnit);
            if (player.Food < cost.Food || player.Wood < cost.Wood || player.Gold < cost.Gold)
            {
                return GodotTrainActionState.MissingResources;
            }

            return GodotTrainActionState.Ready;
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
