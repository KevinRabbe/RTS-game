using RtsGame.Presentation.GodotBridge;

internal static class RtsDepositEventTracker
{
	public static bool TryBuildDepositEvent(GodotFrameDto? previous, GodotFrameDto current, out string message)
	{
		message = string.Empty;
		if (previous == null)
		{
			return false;
		}

		int foodDelta = current.LocalPlayer.Food - previous.LocalPlayer.Food;
		int woodDelta = current.LocalPlayer.Wood - previous.LocalPlayer.Wood;
		int goldDelta = current.LocalPlayer.Gold - previous.LocalPlayer.Gold;
		if (foodDelta <= 0 && woodDelta <= 0 && goldDelta <= 0)
		{
			return false;
		}

		for (int i = 0; i < previous.UnitStatuses.Length; i++)
		{
			GodotUnitStatusDto before = previous.UnitStatuses[i];
			if (before.CarriedAmount <= 0)
			{
				continue;
			}

			GodotUnitStatusDto? after = FindUnitStatus(current, before.UnitId);
			if (after == null || after.CarriedAmount > 0)
			{
				continue;
			}

			string resource = before.CarriedResourceTypeId == 1 ? "food" : before.CarriedResourceTypeId == 2 ? "wood" : before.CarriedResourceTypeId == 3 ? "gold" : "resource";
			message = "unit " + before.UnitId + " deposited " + before.CarriedAmount + " " + resource;
			return true;
		}

		return false;
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
}
