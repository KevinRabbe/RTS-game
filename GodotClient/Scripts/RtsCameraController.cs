using Godot;

public sealed class RtsCameraController
{
	private const float CameraPanPixelsPerSecond = 420.0f;

	private Camera2D? _camera;

	public Camera2D? Camera
	{
		get { return _camera; }
	}

	public void Initialize(Node owner)
	{
		_camera = new Camera2D();
		owner.AddChild(_camera);
		_camera.MakeCurrent();
	}

	public void Update(double delta)
	{
		if (_camera == null)
		{
			return;
		}

		Vector2 direction = Vector2.Zero;
		if (Input.IsKeyPressed(Key.Left))
		{
			direction.X -= 1.0f;
		}

		if (Input.IsKeyPressed(Key.Right))
		{
			direction.X += 1.0f;
		}

		if (Input.IsKeyPressed(Key.Up))
		{
			direction.Y -= 1.0f;
		}

		if (Input.IsKeyPressed(Key.Down))
		{
			direction.Y += 1.0f;
		}

		if (direction == Vector2.Zero)
		{
			return;
		}

		_camera.Position += direction.Normalized() * CameraPanPixelsPerSecond * (float)delta;
	}

	public Vector2 GetUiOrigin(Rect2 viewportRect)
	{
		if (_camera == null)
		{
			return Vector2.Zero;
		}

		Vector2 zoom = _camera.Zoom;
		float zoomX = Mathf.IsZeroApprox(zoom.X) ? 1.0f : zoom.X;
		float zoomY = Mathf.IsZeroApprox(zoom.Y) ? 1.0f : zoom.Y;
		float halfWidth = viewportRect.Size.X * 0.5f * zoomX;
		float halfHeight = viewportRect.Size.Y * 0.5f * zoomY;
		return new Vector2(_camera.Position.X - halfWidth + 12.0f, _camera.Position.Y - halfHeight + 12.0f);
	}

	public Vector2 GetUiSize(Rect2 viewportRect)
	{
		if (_camera == null)
		{
			return viewportRect.Size;
		}

		Vector2 zoom = _camera.Zoom;
		float zoomX = Mathf.IsZeroApprox(zoom.X) ? 1.0f : zoom.X;
		float zoomY = Mathf.IsZeroApprox(zoom.Y) ? 1.0f : zoom.Y;
		return new Vector2(viewportRect.Size.X * zoomX, viewportRect.Size.Y * zoomY);
	}
}
