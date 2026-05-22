using System;
using System.Collections.Generic;
using Godot;
using RtsGame.Presentation.GodotBridge;

public partial class RtsClientRoot : Node2D
{
	private const int LocalPlayerIndex = 0;
	private const float TilePixels = 16.0f;
	private const double TickSeconds = 1.0 / 20.0;
	private const ulong DefaultMatchSeed = 12345UL;
	private const int VillagerUnitTypeId = 1;
	private const int InfantryUnitTypeId = 3;
	private const int TradeCartUnitTypeId = 5;
	private const int InfantryAttackTechId = 1;
	private const int TownCenterBuildingTypeId = 1;
	private const int WallBuildingTypeId = 2;
	private const int TradePostBuildingTypeId = 3;
	private readonly GodotDebugEventLog _debugEventLog = new GodotDebugEventLog(10);
	private Camera2D? _camera;
	private readonly RtsCameraController _cameraController = new RtsCameraController();
	private readonly RtsSelectionController _selectionController = new RtsSelectionController();
	private readonly RtsCommandMarker _commandMarker = new RtsCommandMarker();
	private readonly RtsTradeRouteSelection _tradeRouteSelection = new RtsTradeRouteSelection();
	private readonly RtsTownCenterPlacementState _tcPlacementState = new RtsTownCenterPlacementState();
	private GodotClientFacade? _facade;
	private GodotFrameDto? _frame;
	private readonly Phase6SpriteRenderer _spriteRenderer = new Phase6SpriteRenderer();
	private double _tickAccumulator;
	private bool _paused;
	private bool _showDebugOverlay = true;
	private bool _showHotkeyHelp;
	private bool _screenshotMode;
	private int _hoveredResourceNodeId;
	private int _hoveredBuildingId;

	public override void _Ready()
	{
		_camera = new Camera2D();
		AddChild(_camera);
		_camera.MakeCurrent();
		_spriteRenderer.LoadAssets();
		StartDryArabiaTest01();
		_debugEventLog.Add("ready " + DryArabiaMapName);
	}

	public override void _Process(double delta)
	{
		GodotClientFacade? facade = _facade;
		if (facade == null)
		{
			return;
		}

		_cameraController.UpdateEdgeAndKeyPan(_camera, GetViewport(), delta);
		RefreshHoveredTargetsFromMouse();
		_commandMarker.Tick();

		// Update TC placement ghost tile each frame (no command spam).
		if (_tcPlacementState.IsActive && _frame != null)
		{
			Vector2I newTile = ScreenToTile(GetGlobalMousePosition());
			if (_tcPlacementState.UpdateHoverIfChanged(_frame, newTile))
			{
				QueueRedraw();
			}
		}

		if (_cameraController.IsMiddleDragActive)
		{
			_cameraController.ApplyMiddleDrag(_camera, GetViewport().GetMousePosition());
			QueueRedraw();
		}

		_cameraController.ClampToMapBounds(_camera, GetViewportRect(), facade.MapWidthTiles, facade.MapHeightTiles, TilePixels);

		if (_paused)
		{
			RefreshFrame();
			return;
		}

		_tickAccumulator += delta;
		while (_tickAccumulator >= TickSeconds)
		{
			facade.AdvanceOneTick();
			_tickAccumulator -= TickSeconds;
		}

		RefreshFrame();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_facade == null)
		{
			return;
		}

		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			HandleKey(key);
			return;
		}

		if (@event is InputEventMouseButton mouse)
		{
			// Track middle-mouse press/release for drag panning.
			if (mouse.ButtonIndex == MouseButton.Middle)
			{
				if (mouse.Pressed)
				{
					_cameraController.BeginMiddleDrag(_camera, GetViewport().GetMousePosition());
				}
				else
				{
					_cameraController.EndMiddleDrag();
				}
				return;
			}

			if (_tcPlacementState.IsActive)
			{
				if (mouse.Pressed)
				{
					HandleMouse(mouse);
				}
				return;
			}

			if (mouse.ButtonIndex == MouseButton.Left)
			{
				if (mouse.Pressed)
				{
					_selectionController.BeginDrag(GetGlobalMousePosition());
					QueueRedraw();
				}
				else
				{
					FinishSelectionDrag();
				}
				return;
			}

			if (mouse.Pressed)
			{
				HandleMouse(mouse);
			}
			return;
		}

		if (@event is InputEventMouseMotion && _selectionController.IsDragActive)
		{
			_selectionController.UpdateDrag(GetGlobalMousePosition());
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (_frame == null)
		{
			return;
		}

		_spriteRenderer.DrawTerrain(this, _frame.MapName, _facade!.MapWidthTiles, _facade.MapHeightTiles, TilePixels);
		if (_showDebugOverlay)
		{
			DrawSpatialBlockersOverlay();
		}

		for (int i = 0; i < _frame.Primitives.Length; i++)
		{
			DrawPrimitive(_frame.Primitives[i]);
		}

		if (_tcPlacementState.IsActive)
		{
			RtsTownCenterPlacementRenderer.DrawGhost(this, _tcPlacementState, TilePixels, TileToRaw, ToScreen);
		}

		_selectionController.DrawDragRectangle(this);

		DrawHud();
	}

	private void HandleKey(InputEventKey key)
	{
		if (key.Keycode == Key.F1)
		{
			StartDryArabiaTest01();
			_debugEventLog.Add("restart " + DryArabiaMapName + " 1v1 (F1)");
			return;
		}

		if (key.Keycode == Key.F6)
		{
			StartLocalMatch(6);
			_debugEventLog.Add("restart local 6-player ffa (F6)");
			return;
		}

		if (key.Keycode == Key.F12)
		{
			_screenshotMode = !_screenshotMode;
			_debugEventLog.Add(_screenshotMode ? "screenshot mode on" : "screenshot mode off");
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.Space)
		{
			_paused = !_paused;
			_debugEventLog.Add(_paused ? "pause on" : "pause off");
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.F9)
		{
			_spriteRenderer.ToggleRenderMode();
			_debugEventLog.Add("render mode -> " + _spriteRenderer.RenderModeLabel);
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.F10)
		{
			_showDebugOverlay = !_showDebugOverlay;
			_debugEventLog.Add(_showDebugOverlay ? "debug overlay on" : "debug overlay off");
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.H || key.Keycode == Key.F11)
		{
			_showHotkeyHelp = !_showHotkeyHelp;
			_debugEventLog.Add(_showHotkeyHelp ? "hotkey help on" : "hotkey help off");
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.C)
		{
			_tcPlacementState.Enter(_frame, ScreenToTile(GetGlobalMousePosition()));
			_debugEventLog.Add("TC placement mode entered");
			QueueRedraw();
			return;
		}

		if (key.Keycode == Key.Escape && _tcPlacementState.IsActive)
		{
			_tcPlacementState.Cancel();
			_debugEventLog.Add("TC placement cancelled (Esc)");
			QueueRedraw();
			return;
		}

		if (key.Keycode == Key.W)
		{
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			_commandMarker.Set("Wall", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.LightGray);
			QueueCommandAndConfirm(
				"place wall p=" + LocalPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ")",
				facade => facade.QueuePlaceWall(LocalPlayerIndex, tile.X, tile.Y));
			return;
		}

		if (key.Keycode == Key.T)
		{
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			_commandMarker.Set("TradePost", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.Gold);
			QueueCommandAndConfirm(
				"place trade post p=" + LocalPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ")",
				facade => facade.QueuePlaceTradePost(LocalPlayerIndex, tile.X, tile.Y));
			return;
		}

		if (key.Keycode == Key.R)
		{
			TryCreateTradeRoute(GetGlobalMousePosition());
			return;
		}

		if (key.Keycode == Key.V)
		{
			TrainFromSelectedBuilding(VillagerUnitTypeId);
			return;
		}

		if (key.Keycode == Key.I)
		{
			TrainFromSelectedBuilding(InfantryUnitTypeId);
			return;
		}

		if (key.Keycode == Key.K)
		{
			TrainFromSelectedBuilding(TradeCartUnitTypeId);
			return;
		}

		if (key.Keycode == Key.Y)
		{
			ResearchFromSelectedBuilding(InfantryAttackTechId);
		}
	}

	private void StartLocalMatch(int playerCount)
	{
		_facade = RtsSessionBootstrap.StartLocalMatch(DefaultMatchSeed, playerCount);
		ResetLocalRuntimeState();
	}

	private const string DryArabiaMapName = "DryArabiaTest01";

	private void StartDryArabiaTest01()
	{
		_facade = RtsSessionBootstrap.StartDryArabiaTest01(DefaultMatchSeed);
		ResetLocalRuntimeState();
	}

	private void ResetLocalRuntimeState()
	{
		GodotClientFacade? facade = _facade;
		if (facade == null)
		{
			return;
		}

		RtsSessionBootstrap.ResetRuntimeState(
			facade,
			_selectionController,
			_tradeRouteSelection,
			_tcPlacementState,
			_cameraController,
			RefreshFrame,
			ref _hoveredResourceNodeId,
			ref _tickAccumulator,
			ref _paused);
	}

	private void HandleMouse(InputEventMouseButton mouse)
	{
		GodotFrameDto? frame = _frame;
		if (_facade == null || frame == null)
		{
			return;
		}

		Vector2 mouseWorldPosition = GetGlobalMousePosition();
		Vector2I tile = ScreenToTile(mouseWorldPosition);
		long mouseXRaw = ScreenToRaw(mouseWorldPosition.X);
		long mouseYRaw = ScreenToRaw(mouseWorldPosition.Y);

		// --- Placement mode intercept ---
		if (_tcPlacementState.IsActive)
		{
			if (mouse.ButtonIndex == MouseButton.Left)
			{
				ConfirmTcPlacement(tile);
			}
			else if (mouse.ButtonIndex == MouseButton.Right)
			{
				_tcPlacementState.Cancel();
				_debugEventLog.Add("TC placement cancelled (RMB)");
				QueueRedraw();
			}
			return;
		}
		// ---------------------------------

		if (mouse.ButtonIndex == MouseButton.Left)
		{
			_tradeRouteSelection.Clear();
			_selectionController.SelectAt(
				frame,
				LocalPlayerIndex,
				mouseWorldPosition,
				ScreenToRaw,
				RtsHoverStateResolver.FindResourceAt(_frame, mouseWorldPosition, (int)TilePixels),
				_hoveredResourceNodeId,
				_debugEventLog.Add);
			RefreshFrame();
			return;
		}

		if (mouse.ButtonIndex == MouseButton.Right && _selectionController.HasSelectedUnits)
		{
			int[] selectedUnitIds = _selectionController.GetSelectedUnitIdsSorted();
			GodotInteractionProbeResult probe = GodotInteractionProbe.Probe(frame, LocalPlayerIndex, mouseXRaw, mouseYRaw);
			GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(
				frame,
				LocalPlayerIndex,
				_selectionController.HasSelectedUnits,
				mouseXRaw,
				mouseYRaw);
			_debugEventLog.Add(
				"rclick raw=(" + mouseXRaw + "," + mouseYRaw + ") tile=(" + tile.X + "," + tile.Y + ") target="
				+ probe.TargetKind + ":" + probe.TargetEntityId + " route=" + intent.Kind);

			if (intent.Kind == GodotInteractionIntentKind.Attack)
			{
				GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, intent.TargetEntityId);
				if (target != null)
				{
					_commandMarker.Set("Attack", ToScreen(target.XRaw, target.YRaw), Colors.IndianRed);
				}

				QueueCommandAndConfirm(
					"attack p=" + LocalPlayerIndex + " targetEntity=" + intent.TargetEntityId + " units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueAttack(LocalPlayerIndex, selectedUnitIds, intent.TargetEntityId));
			}
			else if (intent.Kind == GodotInteractionIntentKind.AssignBuild)
			{
				GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, intent.TargetEntityId);
				if (target != null)
				{
					_commandMarker.Set("Build", ToScreen(target.XRaw, target.YRaw), Colors.Khaki);
				}

				QueueCommandAndConfirm(
					"assign build p=" + LocalPlayerIndex + " targetBuilding=" + intent.TargetEntityId + " units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueAssignBuild(LocalPlayerIndex, intent.TargetEntityId, selectedUnitIds));
			}
			else if (intent.Kind == GodotInteractionIntentKind.GatherResource)
			{
				GodotPrimitiveDto? target = RtsFrameLookup.FindPrimitiveByEntityId(frame, intent.ResourceNodeId);
				if (target != null)
				{
					_commandMarker.Set("Gather", ToScreen(target.XRaw, target.YRaw), Colors.ForestGreen);
				}

				QueueCommandAndConfirm(
					"gather p=" + LocalPlayerIndex + " resource=" + intent.ResourceNodeId + " units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueGatherResource(LocalPlayerIndex, intent.ResourceNodeId, selectedUnitIds));
			}
			else if (intent.Kind == GodotInteractionIntentKind.Move)
			{
				_commandMarker.Set("Move", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.LightSkyBlue);
				QueueCommandAndConfirm(
					"move p=" + LocalPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ") units=[" + string.Join(",", selectedUnitIds) + "]",
					f => f.QueueMoveUnits(LocalPlayerIndex, selectedUnitIds, tile.X, tile.Y));
			}
		}
	}

	private void TrainFromSelectedBuilding(int unitTypeId)
	{
		if (_frame == null)
		{
			return;
		}

		if (!RtsBuildingActionGuard.CanTrainFromSelectedBuilding(_frame, _selectionController.SelectedBuildingId, unitTypeId, out GodotTrainActionState state))
		{
			if (_selectionController.SelectedBuildingId == 0)
			{
				_debugEventLog.Add("train blocked reason=building not selected unit=" + GodotBuildingDebugStatusBuilder.ResolveUnitTypeLabel(unitTypeId));
				return;
			}

			_debugEventLog.Add(
				"train blocked reason=" + GodotBuildingDebugStatusBuilder.ResolveTrainBlockedReason(state)
				+ " building=" + _selectionController.SelectedBuildingId
				+ " unit=" + GodotBuildingDebugStatusBuilder.ResolveUnitTypeLabel(unitTypeId));
			RefreshFrame();
			return;
		}

		QueueCommandAndConfirm(
			"train p=" + LocalPlayerIndex + " " + GodotBuildingDebugStatusBuilder.BuildTrainIntentText(_selectionController.SelectedBuildingId, unitTypeId),
			facade => facade.QueueTrainUnit(LocalPlayerIndex, _selectionController.SelectedBuildingId, unitTypeId));
	}

	private void ResearchFromSelectedBuilding(int techId)
	{
		if (_frame == null)
		{
			return;
		}

		if (!RtsBuildingActionGuard.CanResearchInfantryAttackFromSelectedBuilding(_frame, _selectionController.SelectedBuildingId, out GodotResearchActionState state))
		{
			if (_selectionController.SelectedBuildingId == 0)
			{
				return;
			}

			_debugEventLog.Add("research blocked state=" + state + " building=" + _selectionController.SelectedBuildingId + " tech=" + techId);
			RefreshFrame();
			return;
		}

		QueueCommandAndConfirm(
			"research p=" + LocalPlayerIndex + " building=" + _selectionController.SelectedBuildingId + " tech=" + techId,
			facade => facade.QueueResearchTech(LocalPlayerIndex, _selectionController.SelectedBuildingId, techId));
	}

	private void TryCreateTradeRoute(Vector2 screenPosition)
	{
		if (_frame == null || !_selectionController.HasSelectedUnits)
		{
			return;
		}

		int tradeCartId = GodotTradeRouteRouter.FindSelectedTradeCart(_frame, _selectionController.GetSelectedUnitIdsSorted());
		if (tradeCartId == 0)
		{
			_debugEventLog.Add("trade route blocked: no selected trade cart");
			return;
		}

		int tradePostId = GodotTradeRouteRouter.FindLocalTradePostAt(
			_frame,
			LocalPlayerIndex,
			ScreenToRaw(screenPosition.X),
			ScreenToRaw(screenPosition.Y));

		if (tradePostId == 0)
		{
			_debugEventLog.Add("trade route blocked: no local trade post under cursor");
			return;
		}

		if (_tradeRouteSelection.TrySetFirstEndpoint(tradePostId))
		{
			_debugEventLog.Add("trade route step A set to tradePost=" + tradePostId);
			RefreshFrame();
			return;
		}

		if (!_tradeRouteSelection.TryConsumeRoute(tradePostId, out int routeA))
		{
			RefreshFrame();
			return;
		}

		QueueCommandAndConfirm(
			"trade route p=" + LocalPlayerIndex + " cart=" + tradeCartId + " A=" + routeA + " B=" + tradePostId,
			facade => facade.QueueCreateTradeRoute(LocalPlayerIndex, tradeCartId, routeA, tradePostId));
		_commandMarker.Set("TradeRoute", screenPosition, Colors.Gold);
	}

	private void FinishSelectionDrag()
	{
		if (_frame == null)
		{
			return;
		}

		bool handled = _selectionController.CompleteDragAndSelect(
			GetGlobalMousePosition(),
			_frame,
			LocalPlayerIndex,
			ScreenToRaw,
			RtsHoverStateResolver.FindResourceAt(_frame, GetGlobalMousePosition(), (int)TilePixels),
			_hoveredResourceNodeId,
			_debugEventLog.Add);
		if (handled)
		{
			_tradeRouteSelection.Clear();
			RefreshFrame();
		}
	}

	private void DrawPrimitive(GodotPrimitiveDto primitive)
	{
		RtsPrimitiveRenderer.Draw(
			this,
			primitive,
			_frame,
			_spriteRenderer,
			LocalPlayerIndex,
			_selectionController.SelectedUnitIds,
			_selectionController.SelectedBuildingId,
			_hoveredBuildingId,
			_hoveredResourceNodeId,
			ToScreen,
			RawToPixels,
			PrimitiveRect,
			DrawConstructionOverlayIfNeeded);
	}

	private void DrawHud()
	{
		if (_frame == null)
		{
			return;
		}

		Vector2 uiOrigin = GetUiOrigin();

		if (_screenshotMode)
		{
			RtsHudTopBarRenderer.DrawMinimal(this, uiOrigin, _frame, _spriteRenderer);
			return;
		}

		string[] lines = GodotHudTextBuilder.BuildLines(
			_frame,
			_selectionController.GetSelectedUnitIdsSorted(),
			_selectionController.SelectedBuildingId,
			_hoveredResourceNodeId,
			_paused);

		RtsHudTopBarRenderer.DrawMain(this, uiOrigin, lines, _spriteRenderer);

		_commandMarker.Draw(this);

		Vector2 uiSize = GetUiSize();
		if (_showDebugOverlay)
		{
			DrawDebugOverlay(uiOrigin, uiSize);
		}

		if (_showHotkeyHelp)
		{
			GodotHotkeyHelpEntry[] entries = GodotHotkeyHelpBuilder.Build(researchIsWired: true);
			RtsHotkeyHelpPanelRenderer.Draw(this, uiOrigin, uiSize, entries);
		}
	}

	private void DrawDebugOverlay(Vector2 uiOrigin, Vector2 uiSize)
	{
		string[] statusLines = GodotSelectedStatusBuilder.BuildLines(_frame, _selectionController.SelectedUnitIds, _selectionController.SelectedBuildingId, _hoveredResourceNodeId);
		string[] buildingLines = GodotBuildingDebugStatusBuilder.BuildLines(_frame, _selectionController.SelectedBuildingId);
		RtsHudPanelRenderer.DrawSelectedStatusPanel(this, uiOrigin, uiSize, statusLines);
		RtsHudPanelRenderer.DrawBuildingStatusPanel(this, uiOrigin, buildingLines);
		RtsDebugOverlayRenderer.Draw(this, uiOrigin, uiSize, _camera, _debugEventLog.GetLines());
	}

	private void DrawSpatialBlockersOverlay()
	{
		if (_frame == null || _facade == null)
		{
			return;
		}

		RtsSpatialBlockerOverlayRenderer.Draw(
			this,
			_frame,
			_facade.MapWidthTiles,
			_facade.MapHeightTiles,
			TilePixels,
			TileToRaw);
	}

	private Vector2 GetUiOrigin()
	{
		return RtsUiViewportMetrics.GetUiOrigin(_camera, GetViewportRect());
	}

	private Vector2 GetUiSize()
	{
		return RtsUiViewportMetrics.GetUiSize(_camera, GetViewportRect());
	}

	private void QueueCommandAndConfirm(string intentDescription, Action<GodotClientFacade> queueAction)
	{
		GodotClientFacade? facade = _facade;
		if (facade == null)
		{
			return;
		}

		int beforeExecuted = _frame?.Match.ExecutedCommandCount ?? facade.ExecutedCommandCount;
		int beforeRejected = _frame?.Match.RejectedCommandCount ?? facade.RejectedCommandCount;
		_debugEventLog.Add("intent " + intentDescription);

		queueAction(facade);
		facade.AdvanceOneTick();
		RefreshFrame();

		int afterExecuted = _frame?.Match.ExecutedCommandCount ?? facade.ExecutedCommandCount;
		int afterRejected = _frame?.Match.RejectedCommandCount ?? facade.RejectedCommandCount;
		GodotCommandResultKind result = GodotCommandResultClassifier.Classify(beforeExecuted, beforeRejected, afterExecuted, afterRejected);
		_debugEventLog.Add(
			RtsCommandResultLogFormatter.BuildResultLogLine(
				result,
				beforeExecuted,
				afterExecuted,
				beforeRejected,
				afterRejected,
				_frame));
	}

	private Rect2 PrimitiveRect(GodotPrimitiveDto primitive)
	{
		return RtsPrimitiveRectBuilder.Build(primitive, RawToPixels, ToScreen);
	}

	private static Vector2 ToScreen(long xRaw, long yRaw)
	{
		return RtsCoordinateTransform.ToScreen(xRaw, yRaw, TilePixels);
	}

	private static float RawToPixels(long raw)
	{
		return RtsCoordinateTransform.RawToPixels(raw, TilePixels);
	}

	private static long ScreenToRaw(float screenCoordinate)
	{
		return RtsCoordinateTransform.ScreenToRaw(screenCoordinate, TilePixels);
	}

	private static long TileToRaw(int tileCoordinate)
	{
		return RtsCoordinateTransform.TileToRaw(tileCoordinate, TilePixels);
	}

	private static Vector2I ScreenToTile(Vector2 screenPosition)
	{
		return RtsCoordinateTransform.ScreenToTile(screenPosition, TilePixels);
	}

	private void RefreshFrame()
	{
		GodotFrameDto? previous = _frame;
		_frame = _facade!.GetFrame(LocalPlayerIndex);
		if (RtsDepositEventTracker.TryBuildDepositEvent(previous, _frame, out string depositEvent))
		{
			_debugEventLog.Add(depositEvent);
		}
		Vector2 mouse = GetGlobalMousePosition();
		_hoveredResourceNodeId = RtsHoverStateResolver.FindResourceAt(_frame, mouse, (int)TilePixels);
		_hoveredBuildingId = RtsHoverStateResolver.FindHoveredBuildingAt(_frame, LocalPlayerIndex, mouse, (int)TilePixels);
		QueueRedraw();
	}

	private void RefreshHoveredTargetsFromMouse()
	{
		if (_frame == null)
		{
			return;
		}

		Vector2 mouse = GetGlobalMousePosition();
		int hoveredResource = RtsHoverStateResolver.FindResourceAt(_frame, mouse, (int)TilePixels);
		int hoveredBuilding = RtsHoverStateResolver.FindHoveredBuildingAt(_frame, LocalPlayerIndex, mouse, (int)TilePixels);
		if (hoveredResource != _hoveredResourceNodeId || hoveredBuilding != _hoveredBuildingId)
		{
			_hoveredResourceNodeId = hoveredResource;
			_hoveredBuildingId = hoveredBuilding;
			QueueRedraw();
		}
	}

	private void DrawConstructionOverlayIfNeeded(GodotPrimitiveDto primitive)
	{
		RtsConstructionOverlayBridge.DrawIfNeeded(this, _frame, primitive, ToScreen);
	}

	private void ConfirmTcPlacement(Vector2I tile)
	{
		if (_facade == null || _frame == null)
		{
			return;
		}

		_tcPlacementState.Cancel();
		GodotFrameDto frameBefore = _frame;
		TcPlacementPreviewResult previewBefore = _tcPlacementState.PreviewResult;

		if (_tcPlacementState.PreviewResult != TcPlacementPreviewResult.Valid)
		{
			_debugEventLog.Add("TC placement rejected by preview: " + _tcPlacementState.PreviewResult);
			QueueRedraw();
			return;
		}

		_commandMarker.Set("TC", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.LightBlue);
		int beforeExecuted = frameBefore.Match.ExecutedCommandCount;
		int beforeRejected = frameBefore.Match.RejectedCommandCount;
		QueueCommandAndConfirm(
			"place town center p=" + LocalPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ")",
			facade => facade.QueuePlaceTownCenter(LocalPlayerIndex, tile.X, tile.Y));

		int afterExecuted = _frame?.Match.ExecutedCommandCount ?? _facade.ExecutedCommandCount;
		int afterRejected = _frame?.Match.RejectedCommandCount ?? _facade.RejectedCommandCount;
		GodotCommandResultKind result = GodotCommandResultClassifier.Classify(beforeExecuted, beforeRejected, afterExecuted, afterRejected);
		if (previewBefore == TcPlacementPreviewResult.Valid && result == GodotCommandResultKind.Rejected)
		{
			LogTcPreviewCommandMismatch(tile, previewBefore, frameBefore);
		}
	}

	private void LogTcPreviewCommandMismatch(Vector2I tile, TcPlacementPreviewResult previewResult, GodotFrameDto frameBefore)
	{
		_debugEventLog.Add(
			RtsTownCenterPlacementDiagnostics.BuildPreviewCommandMismatchMessage(
				tile,
				previewResult,
				frameBefore.LocalPlayer,
				LocalPlayerIndex));
	}

}
