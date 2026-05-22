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
			DrawTcPlacementGhost();
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
		_facade = GodotClientFacade.CreateLocal(DefaultMatchSeed, playerCount);
		ResetLocalRuntimeState();
	}

	private const string DryArabiaMapName = "DryArabiaTest01";

	private void StartDryArabiaTest01()
	{
		_facade = GodotClientFacade.CreateDryArabiaTest01(DefaultMatchSeed);
		ResetLocalRuntimeState();
	}

	private void ResetLocalRuntimeState()
	{
		GodotClientFacade? facade = _facade;
		if (facade == null)
		{
			return;
		}

		_selectionController.Reset();
		_tradeRouteSelection.Clear();
		_hoveredResourceNodeId = 0;
		_tcPlacementState.Reset();
		_cameraController.EndMiddleDrag();
		_tickAccumulator = 0.0;
		_paused = false;
		facade.AdvanceOneTick();
		RefreshFrame();
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
				FindResourceAt(mouseWorldPosition),
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
			FindResourceAt(GetGlobalMousePosition()),
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
		switch (GodotPrimitiveDrawKindResolver.Resolve(primitive))
		{
			case GodotPrimitiveDrawKind.Unit:
				DrawUnit(primitive);
				break;
			case GodotPrimitiveDrawKind.Building:
				DrawBuilding(primitive);
				break;
			case GodotPrimitiveDrawKind.TradeRoute:
				DrawTradeRoute(primitive);
				break;
			case GodotPrimitiveDrawKind.HealthBar:
				DrawHealthBar(primitive);
				break;
			case GodotPrimitiveDrawKind.FogOverlay:
				DrawFogOverlay();
				break;
			case GodotPrimitiveDrawKind.Resource:
				DrawResource(primitive);
				break;
		}
	}

	private void DrawUnit(GodotPrimitiveDto primitive)
	{
		bool isSelected = _selectionController.IsUnitSelected(primitive.EntityId);
		if (_spriteRenderer.TryDrawUnit(this, primitive, _frame, _selectionController.SelectedUnitIds, ToScreen, RawToPixels))
		{
			if (isSelected)
			{
				DrawSelectionRing(primitive, Colors.Aqua);
			}

			return;
		}

		Rect2 rect = PrimitiveRect(primitive);
		Color color = RtsVisualStyleColors.Resolve(GodotVisualStyleResolver.ResolveUnit(primitive, LocalPlayerIndex));
		DrawRect(rect, color);
		if (isSelected)
		{
			DrawSelectionRing(primitive, Colors.Aqua);
			DrawRect(rect.Grow(2.0f), Colors.White, false, 2.0f);
		}
	}

	private void DrawBuilding(GodotPrimitiveDto primitive)
	{
		bool isSelected = _selectionController.SelectedBuildingId == primitive.EntityId;
		bool isHovered = _hoveredBuildingId == primitive.EntityId;
		if (_spriteRenderer.TryDrawBuilding(this, primitive, _frame, _selectionController.SelectedBuildingId, ToScreen, RawToPixels))
		{
			if (isSelected)
			{
				DrawBuildingFootprintOutline(primitive, Colors.Gold);
			}
			else if (isHovered)
			{
				DrawBuildingFootprintOutline(primitive, Colors.Khaki);
			}

			DrawConstructionOverlayIfNeeded(primitive);

			return;
		}

		Rect2 rect = PrimitiveRect(primitive);
		Color color = RtsVisualStyleColors.Resolve(GodotVisualStyleResolver.ResolveBuilding(primitive));
		DrawRect(rect, color);
		if (isSelected)
		{
			DrawBuildingFootprintOutline(primitive, Colors.Gold);
		}
		else if (isHovered)
		{
			DrawBuildingFootprintOutline(primitive, Colors.Khaki);
		}

		DrawConstructionOverlayIfNeeded(primitive);
	}

	private void DrawResource(GodotPrimitiveDto primitive)
	{
		if (_spriteRenderer.TryDrawResource(this, primitive, ToScreen, RawToPixels))
		{
			if (primitive.EntityId == _hoveredResourceNodeId)
			{
				Rect2 spriteRect = PrimitiveRect(primitive).Grow(6.0f);
				DrawArc(spriteRect.GetCenter(), spriteRect.Size.X * 0.65f, 0.0f, Mathf.Tau, 32, Colors.White, 2.0f);
			}

			return;
		}

		Rect2 rect = PrimitiveRect(primitive);
		Color color = RtsVisualStyleColors.Resolve(GodotVisualStyleResolver.ResolveResource(primitive));
		DrawCircle(rect.GetCenter(), rect.Size.X * 0.5f, color);
		if (primitive.EntityId == _hoveredResourceNodeId)
		{
			DrawArc(rect.GetCenter(), rect.Size.X * 0.65f, 0.0f, Mathf.Tau, 32, Colors.White, 2.0f);
		}
	}

	private void DrawTradeRoute(GodotPrimitiveDto primitive)
	{
		RtsTradeRouteRenderer.Draw(this, ToScreen(primitive.XRaw, primitive.YRaw), ToScreen(primitive.EndXRaw, primitive.EndYRaw));
	}

	private void DrawHealthBar(GodotPrimitiveDto primitive)
	{
		RtsHealthBarRenderer.Draw(this, ToScreen(primitive.XRaw, primitive.YRaw), primitive);
	}

	private void DrawFogOverlay()
	{
		DrawRect(new Rect2(Vector2.Zero, new Vector2(2048.0f, 1536.0f)), new Color(0.02f, 0.02f, 0.02f, 0.12f));
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
			DrawMinimalHud(uiOrigin);
			return;
		}

		string[] lines = GodotHudTextBuilder.BuildLines(
			_frame,
			_selectionController.GetSelectedUnitIdsSorted(),
			_selectionController.SelectedBuildingId,
			_hoveredResourceNodeId,
			_paused);

		float hudHeight = 64.0f;
		if (_spriteRenderer.LoadedAssetCount < _spriteRenderer.ExpectedAssetCount)
		{
			hudHeight = 82.0f;
		}

		DrawRect(new Rect2(uiOrigin, new Vector2(1120.0f, hudHeight)), new Color(0.0f, 0.0f, 0.0f, 0.50f));
		for (int i = 0; i < lines.Length; i++)
		{
			DrawString(ThemeDB.FallbackFont, uiOrigin + new Vector2(12.0f, 20.0f + i * 18.0f), lines[i], HorizontalAlignment.Left, -1.0f, 16, Colors.White);
		}

		DrawString(
			ThemeDB.FallbackFont,
			uiOrigin + new Vector2(12.0f, 56.0f),
			"Render " + _spriteRenderer.RenderModeLabel + " (F9)  Assets " + _spriteRenderer.LoadedAssetCount + "/" + _spriteRenderer.ExpectedAssetCount,
			HorizontalAlignment.Left,
			-1.0f,
			16,
			Colors.White);
		if (_spriteRenderer.LoadedAssetCount < _spriteRenderer.ExpectedAssetCount)
		{
			DrawString(
				ThemeDB.FallbackFont,
				uiOrigin + new Vector2(12.0f, 74.0f),
				"Missing: " + _spriteRenderer.MissingAssetsLabel,
				HorizontalAlignment.Left,
				-1.0f,
				14,
				Colors.LightGray);
		}

		_commandMarker.Draw(this);

		Vector2 uiSize = GetUiSize();
		if (_showDebugOverlay)
		{
			DrawDebugOverlay(uiOrigin, uiSize);
		}

		if (_showHotkeyHelp)
		{
			DrawHotkeyHelpPanel(uiOrigin, uiSize);
		}
	}

	private void DrawMinimalHud(Vector2 uiOrigin)
	{
		GodotLocalPlayerDto player = _frame!.LocalPlayer;
		string line = "Tick " + _frame.Tick
			+ "  Map " + _frame.MapName
			+ "  Food " + player.Food
			+ "  Wood " + player.Wood
			+ "  Gold " + player.Gold
			+ "  Pop " + player.PopulationUsed + "/" + player.PopulationCap
			+ "  " + _spriteRenderer.RenderModeLabel;

		DrawRect(new Rect2(uiOrigin, new Vector2(1120.0f, 32.0f)), new Color(0.0f, 0.0f, 0.0f, 0.50f));
		DrawString(ThemeDB.FallbackFont, uiOrigin + new Vector2(12.0f, 22.0f), line, HorizontalAlignment.Left, -1.0f, 16, Colors.White);
	}

	private void DrawDebugOverlay(Vector2 uiOrigin, Vector2 uiSize)
	{
		string[] statusLines = GodotSelectedStatusBuilder.BuildLines(_frame, _selectionController.SelectedUnitIds, _selectionController.SelectedBuildingId, _hoveredResourceNodeId);
		string[] buildingLines = GodotBuildingDebugStatusBuilder.BuildLines(_frame, _selectionController.SelectedBuildingId);
		DrawSelectedStatusPanel(uiOrigin, uiSize, statusLines);
		DrawBuildingStatusPanel(uiOrigin, buildingLines);

		float panelWidth = 420.0f;
		float panelHeight = 168.0f;
		Vector2 panelPos = uiOrigin + new Vector2(Mathf.Max(0.0f, uiSize.X - panelWidth - 12.0f), 88.0f);
		DrawRect(new Rect2(panelPos, new Vector2(panelWidth, panelHeight)), new Color(0.0f, 0.0f, 0.0f, 0.52f));
		
		string debugTitle = _camera != null 
			? $"Debug Overlay (F10) - Cam: {_camera.Position.X:F0},{_camera.Position.Y:F0}" 
			: "Debug Overlay (F10)";
		DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 20.0f), debugTitle, HorizontalAlignment.Left, -1.0f, 15, Colors.WhiteSmoke);

		string[] events = _debugEventLog.GetLines();
		DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 40.0f), "Events", HorizontalAlignment.Left, -1.0f, 14, Colors.WhiteSmoke);
		int maxEvents = Mathf.Min(events.Length, 8);
		for (int i = 0; i < maxEvents; i++)
		{
			int eventIndex = events.Length - maxEvents + i;
			DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 58.0f + i * 13.0f), events[eventIndex], HorizontalAlignment.Left, -1.0f, 12, Colors.LightGray);
		}
	}

	private void DrawSpatialBlockersOverlay()
	{
		if (_frame == null || _facade == null)
		{
			return;
		}

		Color blockedColor = new Color(0.95f, 0.2f, 0.2f, 0.18f);
		float tileSize = TilePixels;
		int width = _facade.MapWidthTiles;
		int height = _facade.MapHeightTiles;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (!IsTileBlockedByVisibleSimEntity(x, y))
				{
					continue;
				}

				DrawRect(new Rect2(x * tileSize, y * tileSize, tileSize, tileSize), blockedColor);
			}
		}
	}

	private bool IsTileBlockedByVisibleSimEntity(int tileX, int tileY)
	{
		if (_frame == null)
		{
			return false;
		}

		long tileXRaw = TileToRaw(tileX);
		long tileYRaw = TileToRaw(tileY);
		for (int i = 0; i < _frame.Primitives.Length; i++)
		{
			GodotPrimitiveDto primitive = _frame.Primitives[i];
			GodotPrimitiveDrawKind kind = GodotPrimitiveDrawKindResolver.Resolve(primitive);
			if (kind == GodotPrimitiveDrawKind.Building || kind == GodotPrimitiveDrawKind.Resource)
			{
				if (IsWithinPrimitiveBounds(tileXRaw, tileYRaw, primitive))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool IsWithinPrimitiveBounds(long tileXRaw, long tileYRaw, GodotPrimitiveDto primitive)
	{
		long halfWidthRaw = primitive.WidthRaw / 2;
		long halfHeightRaw = primitive.HeightRaw / 2;
		return tileXRaw >= primitive.XRaw - halfWidthRaw
			&& tileXRaw <= primitive.XRaw + halfWidthRaw
			&& tileYRaw >= primitive.YRaw - halfHeightRaw
			&& tileYRaw <= primitive.YRaw + halfHeightRaw;
	}

	private void DrawHotkeyHelpPanel(Vector2 uiOrigin, Vector2 uiSize)
	{
		GodotHotkeyHelpEntry[] entries = GodotHotkeyHelpBuilder.Build(researchIsWired: true);
		Vector2 panelPos = uiOrigin + new Vector2(Mathf.Max(0.0f, uiSize.X - 620.0f - 12.0f), 264.0f);
		float panelWidth = 620.0f;
		float panelHeight = 24.0f + entries.Length * 16.0f + 12.0f;
		DrawRect(new Rect2(panelPos, new Vector2(panelWidth, panelHeight)), new Color(0.0f, 0.0f, 0.0f, 0.56f));
		DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 20.0f), "Hotkeys (H/F11)", HorizontalAlignment.Left, -1.0f, 15, Colors.WhiteSmoke);
		for (int i = 0; i < entries.Length; i++)
		{
			string line = entries[i].Input + ": " + entries[i].Action;
			DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 38.0f + i * 16.0f), line, HorizontalAlignment.Left, -1.0f, 13, Colors.LightGray);
		}
	}

	private void DrawSelectedStatusPanel(Vector2 uiOrigin, Vector2 uiSize, string[] statusLines)
	{
		float panelWidth = Mathf.Min(760.0f, uiSize.X - 24.0f);
		Vector2 panelPos = uiOrigin + new Vector2(0.0f, Mathf.Max(96.0f, uiSize.Y - 66.0f));
		DrawRect(new Rect2(panelPos, new Vector2(panelWidth, 52.0f)), new Color(0.0f, 0.0f, 0.0f, 0.48f));
		for (int i = 0; i < statusLines.Length; i++)
		{
			DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 19.0f + i * 16.0f), statusLines[i], HorizontalAlignment.Left, -1.0f, 13, Colors.LightGray);
		}
	}

	private void DrawBuildingStatusPanel(Vector2 uiOrigin, string[] lines)
	{
		Vector2 panelPos = uiOrigin + new Vector2(0.0f, 76.0f);
		DrawRect(new Rect2(panelPos, new Vector2(860.0f, 34.0f)), new Color(0.0f, 0.0f, 0.0f, 0.46f));
		for (int i = 0; i < lines.Length && i < 2; i++)
		{
			DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(8.0f, 13.0f + i * 15.0f), lines[i], HorizontalAlignment.Left, -1.0f, 12, Colors.LightGray);
		}
	}

	private void DrawSelectionRing(GodotPrimitiveDto primitive, Color color)
	{
		Vector2 center = ToScreen(primitive.XRaw, primitive.YRaw);
		float radiusSource = Mathf.Max(RawToPixels(primitive.WidthRaw), RawToPixels(primitive.HeightRaw));
		float radius = Mathf.Max(10.0f, radiusSource * 0.74f);
		Vector2 ringCenter = center + new Vector2(0.0f, 4.0f);
		DrawArc(ringCenter, radius + 1.5f, 0.0f, Mathf.Tau, 36, Colors.Black, 3.0f);
		DrawArc(ringCenter, radius, 0.0f, Mathf.Tau, 36, color, 2.4f);
	}

	private void DrawBuildingFootprintOutline(GodotPrimitiveDto primitive, Color color)
	{
		Rect2 rect = PrimitiveRect(primitive).Grow(2.0f);
		DrawRect(rect, Colors.Black, false, 3.0f);
		DrawRect(rect, color, false, 2.0f);
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
		Vector2 center = ToScreen(primitive.XRaw, primitive.YRaw);
		float width = RawToPixels(primitive.WidthRaw);
		float height = RawToPixels(primitive.HeightRaw);
		return new Rect2(center.X - width * 0.5f, center.Y - height * 0.5f, width, height);
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
		_hoveredResourceNodeId = FindResourceAt(GetGlobalMousePosition());
		_hoveredBuildingId = FindHoveredBuildingAt(GetGlobalMousePosition());
		QueueRedraw();
	}

	private void RefreshHoveredTargetsFromMouse()
	{
		if (_frame == null)
		{
			return;
		}

		Vector2 mouse = GetGlobalMousePosition();
		int hoveredResource = FindResourceAt(mouse);
		int hoveredBuilding = FindHoveredBuildingAt(mouse);
		if (hoveredResource != _hoveredResourceNodeId || hoveredBuilding != _hoveredBuildingId)
		{
			_hoveredResourceNodeId = hoveredResource;
			_hoveredBuildingId = hoveredBuilding;
			QueueRedraw();
		}
	}

	private int FindResourceAt(Vector2 screenPosition)
	{
		if (_frame == null)
		{
			return 0;
		}

		return RtsHoverProbe.FindResourceAt(_frame, ScreenToRaw(screenPosition.X), ScreenToRaw(screenPosition.Y));
	}

	private int FindHoveredBuildingAt(Vector2 screenPosition)
	{
		if (_frame == null)
		{
			return 0;
		}

		return RtsHoverProbe.FindLocalBuildingAt(
			_frame,
			LocalPlayerIndex,
			ScreenToRaw(screenPosition.X),
			ScreenToRaw(screenPosition.Y));
	}

	private void DrawConstructionOverlayIfNeeded(GodotPrimitiveDto primitive)
	{
		if (_frame == null)
		{
			return;
		}

		GodotBuildingStatusDto? status = RtsFrameLookup.FindBuildingStatus(_frame, primitive.EntityId);
		if (status == null || !status.IsUnderConstruction)
		{
			return;
		}

		RtsConstructionOverlayRenderer.Draw(this, ToScreen(primitive.XRaw, primitive.YRaw), status);
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

	private void DrawTcPlacementGhost()
	{
		if (_frame == null)
		{
			return;
		}

		Vector2 center = ToScreen(TileToRaw(_tcPlacementState.HoveredTile.X), TileToRaw(_tcPlacementState.HoveredTile.Y));
		RtsTownCenterPlacementGhostRenderer.Draw(this, center, TilePixels, _tcPlacementState.PreviewResult);
	}
}
