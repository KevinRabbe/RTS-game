internal static class RtsContextHintFormatter
{
	internal static string BuildPlacementHint(bool isTownCenterPlacement)
	{
		return isTownCenterPlacement ? "LMB Place TC  RMB Cancel" : "";
	}

	internal static string BuildAttackMoveModeHint(bool isAttackMoveTargeting)
	{
		return isAttackMoveTargeting ? "LMB AttackMove/Attack  RMB Cancel->Context" : "";
	}

	internal static string BuildNormalCommandHint(
		bool hasSelectedUnits,
		RtsResolvedCommandKind resolvedKind,
		int targetEntityId,
		int resourceNodeId)
	{
		if (!hasSelectedUnits)
		{
			return "";
		}

		switch (resolvedKind)
		{
			case RtsResolvedCommandKind.Attack:
				return targetEntityId == 0
					? "RMB Attack"
					: "RMB Attack T" + targetEntityId;
			case RtsResolvedCommandKind.AssignBuild:
				return targetEntityId == 0
					? "RMB Build"
					: "RMB Build B" + targetEntityId;
			case RtsResolvedCommandKind.Gather:
				return resourceNodeId == 0
					? "RMB Gather"
					: "RMB Gather R" + resourceNodeId;
			case RtsResolvedCommandKind.Move:
				return "RMB Move";
			case RtsResolvedCommandKind.AttackMove:
				return "RMB AttackMove";
			default:
				return "";
		}
	}
}
