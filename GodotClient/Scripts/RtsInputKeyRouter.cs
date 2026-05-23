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
		RtsTownCenterPlacementState tcPlacementState,
		Action startDryArabiaTest01,
		Action startCombatTest01,
		Action<int> startLocalMatch,
		Action<Vector2> tryCreateTradeRoute,
		Action<int> trainFromSelectedBuilding,
		Action<int> researchFromSelectedBuilding,
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
		if (key.Keycode == Key.F1)
		{
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
			tcPlacementState.Enter(frame, screenToTile(getMousePosition()));
			addDebugEvent("TC placement mode entered");
			queueRedraw();
			return;
		}

		if (key.Keycode == Key.Escape && tcPlacementState.IsActive)
		{
			tcPlacementState.Cancel();
			addDebugEvent("TC placement cancelled (Esc)");
			queueRedraw();
			return;
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
}
