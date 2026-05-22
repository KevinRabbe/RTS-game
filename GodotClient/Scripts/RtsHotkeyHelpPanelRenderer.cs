using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsHotkeyHelpPanelRenderer
{
	public static void Draw(CanvasItem canvas, Vector2 uiOrigin, Vector2 uiSize, GodotHotkeyHelpEntry[] entries)
	{
		Vector2 panelPos = uiOrigin + new Vector2(Mathf.Max(0.0f, uiSize.X - 620.0f - 12.0f), 264.0f);
		float panelWidth = 620.0f;
		float panelHeight = 24.0f + entries.Length * 16.0f + 12.0f;
		canvas.DrawRect(new Rect2(panelPos, new Vector2(panelWidth, panelHeight)), new Color(0.0f, 0.0f, 0.0f, 0.56f));
		canvas.DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 20.0f), "Hotkeys (H/F11)", HorizontalAlignment.Left, -1.0f, 15, Colors.WhiteSmoke);
		for (int i = 0; i < entries.Length; i++)
		{
			string line = entries[i].Input + ": " + entries[i].Action;
			canvas.DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 38.0f + i * 16.0f), line, HorizontalAlignment.Left, -1.0f, 13, Colors.LightGray);
		}
	}
}
