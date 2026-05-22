using Godot;
using RtsGame.Presentation.GodotBridge;

internal sealed class RtsTownCenterPlacementState
{
	private bool _active;
	private Vector2I _hoveredTile;
	private TcPlacementPreviewResult _preview = TcPlacementPreviewResult.Unknown;

	public bool IsActive
	{
		get { return _active; }
	}

	public Vector2I HoveredTile
	{
		get { return _hoveredTile; }
	}

	public TcPlacementPreviewResult PreviewResult
	{
		get { return _preview; }
	}

	public void Reset()
	{
		_active = false;
		_hoveredTile = default;
		_preview = TcPlacementPreviewResult.Unknown;
	}

	public void Enter(GodotFrameDto? frame, Vector2I tile)
	{
		_active = true;
		_hoveredTile = tile;
		_preview = frame != null
			? TcPlacementPreview.Evaluate(frame, tile.X, tile.Y)
			: TcPlacementPreviewResult.Unknown;
	}

	public void Cancel()
	{
		_active = false;
	}

	public bool UpdateHoverIfChanged(GodotFrameDto frame, Vector2I tile)
	{
		if (!_active || tile == _hoveredTile)
		{
			return false;
		}

		_hoveredTile = tile;
		_preview = TcPlacementPreview.Evaluate(frame, tile.X, tile.Y);
		return true;
	}
}
