using Godot;

internal sealed class RtsCommandMarker
{
	private string _label = string.Empty;
	private Color _color = Colors.White;
	private Vector2 _worldPosition = Vector2.Zero;
	private int _ticksRemaining;

	public void Tick()
	{
		if (_ticksRemaining > 0)
		{
			_ticksRemaining--;
		}
	}

	public void Set(string label, Vector2 worldPosition, Color color)
	{
		_label = label;
		_worldPosition = worldPosition;
		_color = color;
		_ticksRemaining = 40;
	}

	public void Draw(CanvasItem canvas)
	{
		if (_ticksRemaining <= 0)
		{
			return;
		}

		float pulse = 1.0f + (_ticksRemaining % 6) * 0.08f;
		canvas.DrawArc(_worldPosition, 8.0f * pulse, 0.0f, Mathf.Tau, 36, _color, 2.0f);
		canvas.DrawString(ThemeDB.FallbackFont, _worldPosition + new Vector2(10.0f, -8.0f), _label, HorizontalAlignment.Left, -1.0f, 12, _color);
	}
}
