using Godot;

internal static class RtsHudPanelRenderer
{
	public static void DrawSelectedStatusPanel(CanvasItem canvas, Vector2 uiOrigin, Vector2 uiSize, string[] statusLines)
	{
		float panelWidth = Mathf.Min(760.0f, uiSize.X - 24.0f);
		Vector2 panelPos = uiOrigin + new Vector2(0.0f, Mathf.Max(96.0f, uiSize.Y - 66.0f));
		canvas.DrawRect(new Rect2(panelPos, new Vector2(panelWidth, 52.0f)), new Color(0.0f, 0.0f, 0.0f, 0.48f));
		for (int i = 0; i < statusLines.Length; i++)
		{
			canvas.DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 19.0f + i * 16.0f), statusLines[i], HorizontalAlignment.Left, -1.0f, 13, Colors.LightGray);
		}
	}

	public static void DrawBuildingStatusPanel(CanvasItem canvas, Vector2 uiOrigin, string[] lines)
	{
		Vector2 panelPos = uiOrigin + new Vector2(0.0f, 76.0f);
		canvas.DrawRect(new Rect2(panelPos, new Vector2(860.0f, 34.0f)), new Color(0.0f, 0.0f, 0.0f, 0.46f));
		for (int i = 0; i < lines.Length && i < 2; i++)
		{
			canvas.DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(8.0f, 13.0f + i * 15.0f), lines[i], HorizontalAlignment.Left, -1.0f, 12, Colors.LightGray);
		}
	}
}
