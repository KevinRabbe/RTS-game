using Godot;

public static class RtsDebugOverlayRenderer
{
	public static void Draw(
		Node2D canvas,
		Vector2 uiOrigin,
		Vector2 uiSize,
		Camera2D? camera,
		string[] events)
	{
		float panelWidth = 420.0f;
		float panelHeight = 168.0f;
		Vector2 panelPos = uiOrigin + new Vector2(Mathf.Max(0.0f, uiSize.X - panelWidth - 12.0f), 88.0f);
		canvas.DrawRect(new Rect2(panelPos, new Vector2(panelWidth, panelHeight)), new Color(0.0f, 0.0f, 0.0f, 0.52f));

		string debugTitle = camera != null
			? $"Debug Overlay (F10) - Cam: {camera.Position.X:F0},{camera.Position.Y:F0}"
			: "Debug Overlay (F10)";
		canvas.DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 20.0f), debugTitle, HorizontalAlignment.Left, -1.0f, 15, Colors.WhiteSmoke);

		canvas.DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 40.0f), "Events", HorizontalAlignment.Left, -1.0f, 14, Colors.WhiteSmoke);
		int maxEvents = Mathf.Min(events.Length, 8);
		for (int i = 0; i < maxEvents; i++)
		{
			int eventIndex = events.Length - maxEvents + i;
			canvas.DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 58.0f + i * 13.0f), events[eventIndex], HorizontalAlignment.Left, -1.0f, 12, Colors.LightGray);
		}
	}
}
