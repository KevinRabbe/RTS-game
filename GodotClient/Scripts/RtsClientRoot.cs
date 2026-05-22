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
		RtsInputKeyRouter.HandleKey(
			key,
			_frame,
			LocalPlayerIndex,
			DryArabiaMapName,
			GetGlobalMousePosition,
			ScreenToTile,
			TileToRaw,
			ToScreen,
			_spriteRenderer,
			_tcPlacementState,
			StartDryArabiaTest01,
			StartLocalMatch,
			TryCreateTradeRoute,
			TrainFromSelectedBuilding,
			ResearchFromSelectedBuilding,
			QueueCommandAndConfirm,
			RefreshFrame,
			QueueRedraw,
			_debugEventLog.Add,
			_commandMarker,
			ref _screenshotMode,
			ref _paused,
			ref _showDebugOverlay,
			ref _showHotkeyHelp,
			VillagerUnitTypeId,
			InfantryUnitTypeId,
			TradeCartUnitTypeId,
			InfantryAttackTechId,
			LocalPlayerIndex);
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
		Vector2 mouseWorldPosition = GetGlobalMousePosition();
		Vector2I tile = ScreenToTile(mouseWorldPosition);
		long mouseXRaw = ScreenToRaw(mouseWorldPosition.X);
		long mouseYRaw = ScreenToRaw(mouseWorldPosition.Y);
		RtsInputMouseRouter.HandleMouse(
			mouse,
			_facade,
			_frame,
			LocalPlayerIndex,
			mouseWorldPosition,
			tile,
			mouseXRaw,
			mouseYRaw,
			_hoveredResourceNodeId,
			_tcPlacementState,
			_selectionController,
			_tradeRouteSelection,
			RtsHoverStateResolver.FindResourceAt,
			RefreshFrame,
			QueueRedraw,
			_debugEventLog.Add,
			ConfirmTcPlacement,
			QueueCommandAndConfirm,
			ToScreen,
			TileToRaw,
			_commandMarker.Set);
	}

	private void TrainFromSelectedBuilding(int unitTypeId)
	{
		RtsBuildingActionRouter.TrainFromSelectedBuilding(
			_frame,
			LocalPlayerIndex,
			_selectionController.SelectedBuildingId,
			unitTypeId,
			_debugEventLog.Add,
			RefreshFrame,
			QueueCommandAndConfirm);
	}

	private void ResearchFromSelectedBuilding(int techId)
	{
		RtsBuildingActionRouter.ResearchFromSelectedBuilding(
			_frame,
			LocalPlayerIndex,
			_selectionController.SelectedBuildingId,
			techId,
			_debugEventLog.Add,
			RefreshFrame,
			QueueCommandAndConfirm);
	}

	private void TryCreateTradeRoute(Vector2 screenPosition)
	{
		RtsBuildingActionRouter.TryCreateTradeRoute(
			_frame,
			LocalPlayerIndex,
			_selectionController.HasSelectedUnits,
			_selectionController.GetSelectedUnitIdsSorted(),
			_tradeRouteSelection,
			screenPosition,
			ScreenToRaw,
			ToScreen,
			TileToRaw,
			ScreenToTile,
			_debugEventLog.Add,
			RefreshFrame,
			QueueCommandAndConfirm,
			_commandMarker);
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
		RtsTownCenterPlacementActionService.ConfirmPlacement(
			_facade,
			_frame,
			_tcPlacementState,
			LocalPlayerIndex,
			tile,
			TileToRaw,
			ToScreen,
			_debugEventLog.Add,
			QueueRedraw,
			QueueCommandAndConfirm,
			_commandMarker,
			() => _frame?.Match.ExecutedCommandCount ?? _facade?.ExecutedCommandCount ?? 0,
			() => _frame?.Match.RejectedCommandCount ?? _facade?.RejectedCommandCount ?? 0);
	}

}
