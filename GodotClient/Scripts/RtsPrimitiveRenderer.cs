using System;
using System.Collections.Generic;
using Godot;
using RtsGame.Presentation.GodotBridge;

public static class RtsPrimitiveRenderer
{
	public static void Draw(
		Node2D canvas,
		GodotPrimitiveDto primitive,
		GodotFrameDto? frame,
		Phase6SpriteRenderer spriteRenderer,
		int localPlayerIndex,
		IReadOnlyCollection<int> selectedUnitIds,
		int selectedBuildingId,
		int hoveredBuildingId,
		int hoveredResourceNodeId,
		Func<long, long, Vector2> toScreen,
		Func<long, float> rawToPixels,
		Func<GodotPrimitiveDto, Rect2> primitiveRect,
		Action<GodotPrimitiveDto> drawConstructionOverlayIfNeeded)
	{
		switch (GodotPrimitiveDrawKindResolver.Resolve(primitive))
		{
			case GodotPrimitiveDrawKind.Unit:
				DrawUnit(canvas, primitive, frame, spriteRenderer, localPlayerIndex, selectedUnitIds, toScreen, rawToPixels, primitiveRect);
				break;
			case GodotPrimitiveDrawKind.Building:
				DrawBuilding(canvas, primitive, frame, spriteRenderer, selectedBuildingId, hoveredBuildingId, toScreen, rawToPixels, primitiveRect, drawConstructionOverlayIfNeeded);
				break;
			case GodotPrimitiveDrawKind.TradeRoute:
				RtsTradeRouteRenderer.Draw(canvas, toScreen(primitive.XRaw, primitive.YRaw), toScreen(primitive.EndXRaw, primitive.EndYRaw));
				break;
			case GodotPrimitiveDrawKind.HealthBar:
				RtsHealthBarRenderer.Draw(canvas, toScreen(primitive.XRaw, primitive.YRaw), primitive);
				break;
			case GodotPrimitiveDrawKind.FogOverlay:
				canvas.DrawRect(new Rect2(Vector2.Zero, new Vector2(2048.0f, 1536.0f)), new Color(0.02f, 0.02f, 0.02f, 0.12f));
				break;
			case GodotPrimitiveDrawKind.Resource:
				DrawResource(canvas, primitive, spriteRenderer, toScreen, rawToPixels, primitiveRect, hoveredResourceNodeId);
				break;
		}
	}

	private static void DrawUnit(
		Node2D canvas,
		GodotPrimitiveDto primitive,
		GodotFrameDto? frame,
		Phase6SpriteRenderer spriteRenderer,
		int localPlayerIndex,
		IReadOnlyCollection<int> selectedUnitIds,
		Func<long, long, Vector2> toScreen,
		Func<long, float> rawToPixels,
		Func<GodotPrimitiveDto, Rect2> primitiveRect)
	{
		bool isSelected = ContainsUnitId(selectedUnitIds, primitive.EntityId);
		if (spriteRenderer.TryDrawUnit(canvas, primitive, frame, selectedUnitIds, toScreen, rawToPixels))
		{
			if (isSelected)
			{
				Vector2 center = toScreen(primitive.XRaw, primitive.YRaw);
				float radiusSource = Mathf.Max(rawToPixels(primitive.WidthRaw), rawToPixels(primitive.HeightRaw));
				RtsSelectionRingRenderer.Draw(canvas, center, radiusSource, Colors.Aqua);
			}

			return;
		}

		Rect2 rect = primitiveRect(primitive);
		Color color = RtsVisualStyleColors.Resolve(GodotVisualStyleResolver.ResolveUnit(primitive, localPlayerIndex));
		canvas.DrawRect(rect, color);
		if (isSelected)
		{
			Vector2 center = toScreen(primitive.XRaw, primitive.YRaw);
			float radiusSource = Mathf.Max(rawToPixels(primitive.WidthRaw), rawToPixels(primitive.HeightRaw));
			RtsSelectionRingRenderer.Draw(canvas, center, radiusSource, Colors.Aqua);
			canvas.DrawRect(rect.Grow(2.0f), Colors.White, false, 2.0f);
		}
	}

	private static bool ContainsUnitId(IReadOnlyCollection<int> selectedUnitIds, int entityId)
	{
		foreach (int selectedId in selectedUnitIds)
		{
			if (selectedId == entityId)
			{
				return true;
			}
		}

		return false;
	}

	private static void DrawBuilding(
		Node2D canvas,
		GodotPrimitiveDto primitive,
		GodotFrameDto? frame,
		Phase6SpriteRenderer spriteRenderer,
		int selectedBuildingId,
		int hoveredBuildingId,
		Func<long, long, Vector2> toScreen,
		Func<long, float> rawToPixels,
		Func<GodotPrimitiveDto, Rect2> primitiveRect,
		Action<GodotPrimitiveDto> drawConstructionOverlayIfNeeded)
	{
		bool isSelected = selectedBuildingId == primitive.EntityId;
		bool isHovered = hoveredBuildingId == primitive.EntityId;
		if (spriteRenderer.TryDrawBuilding(canvas, primitive, frame, selectedBuildingId, toScreen, rawToPixels))
		{
			if (isSelected)
			{
				RtsBuildingFootprintOutlineRenderer.Draw(canvas, primitiveRect(primitive), Colors.Gold);
			}
			else if (isHovered)
			{
				RtsBuildingFootprintOutlineRenderer.Draw(canvas, primitiveRect(primitive), Colors.Khaki);
			}

			drawConstructionOverlayIfNeeded(primitive);
			return;
		}

		Rect2 rect = primitiveRect(primitive);
		Color color = RtsVisualStyleColors.Resolve(GodotVisualStyleResolver.ResolveBuilding(primitive));
		canvas.DrawRect(rect, color);
		if (isSelected)
		{
			RtsBuildingFootprintOutlineRenderer.Draw(canvas, primitiveRect(primitive), Colors.Gold);
		}
		else if (isHovered)
		{
			RtsBuildingFootprintOutlineRenderer.Draw(canvas, primitiveRect(primitive), Colors.Khaki);
		}

		drawConstructionOverlayIfNeeded(primitive);
	}

	private static void DrawResource(
		Node2D canvas,
		GodotPrimitiveDto primitive,
		Phase6SpriteRenderer spriteRenderer,
		Func<long, long, Vector2> toScreen,
		Func<long, float> rawToPixels,
		Func<GodotPrimitiveDto, Rect2> primitiveRect,
		int hoveredResourceNodeId)
	{
		if (spriteRenderer.TryDrawResource(canvas, primitive, toScreen, rawToPixels))
		{
			if (primitive.EntityId == hoveredResourceNodeId)
			{
				RtsResourceHoverRenderer.DrawHoverRing(canvas, primitiveRect(primitive).Grow(6.0f));
			}

			return;
		}

		Rect2 rect = primitiveRect(primitive);
		Color color = RtsVisualStyleColors.Resolve(GodotVisualStyleResolver.ResolveResource(primitive));
		canvas.DrawCircle(rect.GetCenter(), rect.Size.X * 0.5f, color);
		if (primitive.EntityId == hoveredResourceNodeId)
		{
			RtsResourceHoverRenderer.DrawHoverRing(canvas, rect);
		}
	}
}
