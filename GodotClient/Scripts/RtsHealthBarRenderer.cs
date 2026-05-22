using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsHealthBarRenderer
{
	public static void Draw(CanvasItem canvas, Vector2 center, GodotPrimitiveDto primitive)
	{
		if (primitive.MaxHitPoints <= 0)
		{
			return;
		}

		float width = 12.0f;
		float ratio = Mathf.Clamp((float)primitive.CurrentHitPoints / primitive.MaxHitPoints, 0.0f, 1.0f);
		var background = new Rect2(center.X - width * 0.5f, center.Y - 12.0f, width, 2.0f);
		var foreground = new Rect2(background.Position, new Vector2(width * ratio, 2.0f));
		canvas.DrawRect(background, Colors.Black);
		canvas.DrawRect(foreground, Colors.LimeGreen);
	}
}
