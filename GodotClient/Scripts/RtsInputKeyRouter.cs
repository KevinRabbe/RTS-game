using System;
using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsInputKeyRouter
{
	internal static void HandleKey(
		InputEventKey key,
		GodotFrameDto? frame,
		int localPlayerIndex,
		string dryArabiaMapName,
		Func<Vector2> getMousePosition,
		Func<Vector2, Vector2I> screenToTile,
		Func<int, long> tileToRaw,
		Func<long, long, Vector2> toScreen,
		Phase6SpriteRenderer spriteRenderer,
		RtsInputModeState inputModeState,
		RtsTownCenterPlacementState tcPlacementState,
		Action startDryArabiaTest01,
		Action startCombatTest01,
		Action<int> startLocalMatch,
		Action<Vector2> tryCreateTradeRoute,
		Action<int> trainFromSelectedBuilding,
		Action<int> researchFromSelectedBuilding,
		RtsSelectionController selectionController,
		GodotControlGroupState controlGroups,
		Action<string, Action<GodotClientFacade>> queueCommandAndConfirm,
		Action refreshFrame,
		Action queueRedraw,
		Action<string> addDebugEvent,
		RtsCommandMarker commandMarker,
		ref bool screenshotMode,
		ref bool paused,
		ref bool showDebugOverlay,
		ref bool showHotkeyHelp,
		int villagerUnitTypeId,
		int infantryUnitTypeId,
		int tradeCartUnitTypeId,
		int infantryAttackTechId,
		int localPlayer)
	{
		if (TryResolveControlGroup(key, out int groupIndex))
		{
			if (key.CtrlPressed)
			{
				int[] selected = selectionController.GetSelectedUnitIdsSorted();
				controlGroups.Assign(groupIndex, selected);
				addDebugEvent("control group " + groupIndex + " assigned units=" + (selected.Length == 0 ? "none" : string.Join(",", selected)));
				refreshFrame();
				return;
			}

			int[] stored = controlGroups.Recall(groupIndex);
			int[] recallable = GodotControlGroupResolver.FilterRecallableLocalUnitIds(frame, localPlayerIndex, stored);
			selectionController.SelectUnitIds(recallable, addDebugEvent);
			refreshFrame();
			return;
		}

		if (key.Keycode == Key.F1)
		{
			// Scenario switches are global and reset temporary command modes via session init.
			startDryArabiaTest01();
			addDebugEvent("restart " + dryArabiaMapName + " 1v1 (F1)");
			return;
		}

		if (key.Keycode == Key.F2)
		{
			startCombatTest01();
			addDebugEvent("restart CombatTest01 1v1 (F2)");
			return;
		}

		if (key.Keycode == Key.F6)
		{
			startLocalMatch(6);
			addDebugEvent("restart local 6-player ffa (F6)");
			return;
		}

		if (key.Keycode == Key.F12)
		{
			screenshotMode = !screenshotMode;
			addDebugEvent(screenshotMode ? "screenshot mode on" : "screenshot mode off");
			refreshFrame();
			return;
		}

		if (key.Keycode == Key.Space)
		{
			paused = !paused;
			addDebugEvent(paused ? "pause on" : "pause off");
			refreshFrame();
			return;
		}

		if (key.Keycode == Key.F9)
		{
			spriteRenderer.ToggleRenderMode();
			addDebugEvent("render mode -> " + spriteRenderer.RenderModeLabel);
			refreshFrame();
			return;
		}

		if (key.Keycode == Key.F10)
		{
			showDebugOverlay = !showDebugOverlay;
			addDebugEvent(showDebugOverlay ? "debug overlay on" : "debug overlay off");
			refreshFrame();
			return;
		}

		if (key.Keycode == Key.H || key.Keycode == Key.F11)
		{
			showHotkeyHelp = !showHotkeyHelp;
			addDebugEvent(showHotkeyHelp ? "hotkey help on" : "hotkey help off");
			refreshFrame();
			return;
		}

		if (key.Keycode == Key.C)
		{
			inputModeState.EnterTownCenterPlacement();
			tcPlacementState.Enter(frame, screenToTile(getMousePosition()));
			addDebugEvent("TC placement mode entered");
			queueRedraw();
			return;
		}

		if (key.Keycode == Key.A)
		{
			// Attack-move is an explicit temporary targeting mode. Normal RMB on ground
			// remains move; we do not overload default RMB semantics with attack-move.
			inputModeState.EnterAttackMoveTargeting();
			addDebugEvent("attack-move targeting mode entered");
			queueRedraw();
			return;
		}

		if (key.Keycode == Key.Escape)
		{
			// Escape cancels active temporary mode deterministically; no command emitted.
			if (tcPlacementState.IsActive)
			{
				tcPlacementState.Cancel();
				inputModeState.ExitToNormal();
				addDebugEvent("TC placement cancelled (Esc)");
				queueRedraw();
				return;
			}

			if (inputModeState.IsAttackMoveTargeting)
			{
				inputModeState.ExitToNormal();
				addDebugEvent("attack-move targeting cancelled (Esc)");
				queueRedraw();
				return;
			}

			inputModeState.ExitToNormal();
			queueRedraw();
		}

		if (key.Keycode == Key.W)
		{
			Vector2I tile = screenToTile(getMousePosition());
			commandMarker.Set("Wall", toScreen(tileToRaw(tile.X), tileToRaw(tile.Y)), Colors.LightGray);
			queueCommandAndConfirm(
				"place wall p=" + localPlayer + " tile=(" + tile.X + "," + tile.Y + ")",
				facade => facade.QueuePlaceWall(localPlayer, tile.X, tile.Y));
			return;
		}

		if (key.Keycode == Key.T)
		{
			Vector2I tile = screenToTile(getMousePosition());
			commandMarker.Set("TradePost", toScreen(tileToRaw(tile.X), tileToRaw(tile.Y)), Colors.Gold);
			queueCommandAndConfirm(
				"place trade post p=" + localPlayer + " tile=(" + tile.X + "," + tile.Y + ")",
				facade => facade.QueuePlaceTradePost(localPlayer, tile.X, tile.Y));
			return;
		}

		if (key.Keycode == Key.R)
		{
			tryCreateTradeRoute(getMousePosition());
			return;
		}

		if (key.Keycode == Key.V)
		{
			trainFromSelectedBuilding(villagerUnitTypeId);
			return;
		}

		if (key.Keycode == Key.I)
		{
			trainFromSelectedBuilding(infantryUnitTypeId);
			return;
		}

		if (key.Keycode == Key.K)
		{
			trainFromSelectedBuilding(tradeCartUnitTypeId);
			return;
		}

		if (key.Keycode == Key.Y)
		{
			researchFromSelectedBuilding(infantryAttackTechId);
		}
	}

	private static bool TryResolveControlGroup(InputEventKey key, out int groupIndex)
	{
		groupIndex = 0;
		switch (key.Keycode)
		{
			case Key.Key1:
			case Key.Kp1:
				groupIndex = 1;
				return true;
			case Key.Key2:
			case Key.Kp2:
				groupIndex = 2;
				return true;
			case Key.Key3:
			case Key.Kp3:
				groupIndex = 3;
				return true;
			case Key.Key4:
			case Key.Kp4:
				groupIndex = 4;
				return true;
			case Key.Key5:
			case Key.Kp5:
				groupIndex = 5;
				return true;
			case Key.Key6:
			case Key.Kp6:
				groupIndex = 6;
				return true;
			case Key.Key7:
			case Key.Kp7:
				groupIndex = 7;
				return true;
			case Key.Key8:
			case Key.Kp8:
				groupIndex = 8;
				return true;
			case Key.Key9:
			case Key.Kp9:
				groupIndex = 9;
				return true;
			default:
				return false;
		}
	}
}
