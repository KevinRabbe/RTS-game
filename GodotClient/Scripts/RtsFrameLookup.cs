using RtsGame.Presentation.GodotBridge;

internal static class RtsFrameLookup
{
	public static GodotPrimitiveDto? FindPrimitiveByEntityId(GodotFrameDto frame, int entityId)
	{
		for (int i = 0; i < frame.Primitives.Length; i++)
		{
			if (frame.Primitives[i].EntityId == entityId)
			{
				return frame.Primitives[i];
			}
		}

		return null;
	}

	public static GodotBuildingStatusDto? FindBuildingStatus(GodotFrameDto frame, int buildingId)
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
