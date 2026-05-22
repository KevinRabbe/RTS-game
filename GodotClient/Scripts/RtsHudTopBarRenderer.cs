using Godot;
using RtsGame.Presentation.GodotBridge;

public static class RtsHudTopBarRenderer
{
	public static void DrawMain(
		Node2D canvas,
		Vector2 uiOrigin,
		string[] lines,
		Phase6SpriteRenderer spriteRenderer)
	{
		float hudHeight = 64.0f;
		if (spriteRenderer.LoadedAssetCount < spriteRenderer.ExpectedAssetCount)
		{
			hudHeight = 82.0f;
		}

		canvas.DrawRect(new Rect2(uiOrigin, new Vector2(1120.0f, hudHeight)), new Color(0.0f, 0.0f, 0.0f, 0.50f));
		for (int i = 0; i < lines.Length; i++)
		{
			canvas.DrawString(ThemeDB.FallbackFont, uiOrigin + new Vector2(12.0f, 20.0f + i * 18.0f), lines[i], HorizontalAlignment.Left, -1.0f, 16, Colors.White);
		}

		canvas.DrawString(
			ThemeDB.FallbackFont,
			uiOrigin + new Vector2(12.0f, 56.0f),
			"Render " + spriteRenderer.RenderModeLabel + " (F9)  Assets " + spriteRenderer.LoadedAssetCount + "/" + spriteRenderer.ExpectedAssetCount,
			HorizontalAlignment.Left,
			-1.0f,
			16,
			Colors.White);

		if (spriteRenderer.LoadedAssetCount < spriteRenderer.ExpectedAssetCount)
		{
			canvas.DrawString(
				ThemeDB.FallbackFont,
				uiOrigin + new Vector2(12.0f, 74.0f),
				"Missing: " + spriteRenderer.MissingAssetsLabel,
				HorizontalAlignment.Left,
				-1.0f,
				14,
				Colors.LightGray);
		}
	}

	public static void DrawMinimal(
		Node2D canvas,
		Vector2 uiOrigin,
		GodotFrameDto frame,
		Phase6SpriteRenderer spriteRenderer)
	{
		GodotLocalPlayerDto player = frame.LocalPlayer;
		string line = "Tick " + frame.Tick
			+ "  Map " + frame.MapName
			+ "  Food " + player.Food
			+ "  Wood " + player.Wood
			+ "  Gold " + player.Gold
			+ "  Pop " + player.PopulationUsed + "/" + player.PopulationCap
			+ "  " + spriteRenderer.RenderModeLabel;

		canvas.DrawRect(new Rect2(uiOrigin, new Vector2(1120.0f, 32.0f)), new Color(0.0f, 0.0f, 0.0f, 0.50f));
		canvas.DrawString(ThemeDB.FallbackFont, uiOrigin + new Vector2(12.0f, 22.0f), line, HorizontalAlignment.Left, -1.0f, 16, Colors.White);
	}
}
