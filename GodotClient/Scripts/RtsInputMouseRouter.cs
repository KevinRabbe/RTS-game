using System;
using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsInputMouseRouter
{
	internal static void HandleMouse(
		InputEventMouseButton mouse,
		GodotClientFacade? facade,
		GodotFrameDto? frame,
		int localPlayerIndex,
		Vector2 mouseWorldPosition,
		Vector2I tile,
		long mouseXRaw,
		long mouseYRaw,
		int hoveredResourceNodeId,
		RtsTownCenterPlacementState tcPlacementState,
		RtsSelectionController selectionController,
		RtsTradeRouteSelection tradeRouteSelection,
		Func<GodotFrameDto?, Vector2, int, int> findResourceAt,
		Action refreshFrame,
		Action queueRedraw,
		Action<string> addDebugEvent,
		Action<Vector2I> confirmTcPlacement,
		Action<string, Action<GodotClientFacade>> queueCommandAndConfirm,
		Func<long, long, Vector2> toScreen,
		Func<int, long> tileToRaw,
		Action<string, Vector2, Color> setCommandMarker)
	{
		if (facade == null || frame == null)
		{
			return;
		}

		if (tcPlacementState.IsActive)
		{
			if (mouse.ButtonIndex == MouseButton.Left)
			{
				confirmTcPlacement(tile);
			}
			else if (mouse.ButtonIndex == MouseButton.Right)
			{
				tcPlacementState.Cancel();
				addDebugEvent("TC placement cancelled (RMB)");
				queueRedraw();
			}
			return;
		}

		if (mouse.ButtonIndex == MouseButton.Left)
		{
			tradeRouteSelection.Clear();
			selectionController.SelectAt(
				frame,
				localPlayerIndex,
				mouseWorldPosition,
				x => RtsCoordinateTransform.ScreenToRaw(x, 16),
				findResourceAt(frame, mouseWorldPosition, 16),
				hoveredResourceNodeId,
				addDebugEvent);
			refreshFrame();
			return;
		}

		if (mouse.ButtonIndex == MouseButton.Right && selectionController.HasSelectedUnits)
		{
			int[] selectedUnitIds = selectionController.GetSelectedUnitIdsSorted();
			GodotInteractionProbeResult probe = GodotInteractionProbe.Probe(frame, localPlayerIndex, mouseXRaw, mouseYRaw);
			GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(
				frame,
				localPlayerIndex,
				selectionController.HasSelectedUnits,
				mouseXRaw,
				mouseYRaw);
			addDebugEvent(
				"rclick raw=(" + mouseXRaw + "," + mouseYRaw + ") tile=(" + tile.X + "," + tile.Y + ") target="
				+ probe.TargetKind + ":" + probe.TargetEntityId + " route=" + intent.Kind);

			if (intent.Kind == GodotInteractionIntentKind.Attack)
			{
				GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, intent.TargetEntityId);
				if (target != null)
				{
					setCommandMarker("Attack", toScreen(target.XRaw, target.YRaw), Colors.IndianRed);
				}

				queueCommandAndConfirm(
					"attack p=" + localPlayerIndex + " targetEntity=" + intent.TargetEntityId + " units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueAttack(localPlayerIndex, selectedUnitIds, intent.TargetEntityId));
			}
			else if (intent.Kind == GodotInteractionIntentKind.AssignBuild)
			{
				GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, intent.TargetEntityId);
				if (target != null)
				{
					setCommandMarker("Build", toScreen(target.XRaw, target.YRaw), Colors.Khaki);
				}

				queueCommandAndConfirm(
					"assign build p=" + localPlayerIndex + " targetBuilding=" + intent.TargetEntityId + " units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueAssignBuild(localPlayerIndex, intent.TargetEntityId, selectedUnitIds));
			}
			else if (intent.Kind == GodotInteractionIntentKind.GatherResource)
			{
				GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, intent.ResourceNodeId);
				if (target != null)
				{
					setCommandMarker("Gather", toScreen(target.XRaw, target.YRaw), Colors.ForestGreen);
				}

				queueCommandAndConfirm(
					"gather p=" + localPlayerIndex + " resource=" + intent.ResourceNodeId + " units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueGatherResource(localPlayerIndex, intent.ResourceNodeId, selectedUnitIds));
			}
			else if (intent.Kind == GodotInteractionIntentKind.Move)
			{
				setCommandMarker("Move", toScreen(tileToRaw(tile.X), tileToRaw(tile.Y)), Colors.LightSkyBlue);
				queueCommandAndConfirm(
					"move p=" + localPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ") units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueMoveUnits(localPlayerIndex, selectedUnitIds, tile.X, tile.Y));
			}
		}
	}
}
