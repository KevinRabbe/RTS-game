using RtsGame.Presentation.GodotBridge;

internal static class RtsContextCommandResolver
{
	// Centralized normal-mode RMB routing contract:
	// enemy => attack, resource => gather, build target => assign build, ground => move.
	// Keeping this in one resolver prevents drift between input modes.
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
