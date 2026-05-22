using Godot;

internal static class RtsBuildingFootprintOutlineRenderer
{
	public static void Draw(CanvasItem canvas, Rect2 rect, Color color)
	{
		Rect2 grown = rect.Grow(2.0f);
		canvas.DrawRect(grown, Colors.Black, false, 3.0f);
		canvas.DrawRect(grown, color, false, 2.0f);
	}
}
