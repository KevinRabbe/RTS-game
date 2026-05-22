using Godot;

internal static class RtsResourceHoverRenderer
{
	public static void DrawHoverRing(CanvasItem canvas, Rect2 rect)
	{
		canvas.DrawArc(rect.GetCenter(), rect.Size.X * 0.65f, 0.0f, Mathf.Tau, 32, Colors.White, 2.0f);
	}
}
