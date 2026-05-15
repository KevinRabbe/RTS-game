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
	private const float CameraPanPixelsPerSecond = 420.0f;
	private const float EdgePanMarginPx = 32.0f;
	private const float EdgePanSpeedPixelsPerSecond = 380.0f;
	private const int VillagerUnitTypeId = 1;
	private const int InfantryUnitTypeId = 3;
	private const int TradeCartUnitTypeId = 5;
	private const int InfantryAttackTechId = 1;
	private const int TownCenterBuildingTypeId = 1;
	private const int WallBuildingTypeId = 2;
	private const int TradePostBuildingTypeId = 3;
	private const int TownCenterRadiusTiles = 2;
	private const int WallRadiusTiles = 1;
	private const int TradePostRadiusTiles = 2;
	private const int ResourceRadiusTiles = 1;

	private readonly List<int> _selectedUnitIds = new List<int>();
	private readonly GodotDebugEventLog _debugEventLog = new GodotDebugEventLog(10);
	private Camera2D? _camera;
	private GodotClientFacade? _facade;
	private GodotFrameDto? _frame;
	private readonly Phase6SpriteRenderer _spriteRenderer = new Phase6SpriteRenderer();
	private double _tickAccumulator;
	private bool _paused;
	private bool _showDebugOverlay = true;
	private bool _showHotkeyHelp;
	private bool _screenshotMode;
	private string _commandMarkerLabel = "";
	private Color _commandMarkerColor = Colors.White;
	private Vector2 _commandMarkerWorldPosition = Vector2.Zero;
	private int _commandMarkerTicksRemaining;
	private int _hoveredResourceNodeId;
	private int _hoveredBuildingId;
	private int _selectedBuildingId;
	private int _pendingTradeRouteAId;

	// TC placement mode
	private bool _tcPlacementMode;
	private Vector2I _tcHoveredTile;
	private TcPlacementPreviewResult _tcPreviewResult = TcPlacementPreviewResult.Unknown;

	// Middle-mouse drag pan
	private bool _middleMouseDragActive;
	private Vector2 _middleMouseDragStartScreen = Vector2.Zero;
	private Vector2 _middleMouseDragStartCamera = Vector2.Zero;

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
		if (_facade == null)
		{
			return;
		}

		UpdateCamera(delta);
		if (_commandMarkerTicksRemaining > 0)
		{
			_commandMarkerTicksRemaining--;
		}

		// Update TC placement ghost tile each frame (no command spam).
		if (_tcPlacementMode && _frame != null)
		{
			Vector2I newTile = ScreenToTile(GetGlobalMousePosition());
			if (newTile != _tcHoveredTile)
			{
				_tcHoveredTile = newTile;
				_tcPreviewResult = TcPlacementPreview.Evaluate(_frame, _tcHoveredTile.X, _tcHoveredTile.Y);
				QueueRedraw();
			}
		}

		// Middle-mouse drag camera pan.
		if (_middleMouseDragActive && _camera != null)
		{
			Vector2 currentScreen = GetViewport().GetMousePosition();
			Vector2 panDelta = currentScreen - _middleMouseDragStartScreen;
			_camera.Position = _middleMouseDragStartCamera - panDelta;
			QueueRedraw();
		}

		ClampCameraToBounds();

		if (_paused)
		{
			RefreshFrame();
			return;
		}

		_tickAccumulator += delta;
		while (_tickAccumulator >= TickSeconds)
		{
			_facade.AdvanceOneTick();
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
					_middleMouseDragActive = true;
					_middleMouseDragStartScreen = GetViewport().GetMousePosition();
					_middleMouseDragStartCamera = _camera?.Position ?? Vector2.Zero;
				}
				else
				{
					_middleMouseDragActive = false;
				}
				return;
			}

			if (mouse.Pressed)
			{
				HandleMouse(mouse);
			}
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

		if (_tcPlacementMode)
		{
			DrawTcPlacementGhost();
		}

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
			_tcPlacementMode = true;
			_tcHoveredTile = ScreenToTile(GetGlobalMousePosition());
			_tcPreviewResult = _frame != null
				? TcPlacementPreview.Evaluate(_frame, _tcHoveredTile.X, _tcHoveredTile.Y)
				: TcPlacementPreviewResult.Unknown;
			_debugEventLog.Add("TC placement mode entered");
			QueueRedraw();
			return;
		}

		if (key.Keycode == Key.Escape && _tcPlacementMode)
		{
			_tcPlacementMode = false;
			_debugEventLog.Add("TC placement cancelled (Esc)");
			QueueRedraw();
			return;
		}

		if (key.Keycode == Key.W)
		{
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			SetCommandMarker("Wall", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.LightGray);
			QueueCommandAndConfirm(
				"place wall p=" + LocalPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ")",
				facade => facade.QueuePlaceWall(LocalPlayerIndex, tile.X, tile.Y));
			return;
		}

		if (key.Keycode == Key.T)
		{
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			SetCommandMarker("TradePost", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.Gold);
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

		_selectedUnitIds.Clear();
		_selectedBuildingId = 0;
		_pendingTradeRouteAId = 0;
		_hoveredResourceNodeId = 0;
		_tcPlacementMode = false;
		_tcPreviewResult = TcPlacementPreviewResult.Unknown;
		_middleMouseDragActive = false;
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
		if (_tcPlacementMode)
		{
			if (mouse.ButtonIndex == MouseButton.Left)
			{
				ConfirmTcPlacement(tile);
			}
			else if (mouse.ButtonIndex == MouseButton.Right)
			{
				_tcPlacementMode = false;
				_debugEventLog.Add("TC placement cancelled (RMB)");
				QueueRedraw();
			}
			return;
		}
		// ---------------------------------

		if (mouse.ButtonIndex == MouseButton.Left)
		{
			SelectAt(mouseWorldPosition);
			RefreshFrame();
			return;
		}

		if (mouse.ButtonIndex == MouseButton.Right && _selectedUnitIds.Count > 0)
		{
			GodotInteractionProbeResult probe = GodotInteractionProbe.Probe(frame, LocalPlayerIndex, mouseXRaw, mouseYRaw);
			GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(
				frame,
				LocalPlayerIndex,
				_selectedUnitIds.Count > 0,
				mouseXRaw,
				mouseYRaw);
			_debugEventLog.Add(
				"rclick raw=(" + mouseXRaw + "," + mouseYRaw + ") tile=(" + tile.X + "," + tile.Y + ") target="
				+ probe.TargetKind + ":" + probe.TargetEntityId + " route=" + intent.Kind);

			if (intent.Kind == GodotInteractionIntentKind.Attack)
			{
				GodotPrimitiveDto? target = FindPrimitiveByEntityId(frame, intent.TargetEntityId);
				if (target != null)
				{
					SetCommandMarker("Attack", ToScreen(target.XRaw, target.YRaw), Colors.IndianRed);
				}

				QueueCommandAndConfirm(
					"attack p=" + LocalPlayerIndex + " targetEntity=" + intent.TargetEntityId,
					f => f.QueueAttack(LocalPlayerIndex, _selectedUnitIds.ToArray(), intent.TargetEntityId));
			}
			else if (intent.Kind == GodotInteractionIntentKind.AssignBuild)
			{
				GodotPrimitiveDto? target = FindPrimitiveByEntityId(frame, intent.TargetEntityId);
				if (target != null)
				{
					SetCommandMarker("Build", ToScreen(target.XRaw, target.YRaw), Colors.Khaki);
				}

				QueueCommandAndConfirm(
					"assign build p=" + LocalPlayerIndex + " targetBuilding=" + intent.TargetEntityId,
					f => f.QueueAssignBuild(LocalPlayerIndex, intent.TargetEntityId, _selectedUnitIds.ToArray()));
			}
			else if (intent.Kind == GodotInteractionIntentKind.GatherResource)
			{
				GodotPrimitiveDto? target = FindPrimitiveByEntityId(frame, intent.ResourceNodeId);
				if (target != null)
				{
					SetCommandMarker("Gather", ToScreen(target.XRaw, target.YRaw), Colors.ForestGreen);
				}

				QueueCommandAndConfirm(
					"gather p=" + LocalPlayerIndex + " resource=" + intent.ResourceNodeId,
					f => f.QueueGatherResource(LocalPlayerIndex, intent.ResourceNodeId, _selectedUnitIds.ToArray()));
			}
			else if (intent.Kind == GodotInteractionIntentKind.Move)
			{
				SetCommandMarker("Move", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.LightSkyBlue);
				QueueCommandAndConfirm(
					"move p=" + LocalPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ")",
					f => f.QueueMoveUnits(LocalPlayerIndex, _selectedUnitIds.ToArray(), tile.X, tile.Y));
			}
		}
	}

	private void ClampCameraToBounds()
	{
		if (_camera == null || _facade == null)
		{
			return;
		}

		// Use map dimensions as base playable area
		float mapWidthPx = _facade.MapWidthTiles * TilePixels;
		float mapHeightPx = _facade.MapHeightTiles * TilePixels;

		// Account for viewport size so the edges of the view stay within map bounds
		Rect2 viewportRect = GetViewportRect();
		Vector2 zoom = _camera.Zoom;
		float zoomX = Mathf.IsZeroApprox(zoom.X) ? 1.0f : zoom.X;
		float zoomY = Mathf.IsZeroApprox(zoom.Y) ? 1.0f : zoom.Y;

		// Visible world width/height = viewport pixels / zoom
		float viewWidth = viewportRect.Size.X / zoomX;
		float viewHeight = viewportRect.Size.Y / zoomY;

		float halfViewWidth = viewWidth * 0.5f;
		float halfViewHeight = viewHeight * 0.5f;

		float minX, maxX, minY, maxY;

		if (viewWidth < mapWidthPx)
		{
			minX = halfViewWidth;
			maxX = mapWidthPx - halfViewWidth;
		}
		else
		{
			// Map is smaller than viewport, center it
			minX = maxX = mapWidthPx * 0.5f;
		}

		if (viewHeight < mapHeightPx)
		{
			minY = halfViewHeight;
			maxY = mapHeightPx - halfViewHeight;
		}
		else
		{
			// Map is smaller than viewport, center it
			minY = maxY = mapHeightPx * 0.5f;
		}

		float clampedX = Mathf.Clamp(_camera.Position.X, minX, maxX);
		float clampedY = Mathf.Clamp(_camera.Position.Y, minY, maxY);

		_camera.Position = new Vector2(clampedX, clampedY);
	}

	private void UpdateCamera(double delta)
	{
		if (_camera == null)
		{
			return;
		}

		// Skip edge pan while middle-mouse drag is active (drag handles camera directly).
		if (_middleMouseDragActive)
		{
			return;
		}

		Vector2 direction = Vector2.Zero;

		// Arrow-key pan (preserved).
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

		// Mouse edge pan.
		Vector2 mousePos = GetViewport().GetMousePosition();
		Rect2 viewport = GetViewportRect();
		if (mousePos.X <= EdgePanMarginPx)
		{
			direction.X -= 1.0f;
		}
		else if (mousePos.X >= viewport.Size.X - EdgePanMarginPx)
		{
			direction.X += 1.0f;
		}

		if (mousePos.Y <= EdgePanMarginPx)
		{
			direction.Y -= 1.0f;
		}
		else if (mousePos.Y >= viewport.Size.Y - EdgePanMarginPx)
		{
			direction.Y += 1.0f;
		}

		if (direction == Vector2.Zero)
		{
			return;
		}

		_camera.Position += direction.Normalized() * EdgePanSpeedPixelsPerSecond * (float)delta;
	}

	private void TrainFromSelectedBuilding(int unitTypeId)
	{
		if (_selectedBuildingId == 0)
		{
			_debugEventLog.Add("train blocked reason=building not selected unit=" + GodotBuildingDebugStatusBuilder.ResolveUnitTypeLabel(unitTypeId));
			return;
		}

		if (_frame == null)
		{
			return;
		}

		GodotTrainActionState state = GodotTrainActionEvaluator.Evaluate(_frame, _selectedBuildingId, unitTypeId);
		if (state != GodotTrainActionState.Ready)
		{
			_debugEventLog.Add(
				"train blocked reason=" + GodotBuildingDebugStatusBuilder.ResolveTrainBlockedReason(state)
				+ " building=" + _selectedBuildingId
				+ " unit=" + GodotBuildingDebugStatusBuilder.ResolveUnitTypeLabel(unitTypeId));
			RefreshFrame();
			return;
		}

		QueueCommandAndConfirm(
			"train p=" + LocalPlayerIndex + " " + GodotBuildingDebugStatusBuilder.BuildTrainIntentText(_selectedBuildingId, unitTypeId),
			facade => facade.QueueTrainUnit(LocalPlayerIndex, _selectedBuildingId, unitTypeId));
	}

	private void ResearchFromSelectedBuilding(int techId)
	{
		if (_selectedBuildingId == 0)
		{
			return;
		}

		if (_frame == null)
		{
			return;
		}

		GodotResearchActionState state = GodotResearchActionEvaluator.EvaluateInfantryAttack1(_frame, _selectedBuildingId);
		if (state != GodotResearchActionState.Ready)
		{
			_debugEventLog.Add("research blocked state=" + state + " building=" + _selectedBuildingId + " tech=" + techId);
			RefreshFrame();
			return;
		}

		QueueCommandAndConfirm(
			"research p=" + LocalPlayerIndex + " building=" + _selectedBuildingId + " tech=" + techId,
			facade => facade.QueueResearchTech(LocalPlayerIndex, _selectedBuildingId, techId));
	}

	private void TryCreateTradeRoute(Vector2 screenPosition)
	{
		if (_frame == null || _selectedUnitIds.Count == 0)
		{
			return;
		}

		int tradeCartId = GodotTradeRouteRouter.FindSelectedTradeCart(_frame, _selectedUnitIds.ToArray());
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

		if (_pendingTradeRouteAId == 0 || _pendingTradeRouteAId == tradePostId)
		{
			_pendingTradeRouteAId = tradePostId;
			_debugEventLog.Add("trade route step A set to tradePost=" + tradePostId);
			RefreshFrame();
			return;
		}

		int routeA = _pendingTradeRouteAId;
		QueueCommandAndConfirm(
			"trade route p=" + LocalPlayerIndex + " cart=" + tradeCartId + " A=" + routeA + " B=" + tradePostId,
			facade => facade.QueueCreateTradeRoute(LocalPlayerIndex, tradeCartId, routeA, tradePostId));
		SetCommandMarker("TradeRoute", screenPosition, Colors.Gold);
		_pendingTradeRouteAId = 0;
	}

	private void SelectAt(Vector2 screenPosition)
	{
		_selectedUnitIds.Clear();
		_selectedBuildingId = 0;
		_pendingTradeRouteAId = 0;
		int clickedResourceId = FindResourceAt(screenPosition);
		if (_frame == null)
		{
			return;
		}

		GodotSelectionResult selection = GodotSelectionRouter.SelectAt(
			_frame,
			LocalPlayerIndex,
			ScreenToRaw(screenPosition.X),
			ScreenToRaw(screenPosition.Y));

		if (selection.Kind == GodotSelectionKind.Unit)
		{
			_selectedUnitIds.Add(selection.EntityId);
			_debugEventLog.Add("select unit=" + selection.EntityId);
		}
		else if (selection.Kind == GodotSelectionKind.Building)
		{
			_selectedBuildingId = selection.EntityId;
			_debugEventLog.Add("select building=" + selection.EntityId);
		}
		else if (clickedResourceId != 0)
		{
			_debugEventLog.Add("select resource=" + clickedResourceId + " hovered=" + _hoveredResourceNodeId);
		}
		else
		{
			_debugEventLog.Add("selection cleared");
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
		bool isSelected = _selectedUnitIds.Contains(primitive.EntityId);
		if (_spriteRenderer.TryDrawUnit(this, primitive, _frame, _selectedUnitIds, ToScreen, RawToPixels))
		{
			if (isSelected)
			{
				DrawSelectionRing(primitive, Colors.Aqua);
			}

			return;
		}

		Rect2 rect = PrimitiveRect(primitive);
		Color color = GetStyleColor(GodotVisualStyleResolver.ResolveUnit(primitive, LocalPlayerIndex));
		DrawRect(rect, color);
		if (isSelected)
		{
			DrawSelectionRing(primitive, Colors.Aqua);
			DrawRect(rect.Grow(2.0f), Colors.White, false, 2.0f);
		}
	}

	private void DrawBuilding(GodotPrimitiveDto primitive)
	{
		bool isSelected = _selectedBuildingId == primitive.EntityId;
		bool isHovered = _hoveredBuildingId == primitive.EntityId;
		if (_spriteRenderer.TryDrawBuilding(this, primitive, _frame, _selectedBuildingId, ToScreen, RawToPixels))
		{
			if (isSelected)
			{
				DrawSelectionRing(primitive, Colors.Gold);
			}
			else if (isHovered)
			{
				DrawSelectionRing(primitive, Colors.Khaki);
			}

			DrawConstructionOverlayIfNeeded(primitive);

			return;
		}

		Rect2 rect = PrimitiveRect(primitive);
		Color color = GetStyleColor(GodotVisualStyleResolver.ResolveBuilding(primitive));
		DrawRect(rect, color);
		if (isSelected)
		{
			DrawSelectionRing(primitive, Colors.Gold);
			DrawRect(rect.Grow(2.0f), Colors.White, false, 2.0f);
		}
		else if (isHovered)
		{
			DrawSelectionRing(primitive, Colors.Khaki);
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
		Color color = GetStyleColor(GodotVisualStyleResolver.ResolveResource(primitive));
		DrawCircle(rect.GetCenter(), rect.Size.X * 0.5f, color);
		if (primitive.EntityId == _hoveredResourceNodeId)
		{
			DrawArc(rect.GetCenter(), rect.Size.X * 0.65f, 0.0f, Mathf.Tau, 32, Colors.White, 2.0f);
		}
	}

	private void DrawTradeRoute(GodotPrimitiveDto primitive)
	{
		DrawLine(ToScreen(primitive.XRaw, primitive.YRaw), ToScreen(primitive.EndXRaw, primitive.EndYRaw), Colors.Gold, 2.0f);
	}

	private void DrawHealthBar(GodotPrimitiveDto primitive)
	{
		if (primitive.MaxHitPoints <= 0)
		{
			return;
		}

		Vector2 center = ToScreen(primitive.XRaw, primitive.YRaw);
		float width = 12.0f;
		float ratio = Mathf.Clamp((float)primitive.CurrentHitPoints / primitive.MaxHitPoints, 0.0f, 1.0f);
		var background = new Rect2(center.X - width * 0.5f, center.Y - 12.0f, width, 2.0f);
		var foreground = new Rect2(background.Position, new Vector2(width * ratio, 2.0f));
		DrawRect(background, Colors.Black);
		DrawRect(foreground, Colors.LimeGreen);
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
			_selectedUnitIds.ToArray(),
			_selectedBuildingId,
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

		DrawCommandMarker();

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
		string[] statusLines = GodotSelectedStatusBuilder.BuildLines(_frame, _selectedUnitIds, _selectedBuildingId, _hoveredResourceNodeId);
		string[] buildingLines = GodotBuildingDebugStatusBuilder.BuildLines(_frame, _selectedBuildingId);
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
			if (kind == GodotPrimitiveDrawKind.Building)
			{
				int radius = ResolveBuildingRadius(primitive.TypeId);
				if (IsWithinRadius(tileXRaw, tileYRaw, primitive.XRaw, primitive.YRaw, radius))
				{
					return true;
				}
			}
			else if (kind == GodotPrimitiveDrawKind.Resource)
			{
				if (IsWithinRadius(tileXRaw, tileYRaw, primitive.XRaw, primitive.YRaw, ResourceRadiusTiles))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool IsWithinRadius(long tileXRaw, long tileYRaw, long centerXRaw, long centerYRaw, int radiusTiles)
	{
		long radiusRaw = TileToRaw(radiusTiles);
		long radiusSquared = radiusRaw * radiusRaw;
		long dx = tileXRaw - centerXRaw;
		long dy = tileYRaw - centerYRaw;
		long distSquared = dx * dx + dy * dy;
		return distSquared < radiusSquared;
	}

	private static int ResolveBuildingRadius(int buildingTypeId)
	{
		switch (buildingTypeId)
		{
			case TownCenterBuildingTypeId:
				return TownCenterRadiusTiles;
			case WallBuildingTypeId:
				return WallRadiusTiles;
			case TradePostBuildingTypeId:
				return TradePostRadiusTiles;
			default:
				return WallRadiusTiles;
		}
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
		float radius = Mathf.Max(10.0f, RawToPixels(primitive.SizeRaw) * 0.74f);
		Vector2 ringCenter = center + new Vector2(0.0f, 4.0f);
		DrawArc(ringCenter, radius + 1.5f, 0.0f, Mathf.Tau, 36, Colors.Black, 3.0f);
		DrawArc(ringCenter, radius, 0.0f, Mathf.Tau, 36, color, 2.4f);
	}

	private void DrawCommandMarker()
	{
		if (_commandMarkerTicksRemaining <= 0)
		{
			return;
		}

		float pulse = 1.0f + (_commandMarkerTicksRemaining % 6) * 0.08f;
		DrawArc(_commandMarkerWorldPosition, 8.0f * pulse, 0.0f, Mathf.Tau, 36, _commandMarkerColor, 2.0f);
		DrawString(ThemeDB.FallbackFont, _commandMarkerWorldPosition + new Vector2(10.0f, -8.0f), _commandMarkerLabel, HorizontalAlignment.Left, -1.0f, 12, _commandMarkerColor);
	}

	private static Vector2 GetUiOriginFromCamera(Camera2D? camera, Rect2 viewportRect)
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

	private Vector2 GetUiOrigin()
	{
		return GetUiOriginFromCamera(_camera, GetViewportRect());
	}

	private Vector2 GetUiSize()
	{
		Rect2 viewportRect = GetViewportRect();
		if (_camera == null)
		{
			return viewportRect.Size;
		}

		Vector2 zoom = _camera.Zoom;
		float zoomX = Mathf.IsZeroApprox(zoom.X) ? 1.0f : zoom.X;
		float zoomY = Mathf.IsZeroApprox(zoom.Y) ? 1.0f : zoom.Y;
		return new Vector2(viewportRect.Size.X * zoomX, viewportRect.Size.Y * zoomY);
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
		_debugEventLog.Add("result " + result + " ex " + beforeExecuted + "->" + afterExecuted + " rej " + beforeRejected + "->" + afterRejected);
	}

	private static GodotPrimitiveDto? FindPrimitiveByEntityId(GodotFrameDto frame, int entityId)
	{
		for (int i = 0; i < frame.Primitives.Length; i++)
		{
			if (frame.Primitives[i].EntityId == entityId)
			{
				return frame.Primitives[i];
			}
		}

		return null;
	}

	private void SetCommandMarker(string label, Vector2 worldPosition, Color color)
	{
		_commandMarkerLabel = label;
		_commandMarkerWorldPosition = worldPosition;
		_commandMarkerColor = color;
		_commandMarkerTicksRemaining = 40;
	}

	private static Color GetStyleColor(GodotVisualStyle style)
	{
		switch (style)
		{
			case GodotVisualStyle.EnemyUnit:
				return Colors.IndianRed;
			case GodotVisualStyle.LocalVillager:
				return Colors.DeepSkyBlue;
			case GodotVisualStyle.LocalScout:
				return Colors.Aqua;
			case GodotVisualStyle.LocalInfantry:
				return Colors.RoyalBlue;
			case GodotVisualStyle.LocalCavalry:
				return Colors.CornflowerBlue;
			case GodotVisualStyle.CapitalBuilding:
				return Colors.Gold;
			case GodotVisualStyle.NormalBuilding:
				return Colors.SlateGray;
			case GodotVisualStyle.Wall:
				return Colors.DarkGray;
			case GodotVisualStyle.FoodResource:
				return Colors.ForestGreen;
			case GodotVisualStyle.WoodResource:
				return Colors.SaddleBrown;
			case GodotVisualStyle.GoldResource:
				return Colors.Goldenrod;
			default:
				return Colors.SteelBlue;
		}
	}

	private Rect2 PrimitiveRect(GodotPrimitiveDto primitive)
	{
		Vector2 center = ToScreen(primitive.XRaw, primitive.YRaw);
		float size = RawToPixels(primitive.SizeRaw);
		return new Rect2(center.X - size * 0.5f, center.Y - size * 0.5f, size, size);
	}

	private static Vector2 ToScreen(long xRaw, long yRaw)
	{
		return new Vector2(RawToPixels(xRaw), RawToPixels(yRaw));
	}

	private static float RawToPixels(long raw)
	{
		return GodotCoordinateMapper.RawToPixels(raw, TilePixels);
	}

	private static long ScreenToRaw(float screenCoordinate)
	{
		return GodotCoordinateMapper.ScreenToRaw(screenCoordinate, TilePixels);
	}

	private static long TileToRaw(int tileCoordinate)
	{
		return ScreenToRaw(tileCoordinate * TilePixels);
	}

	private static Vector2I ScreenToTile(Vector2 screenPosition)
	{
		int x = GodotCoordinateMapper.ScreenToTile(screenPosition.X, TilePixels);
		int y = GodotCoordinateMapper.ScreenToTile(screenPosition.Y, TilePixels);
		return new Vector2I(x, y);
	}

	private void RefreshFrame()
	{
		GodotFrameDto? previous = _frame;
		_frame = _facade!.GetFrame(LocalPlayerIndex);
		LogDepositEvents(previous, _frame);
		_hoveredResourceNodeId = FindResourceAt(GetGlobalMousePosition());
		_hoveredBuildingId = FindHoveredBuildingAt(GetGlobalMousePosition());
		QueueRedraw();
	}

	private int FindResourceAt(Vector2 screenPosition)
	{
		if (_frame == null)
		{
			return 0;
		}

		return GodotInteractionRouter.FindResourceAt(_frame, ScreenToRaw(screenPosition.X), ScreenToRaw(screenPosition.Y));
	}

	private int FindHoveredBuildingAt(Vector2 screenPosition)
	{
		if (_frame == null)
		{
			return 0;
		}

		long xRaw = ScreenToRaw(screenPosition.X);
		long yRaw = ScreenToRaw(screenPosition.Y);
		for (int i = 0; i < _frame.Primitives.Length; i++)
		{
			GodotPrimitiveDto primitive = _frame.Primitives[i];
			if (primitive.OwnerPlayerIndex != LocalPlayerIndex)
			{
				continue;
			}

			if (GodotPrimitiveDrawKindResolver.Resolve(primitive) != GodotPrimitiveDrawKind.Building)
			{
				continue;
			}

			if (GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, xRaw, yRaw))
			{
				return primitive.EntityId;
			}
		}

		return 0;
	}

	private void DrawConstructionOverlayIfNeeded(GodotPrimitiveDto primitive)
	{
		if (_frame == null)
		{
			return;
		}

		GodotBuildingStatusDto? status = FindBuildingStatus(_frame, primitive.EntityId);
		if (status == null || !status.IsUnderConstruction)
		{
			return;
		}

		Vector2 center = ToScreen(primitive.XRaw, primitive.YRaw);
		var bgRect = new Rect2(center.X - 44.0f, center.Y - 30.0f, 88.0f, 20.0f);
		DrawRect(bgRect, new Color(0.0f, 0.0f, 0.0f, 0.5f));
		DrawString(ThemeDB.FallbackFont, bgRect.Position + new Vector2(4.0f, 9.0f), "BUILDING", HorizontalAlignment.Left, -1.0f, 11, Colors.Khaki);
		DrawString(ThemeDB.FallbackFont, bgRect.Position + new Vector2(4.0f, 19.0f), status.BuildProgressTicks + "/" + status.RequiredBuildTicks, HorizontalAlignment.Left, -1.0f, 10, Colors.LightGray);
	}

	private static GodotBuildingStatusDto? FindBuildingStatus(GodotFrameDto frame, int buildingId)
	{
		for (int i = 0; i < frame.BuildingStatuses.Length; i++)
		{
			if (frame.BuildingStatuses[i].BuildingId == buildingId)
			{
				return frame.BuildingStatuses[i];
			}
		}

		return null;
	}

	private void LogDepositEvents(GodotFrameDto? previous, GodotFrameDto current)
	{
		if (previous == null)
		{
			return;
		}

		int foodDelta = current.LocalPlayer.Food - previous.LocalPlayer.Food;
		int woodDelta = current.LocalPlayer.Wood - previous.LocalPlayer.Wood;
		int goldDelta = current.LocalPlayer.Gold - previous.LocalPlayer.Gold;
		if (foodDelta <= 0 && woodDelta <= 0 && goldDelta <= 0)
		{
			return;
		}

		for (int i = 0; i < previous.UnitStatuses.Length; i++)
		{
			GodotUnitStatusDto before = previous.UnitStatuses[i];
			if (before.CarriedAmount <= 0)
			{
				continue;
			}

			GodotUnitStatusDto? after = FindUnitStatus(current, before.UnitId);
			if (after == null || after.CarriedAmount > 0)
			{
				continue;
			}

			string resource = before.CarriedResourceTypeId == 1 ? "food" : before.CarriedResourceTypeId == 2 ? "wood" : before.CarriedResourceTypeId == 3 ? "gold" : "resource";
			_debugEventLog.Add("unit " + before.UnitId + " deposited " + before.CarriedAmount + " " + resource);
			return;
		}
	}

	private static GodotUnitStatusDto? FindUnitStatus(GodotFrameDto frame, int unitId)
	{
		for (int i = 0; i < frame.UnitStatuses.Length; i++)
		{
			if (frame.UnitStatuses[i].UnitId == unitId)
			{
				return frame.UnitStatuses[i];
			}
		}

		return null;
	}

	private void ConfirmTcPlacement(Vector2I tile)
	{
		if (_facade == null || _frame == null)
		{
			return;
		}

		_tcPlacementMode = false;
		GodotFrameDto frameBefore = _frame;
		TcPlacementPreviewResult previewBefore = _tcPreviewResult;

		if (_tcPreviewResult != TcPlacementPreviewResult.Valid)
		{
			_debugEventLog.Add("TC placement rejected by preview: " + _tcPreviewResult);
			QueueRedraw();
			return;
		}

		SetCommandMarker("TC", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.LightBlue);
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
		GodotLocalPlayerDto player = frameBefore.LocalPlayer;
		_debugEventLog.Add(
			"Preview/command mismatch tile=(" + tile.X + "," + tile.Y + ")"
			+ " preview=" + previewResult
			+ " command=PlaceTownCenter"
			+ " player=" + LocalPlayerIndex
			+ " wood=" + player.Wood
			+ " hasCapitalPlaced=" + player.HasCapitalBeenPlaced
			+ " connected=" + player.IsConnected
			+ " defeated=" + player.IsDefeated
			+ " resigned=" + player.IsResigned);
	}

	private void DrawTcPlacementGhost()
	{
		if (_frame == null)
		{
			return;
		}

		Vector2 center = ToScreen(TileToRaw(_tcHoveredTile.X), TileToRaw(_tcHoveredTile.Y));
		float tileSize = TilePixels;
		
		// TC is 2 radius -> diameter 4 tiles approx. We use RawToPixels for consistency.
		// TC SizeRaw is not directly known without primitive, but usually 4 * FixedOneRaw = 64 pixels.
		float size = 4.0f * tileSize;
		Rect2 rect = new Rect2(center.X - size * 0.5f, center.Y - size * 0.5f, size, size);

		Color ghostColor = _tcPreviewResult == TcPlacementPreviewResult.Valid ? new Color(0.2f, 1.0f, 0.2f, 0.5f) : new Color(1.0f, 0.2f, 0.2f, 0.5f);
		DrawRect(rect, new Color(ghostColor, 0.2f)); // fill
		DrawRect(rect, ghostColor, false, 2.0f);     // border

		// Draw radius
		float radiusPixels = 2.0f * tileSize; // TC Placement Radius = 2
		DrawArc(center, radiusPixels, 0.0f, Mathf.Tau, 32, ghostColor, 1.0f);

		// Label
		DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(0.0f, -4.0f), "[TC] " + _tcPreviewResult, HorizontalAlignment.Left, -1.0f, 12, ghostColor);
	}
}
