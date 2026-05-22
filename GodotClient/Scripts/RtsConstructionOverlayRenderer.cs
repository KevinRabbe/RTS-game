using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsConstructionOverlayRenderer
{
	public static void Draw(CanvasItem canvas, Vector2 center, GodotBuildingStatusDto status)
	{
		var bgRect = new Rect2(center.X - 44.0f, center.Y - 30.0f, 88.0f, 20.0f);
		canvas.DrawRect(bgRect, new Color(0.0f, 0.0f, 0.0f, 0.5f));
		canvas.DrawString(ThemeDB.FallbackFont, bgRect.Position + new Vector2(4.0f, 9.0f), "BUILDING", HorizontalAlignment.Left, -1.0f, 11, Colors.Khaki);
		canvas.DrawString(ThemeDB.FallbackFont, bgRect.Position + new Vector2(4.0f, 19.0f), status.BuildProgressTicks + "/" + status.RequiredBuildTicks, HorizontalAlignment.Left, -1.0f, 10, Colors.LightGray);
	}
}
