using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsPrimitiveRectBuilder
{
	public static Rect2 Build(GodotPrimitiveDto primitive, System.Func<long, float> rawToPixels, System.Func<long, long, Vector2> toScreen)
	{
		Vector2 center = toScreen(primitive.XRaw, primitive.YRaw);
		float width = rawToPixels(primitive.WidthRaw);
		float height = rawToPixels(primitive.HeightRaw);
		return new Rect2(center.X - width * 0.5f, center.Y - height * 0.5f, width, height);
	}
}
