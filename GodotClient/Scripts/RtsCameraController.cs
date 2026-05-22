using Godot;

internal sealed class RtsCameraController
{
	private const float EdgePanMarginPx = 32.0f;
	private const float EdgePanSpeedPixelsPerSecond = 380.0f;

	private bool _middleDragActive;
	private Vector2 _middleDragStartScreen = Vector2.Zero;
	private Vector2 _middleDragStartCamera = Vector2.Zero;

	public bool IsMiddleDragActive
	{
		get { return _middleDragActive; }
	}

	public void BeginMiddleDrag(Camera2D? camera, Vector2 pointerScreen)
	{
		_middleDragActive = true;
		_middleDragStartScreen = pointerScreen;
		_middleDragStartCamera = camera?.Position ?? Vector2.Zero;
	}

	public void EndMiddleDrag()
	{
		_middleDragActive = false;
	}

	public void ApplyMiddleDrag(Camera2D? camera, Vector2 pointerScreen)
	{
		if (!_middleDragActive || camera == null)
		{
			return;
		}

		Vector2 panDelta = pointerScreen - _middleDragStartScreen;
		camera.Position = _middleDragStartCamera - panDelta;
	}

	public void UpdateEdgeAndKeyPan(Camera2D? camera, Viewport viewport, double delta)
	{
		if (camera == null || _middleDragActive)
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

		Vector2 mousePos = viewport.GetMousePosition();
		Rect2 viewportRect = viewport.GetVisibleRect();
		if (mousePos.X <= EdgePanMarginPx)
		{
			direction.X -= 1.0f;
		}
		else if (mousePos.X >= viewportRect.Size.X - EdgePanMarginPx)
		{
			direction.X += 1.0f;
		}

		if (mousePos.Y <= EdgePanMarginPx)
		{
			direction.Y -= 1.0f;
		}
		else if (mousePos.Y >= viewportRect.Size.Y - EdgePanMarginPx)
		{
			direction.Y += 1.0f;
		}

		if (direction == Vector2.Zero)
		{
			return;
		}

		camera.Position += direction.Normalized() * EdgePanSpeedPixelsPerSecond * (float)delta;
	}

	public void ClampToMapBounds(Camera2D? camera, Rect2 viewportRect, int mapWidthTiles, int mapHeightTiles, float tilePixels)
	{
		if (camera == null)
		{
			return;
		}

		float mapWidthPx = mapWidthTiles * tilePixels;
		float mapHeightPx = mapHeightTiles * tilePixels;
		Vector2 zoom = camera.Zoom;
		float zoomX = Mathf.IsZeroApprox(zoom.X) ? 1.0f : zoom.X;
		float zoomY = Mathf.IsZeroApprox(zoom.Y) ? 1.0f : zoom.Y;
		float viewWidth = viewportRect.Size.X / zoomX;
		float viewHeight = viewportRect.Size.Y / zoomY;
		float halfViewWidth = viewWidth * 0.5f;
		float halfViewHeight = viewHeight * 0.5f;

		float minX;
		float maxX;
		float minY;
		float maxY;
		if (viewWidth < mapWidthPx)
		{
			minX = halfViewWidth;
			maxX = mapWidthPx - halfViewWidth;
		}
		else
		{
			minX = maxX = mapWidthPx * 0.5f;
		}

		if (viewHeight < mapHeightPx)
		{
			minY = halfViewHeight;
			maxY = mapHeightPx - halfViewHeight;
		}
		else
		{
			minY = maxY = mapHeightPx * 0.5f;
		}

		float clampedX = Mathf.Clamp(camera.Position.X, minX, maxX);
		float clampedY = Mathf.Clamp(camera.Position.Y, minY, maxY);
		camera.Position = new Vector2(clampedX, clampedY);
	}
}
