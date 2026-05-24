using RtsGame.Presentation.GodotBridge;

internal enum RtsResolvedCommandKind
{
	None = 0,
	Move = 1,
	Gather = 2,
	Attack = 3,
	AssignBuild = 4,
	AttackMove = 5
}

internal readonly struct RtsResolvedCommand
{
	internal RtsResolvedCommand(RtsResolvedCommandKind kind, int targetEntityId, int resourceNodeId)
	{
		Kind = kind;
		TargetEntityId = targetEntityId;
		ResourceNodeId = resourceNodeId;
	}

	internal RtsResolvedCommandKind Kind { get; }
	internal int TargetEntityId { get; }
	internal int ResourceNodeId { get; }
}

internal static class RtsCommandModeResolver
{
	internal static RtsResolvedCommand ResolveModeClick(
		RtsInputModeKind mode,
		GodotFrameDto frame,
		int localPlayerIndex,
		bool hasSelectedUnits,
		long mouseXRaw,
		long mouseYRaw)
	{
		if (!hasSelectedUnits)
		{
			return new RtsResolvedCommand(RtsResolvedCommandKind.None, 0, 0);
		}

		if (mode == RtsInputModeKind.AttackMoveTargeting)
		{
			// In attack-move mode: enemy click intentionally maps to explicit attack;
			// ground click maps to attack-move destination command.
			int targetId = GodotInteractionRouter.FindEnemyTargetAt(frame, localPlayerIndex, mouseXRaw, mouseYRaw);
			if (targetId != 0)
			{
				return new RtsResolvedCommand(RtsResolvedCommandKind.Attack, targetId, 0);
			}

			return new RtsResolvedCommand(RtsResolvedCommandKind.AttackMove, 0, 0);
		}

		GodotInteractionIntent intent = RtsContextCommandResolver.ResolveNormalRightClick(
			frame,
			localPlayerIndex,
			hasSelectedUnits,
			mouseXRaw,
			mouseYRaw);
		switch (intent.Kind)
		{
			case GodotInteractionIntentKind.Attack:
				return new RtsResolvedCommand(RtsResolvedCommandKind.Attack, intent.TargetEntityId, 0);
			case GodotInteractionIntentKind.AssignBuild:
				return new RtsResolvedCommand(RtsResolvedCommandKind.AssignBuild, intent.TargetEntityId, 0);
			case GodotInteractionIntentKind.GatherResource:
				return new RtsResolvedCommand(RtsResolvedCommandKind.Gather, 0, intent.ResourceNodeId);
			case GodotInteractionIntentKind.Move:
				return new RtsResolvedCommand(RtsResolvedCommandKind.Move, 0, 0);
			default:
				return new RtsResolvedCommand(RtsResolvedCommandKind.None, 0, 0);
		}
	}
}
