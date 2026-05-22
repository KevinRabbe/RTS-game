using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsTownCenterPlacementGhostRenderer
{
	public static void Draw(CanvasItem canvas, Vector2 center, float tilePixels, TcPlacementPreviewResult previewResult)
	{
		float size = 4.0f * tilePixels;
		Rect2 rect = new Rect2(center.X - size * 0.5f, center.Y - size * 0.5f, size, size);
		Color ghostColor = previewResult == TcPlacementPreviewResult.Valid
			? new Color(0.2f, 1.0f, 0.2f, 0.5f)
			: new Color(1.0f, 0.2f, 0.2f, 0.5f);

		canvas.DrawRect(rect, new Color(ghostColor, 0.2f));
		canvas.DrawRect(rect, ghostColor, false, 2.0f);
		float radiusPixels = 2.0f * tilePixels;
		canvas.DrawArc(center, radiusPixels, 0.0f, Mathf.Tau, 32, ghostColor, 1.0f);
		canvas.DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(0.0f, -4.0f), "[TC] " + previewResult, HorizontalAlignment.Left, -1.0f, 12, ghostColor);
	}
}
