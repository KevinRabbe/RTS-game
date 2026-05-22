using Godot;

internal static class RtsUiViewportMetrics
{
	public static Vector2 GetUiOrigin(Camera2D? camera, Rect2 viewportRect)
	{
		if (camera == null)
		{
			return Vector2.Zero;
		}

		Vector2 zoom = camera.Zoom;
		float zoomX = Mathf.IsZeroApprox(zoom.X) ? 1.0f : zoom.X;
		float zoomY = Mathf.IsZeroApprox(zoom.Y) ? 1.0f : zoom.Y;
		float halfWidth = viewportRect.Size.X * 0.5f * zoomX;
		float halfHeight = viewportRect.Size.Y * 0.5f * zoomY;
		return new Vector2(camera.Position.X - halfWidth + 12.0f, camera.Position.Y - halfHeight + 12.0f);
	}

	public static Vector2 GetUiSize(Camera2D? camera, Rect2 viewportRect)
	{
		if (camera == null)
		{
			return viewportRect.Size;
		}

		Vector2 zoom = camera.Zoom;
		float zoomX = Mathf.IsZeroApprox(zoom.X) ? 1.0f : zoom.X;
		float zoomY = Mathf.IsZeroApprox(zoom.Y) ? 1.0f : zoom.Y;
		return new Vector2(viewportRect.Size.X * zoomX, viewportRect.Size.Y * zoomY);
	}
}
