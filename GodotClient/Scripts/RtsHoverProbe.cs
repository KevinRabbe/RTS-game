using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsHoverProbe
{
	public static int FindResourceAt(GodotFrameDto frame, long xRaw, long yRaw)
	{
		return GodotInteractionRouter.FindResourceAt(frame, xRaw, yRaw);
	}

	public static int FindLocalBuildingAt(GodotFrameDto frame, int localPlayerIndex, long xRaw, long yRaw)
	{
		for (int i = 0; i < frame.Primitives.Length; i++)
		{
			GodotPrimitiveDto primitive = frame.Primitives[i];
			if (primitive.OwnerPlayerIndex != localPlayerIndex)
			{
				continue;
			}

			if (GodotPrimitiveDrawKindResolver.Resolve(primitive) != GodotPrimitiveDrawKind.Building)
			{
				continue;
			}

			if (GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, xRaw, yRaw))
			{
				return primitive.EntityId;
			}
		}

		return 0;
	}

	public static int FindHoveredUnitAt(GodotFrameDto frame, long xRaw, long yRaw)
	{
		for (int i = 0; i < frame.Primitives.Length; i++)
		{
			GodotPrimitiveDto primitive = frame.Primitives[i];
			if (GodotPrimitiveDrawKindResolver.Resolve(primitive) != GodotPrimitiveDrawKind.Unit)
			{
				continue;
			}

			if (GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, xRaw, yRaw))
			{
				return primitive.EntityId;
			}
		}

		return 0;
	}
}
