using Godot;

internal static class RtsSelectionRingRenderer
{
	public static void Draw(CanvasItem canvas, Vector2 center, float radiusSource, Color color)
	{
		float radius = Mathf.Max(10.0f, radiusSource * 0.74f);
		Vector2 ringCenter = center + new Vector2(0.0f, 4.0f);
		canvas.DrawArc(ringCenter, radius + 1.5f, 0.0f, Mathf.Tau, 36, Colors.Black, 3.0f);
		canvas.DrawArc(ringCenter, radius, 0.0f, Mathf.Tau, 36, color, 2.4f);
	}
}
