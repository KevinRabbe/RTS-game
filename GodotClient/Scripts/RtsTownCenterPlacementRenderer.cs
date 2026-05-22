using System;
using Godot;

internal static class RtsTownCenterPlacementRenderer
{
	internal static void DrawGhost(
		Node2D canvas,
		RtsTownCenterPlacementState placementState,
		float tilePixels,
		Func<int, long> tileToRaw,
		Func<long, long, Vector2> toScreen)
	{
		Vector2 center = toScreen(tileToRaw(placementState.HoveredTile.X), tileToRaw(placementState.HoveredTile.Y));
		RtsTownCenterPlacementGhostRenderer.Draw(canvas, center, tilePixels, placementState.PreviewResult);
	}
}
