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
		RtsInputModeState inputModeState,
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
			inputModeState.EnterTownCenterPlacement();
			if (mouse.ButtonIndex == MouseButton.Left)
			{
				confirmTcPlacement(tile);
			}
			else if (mouse.ButtonIndex == MouseButton.Right)
			{
				tcPlacementState.Cancel();
				inputModeState.ExitToNormal();
				addDebugEvent("TC placement cancelled (RMB)");
				queueRedraw();
			}
			return;
		}

		if (inputModeState.IsAttackMoveTargeting && mouse.ButtonIndex == MouseButton.Left)
		{
			if (selectionController.HasSelectedUnits)
			{
				int[] selectedUnitIds = selectionController.GetSelectedUnitIdsSorted();
				RtsResolvedCommand resolved = RtsCommandModeResolver.ResolveModeClick(
					inputModeState.CurrentMode,
					frame,
					localPlayerIndex,
					true,
					mouseXRaw,
					mouseYRaw);
				ExecuteResolvedCommand(
					resolved,
					frame,
					localPlayerIndex,
					selectedUnitIds,
					tile,
					toScreen,
					tileToRaw,
					queueCommandAndConfirm,
					setCommandMarker);
				addDebugEvent(
					"lclick mode raw=(" + mouseXRaw + "," + mouseYRaw + ") tile=(" + tile.X + "," + tile.Y + ") route=" + resolved.Kind + " mode=" + inputModeState.CurrentMode);
			}

			inputModeState.ExitToNormal();
			queueRedraw();
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

		if (mouse.ButtonIndex == MouseButton.Right && inputModeState.IsAttackMoveTargeting && !selectionController.HasSelectedUnits)
		{
			inputModeState.ExitToNormal();
			addDebugEvent("attack-move targeting cancelled (no selected units)");
			queueRedraw();
			return;
		}

		if (mouse.ButtonIndex == MouseButton.Right && selectionController.HasSelectedUnits)
		{
			int[] selectedUnitIds = selectionController.GetSelectedUnitIdsSorted();
			GodotInteractionProbeResult probe = GodotInteractionProbe.Probe(frame, localPlayerIndex, mouseXRaw, mouseYRaw);
			RtsResolvedCommand resolved = RtsCommandModeResolver.ResolveModeClick(
				inputModeState.CurrentMode,
				frame,
				localPlayerIndex,
				selectionController.HasSelectedUnits,
				mouseXRaw,
				mouseYRaw);
			addDebugEvent(
				"rclick raw=(" + mouseXRaw + "," + mouseYRaw + ") tile=(" + tile.X + "," + tile.Y + ") target="
				+ probe.TargetKind + ":" + probe.TargetEntityId + " route=" + resolved.Kind + " mode=" + inputModeState.CurrentMode);
			ExecuteResolvedCommand(
				resolved,
				frame,
				localPlayerIndex,
				selectedUnitIds,
				tile,
				toScreen,
				tileToRaw,
				queueCommandAndConfirm,
				setCommandMarker);

			if (inputModeState.IsAttackMoveTargeting)
			{
				inputModeState.ExitToNormal();
			}
		}
	}

	private static void ExecuteResolvedCommand(
		RtsResolvedCommand resolved,
		GodotFrameDto frame,
		int localPlayerIndex,
		int[] selectedUnitIds,
		Vector2I tile,
		Func<long, long, Vector2> toScreen,
		Func<int, long> tileToRaw,
		Action<string, Action<GodotClientFacade>> queueCommandAndConfirm,
		Action<string, Vector2, Color> setCommandMarker)
	{
		if (resolved.Kind == RtsResolvedCommandKind.Attack)
		{
			GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, resolved.TargetEntityId);
			if (target != null)
			{
				setCommandMarker("Attack", toScreen(target.XRaw, target.YRaw), Colors.IndianRed);
			}

			queueCommandAndConfirm(
				"attack p=" + localPlayerIndex + " targetEntity=" + resolved.TargetEntityId + " units=[" + string.Join(",", selectedUnitIds) + "]",
				f => f.QueueAttack(localPlayerIndex, selectedUnitIds, resolved.TargetEntityId));
		}
		else if (resolved.Kind == RtsResolvedCommandKind.AssignBuild)
		{
			GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, resolved.TargetEntityId);
			if (target != null)
			{
				setCommandMarker("Build", toScreen(target.XRaw, target.YRaw), Colors.Khaki);
			}

			queueCommandAndConfirm(
				"assign build p=" + localPlayerIndex + " targetBuilding=" + resolved.TargetEntityId + " units=[" + string.Join(",", selectedUnitIds) + "]",
				f => f.QueueAssignBuild(localPlayerIndex, resolved.TargetEntityId, selectedUnitIds));
		}
		else if (resolved.Kind == RtsResolvedCommandKind.Gather)
		{
			GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, resolved.ResourceNodeId);
			if (target != null)
			{
				setCommandMarker("Gather", toScreen(target.XRaw, target.YRaw), Colors.ForestGreen);
			}

			queueCommandAndConfirm(
				"gather p=" + localPlayerIndex + " resource=" + resolved.ResourceNodeId + " units=[" + string.Join(",", selectedUnitIds) + "]",
				f => f.QueueGatherResource(localPlayerIndex, resolved.ResourceNodeId, selectedUnitIds));
		}
		else if (resolved.Kind == RtsResolvedCommandKind.Move)
		{
			setCommandMarker("Move", toScreen(tileToRaw(tile.X), tileToRaw(tile.Y)), Colors.LightSkyBlue);
			queueCommandAndConfirm(
				"move p=" + localPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ") units=[" + string.Join(",", selectedUnitIds) + "]",
				f => f.QueueMoveUnits(localPlayerIndex, selectedUnitIds, tile.X, tile.Y));
		}
		else if (resolved.Kind == RtsResolvedCommandKind.AttackMove)
		{
			setCommandMarker("AttackMove", toScreen(tileToRaw(tile.X), tileToRaw(tile.Y)), Colors.OrangeRed);
			queueCommandAndConfirm(
				"attack-move p=" + localPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ") units=[" + string.Join(",", selectedUnitIds) + "]",
				f => f.QueueAttackMove(localPlayerIndex, selectedUnitIds, tile.X, tile.Y));
		}
	}
}
