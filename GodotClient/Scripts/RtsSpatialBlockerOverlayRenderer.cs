using System;
using Godot;
using RtsGame.Presentation.GodotBridge;

public static class RtsSpatialBlockerOverlayRenderer
{
	public static void Draw(
		Node2D canvas,
		GodotFrameDto frame,
		int mapWidthTiles,
		int mapHeightTiles,
		float tilePixels,
		Func<int, long> tileToRaw)
	{
		Color blockedColor = new Color(0.95f, 0.2f, 0.2f, 0.18f);
		for (int y = 0; y < mapHeightTiles; y++)
		{
			for (int x = 0; x < mapWidthTiles; x++)
			{
				if (!IsTileBlockedByVisibleSimEntity(frame, x, y, tileToRaw))
				{
					continue;
				}

				canvas.DrawRect(new Rect2(x * tilePixels, y * tilePixels, tilePixels, tilePixels), blockedColor);
			}
		}
	}

	private static bool IsTileBlockedByVisibleSimEntity(
		GodotFrameDto frame,
		int tileX,
		int tileY,
		Func<int, long> tileToRaw)
	{
		long tileXRaw = tileToRaw(tileX);
		long tileYRaw = tileToRaw(tileY);
		for (int i = 0; i < frame.Primitives.Length; i++)
		{
			GodotPrimitiveDto primitive = frame.Primitives[i];
			GodotPrimitiveDrawKind kind = GodotPrimitiveDrawKindResolver.Resolve(primitive);
			if (kind == GodotPrimitiveDrawKind.Building || kind == GodotPrimitiveDrawKind.Resource)
			{
				if (IsWithinPrimitiveBounds(tileXRaw, tileYRaw, primitive))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool IsWithinPrimitiveBounds(long tileXRaw, long tileYRaw, GodotPrimitiveDto primitive)
	{
		long halfWidthRaw = primitive.WidthRaw / 2;
		long halfHeightRaw = primitive.HeightRaw / 2;
		return tileXRaw >= primitive.XRaw - halfWidthRaw
			&& tileXRaw <= primitive.XRaw + halfWidthRaw
			&& tileYRaw >= primitive.YRaw - halfHeightRaw
			&& tileYRaw <= primitive.YRaw + halfHeightRaw;
	}
}
