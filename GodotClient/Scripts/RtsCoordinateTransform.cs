using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsCoordinateTransform
{
	public static Vector2 ToScreen(long xRaw, long yRaw, float tilePixels)
	{
		return new Vector2(RawToPixels(xRaw, tilePixels), RawToPixels(yRaw, tilePixels));
	}

	public static float RawToPixels(long raw, float tilePixels)
	{
		return GodotCoordinateMapper.RawToPixels(raw, tilePixels);
	}

	public static long ScreenToRaw(float screenCoordinate, float tilePixels)
	{
		return GodotCoordinateMapper.ScreenToRaw(screenCoordinate, tilePixels);
	}

	public static long TileToRaw(int tileCoordinate, float tilePixels)
	{
		return ScreenToRaw(tileCoordinate * tilePixels, tilePixels);
	}

	public static Vector2I ScreenToTile(Vector2 screenPosition, float tilePixels)
	{
		int x = GodotCoordinateMapper.ScreenToTile(screenPosition.X, tilePixels);
		int y = GodotCoordinateMapper.ScreenToTile(screenPosition.Y, tilePixels);
		return new Vector2I(x, y);
	}
}
