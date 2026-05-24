using RtsGame.Presentation.GodotBridge;

internal static class RtsContextCommandResolver
{
	internal static GodotInteractionIntent ResolveNormalRightClick(
		GodotFrameDto frame,
		int localPlayerIndex,
		bool hasSelectedUnits,
		long mouseXRaw,
		long mouseYRaw)
	{
		return GodotInteractionRouter.RouteRightClick(frame, localPlayerIndex, hasSelectedUnits, mouseXRaw, mouseYRaw);
	}
}
