using RtsGame.Presentation.GodotBridge;

internal static class RtsBuildingActionGuard
{
	public static bool CanTrainFromSelectedBuilding(GodotFrameDto frame, int buildingId, int unitTypeId, out GodotTrainActionState state)
	{
		if (buildingId == 0)
		{
			state = GodotTrainActionState.NotApplicable;
			return false;
		}

		state = GodotTrainActionEvaluator.Evaluate(frame, buildingId, unitTypeId);
		return state == GodotTrainActionState.Ready;
	}

	public static bool CanResearchInfantryAttackFromSelectedBuilding(GodotFrameDto frame, int buildingId, out GodotResearchActionState state)
	{
		if (buildingId == 0)
		{
			state = GodotResearchActionState.NotApplicable;
			return false;
		}

		state = GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, buildingId);
		return state == GodotResearchActionState.Ready;
	}
}
