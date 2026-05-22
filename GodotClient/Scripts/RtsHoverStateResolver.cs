using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsHoverStateResolver
{
	internal static int FindResourceAt(GodotFrameDto? frame, Vector2 screenPosition, int tilePixels)
	{
		if (frame == null)
		{
			return 0;
		}

		long xRaw = RtsCoordinateTransform.ScreenToRaw(screenPosition.X, tilePixels);
		long yRaw = RtsCoordinateTransform.ScreenToRaw(screenPosition.Y, tilePixels);
		return RtsHoverProbe.FindResourceAt(frame, xRaw, yRaw);
	}

	internal static int FindHoveredBuildingAt(GodotFrameDto? frame, int localPlayerIndex, Vector2 screenPosition, int tilePixels)
	{
		if (frame == null)
		{
			return 0;
		}

		long xRaw = RtsCoordinateTransform.ScreenToRaw(screenPosition.X, tilePixels);
		long yRaw = RtsCoordinateTransform.ScreenToRaw(screenPosition.Y, tilePixels);
		return RtsHoverProbe.FindLocalBuildingAt(frame, localPlayerIndex, xRaw, yRaw);
	}
}
