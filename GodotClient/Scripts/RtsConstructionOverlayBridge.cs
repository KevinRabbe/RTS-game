using System;
using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsConstructionOverlayBridge
{
	internal static void DrawIfNeeded(
		Node2D canvas,
		GodotFrameDto? frame,
		GodotPrimitiveDto primitive,
		Func<long, long, Vector2> toScreen)
	{
		if (frame == null)
		{
			return;
		}

		GodotBuildingStatusDto? status = RtsFrameLookup.FindBuildingStatus(frame, primitive.EntityId);
		if (status == null || !status.IsUnderConstruction)
		{
			return;
		}

		RtsConstructionOverlayRenderer.Draw(canvas, toScreen(primitive.XRaw, primitive.YRaw), status);
	}
}
