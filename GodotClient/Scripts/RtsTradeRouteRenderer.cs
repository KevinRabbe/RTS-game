using Godot;

internal static class RtsTradeRouteRenderer
{
	public static void Draw(CanvasItem canvas, Vector2 start, Vector2 end)
	{
		canvas.DrawLine(start, end, Colors.Gold, 2.0f);
	}
}
