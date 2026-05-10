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
	private const int VillagerUnitTypeId = 1;
	private const int InfantryUnitTypeId = 3;
	private const int TradeCartUnitTypeId = 5;
	private const int InfantryAttackTechId = 1;

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
	private string _commandMarkerLabel = "";
	private Color _commandMarkerColor = Colors.White;
	private Vector2 _commandMarkerWorldPosition = Vector2.Zero;
	private int _commandMarkerTicksRemaining;
	private int _hoveredResourceNodeId;
	private int _selectedBuildingId;
	private int _pendingTradeRouteAId;

	public override void _Ready()
	{
		_camera = new Camera2D();
		AddChild(_camera);
		_camera.MakeCurrent();
		_spriteRenderer.LoadAssets();
		StartLocalMatch(2);
		_debugEventLog.Add("ready local match 1v1");
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

		if (@event is InputEventMouseButton mouse && mouse.Pressed)
		{
			HandleMouse(mouse);
		}
	}

	public override void _Draw()
	{
		if (_frame == null)
		{
			return;
		}

		for (int i = 0; i < _frame.Primitives.Length; i++)
		{
			DrawPrimitive(_frame.Primitives[i]);
		}

		DrawHud();
	}

	private void HandleKey(InputEventKey key)
	{
		if (key.Keycode == Key.F1)
		{
			StartLocalMatch(2);
			_debugEventLog.Add("restart local 1v1 (F1)");
			return;
		}

		if (key.Keycode == Key.F6)
		{
			StartLocalMatch(6);
			_debugEventLog.Add("restart local 6-player ffa (F6)");
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
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			SetCommandMarker("TC", ToScreen(TileToRaw(tile.X), TileToRaw(tile.Y)), Colors.LightBlue);
			QueueCommandAndConfirm(
				"place town center p=" + LocalPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ")",
				facade => facade.QueuePlaceTownCenter(LocalPlayerIndex, tile.X, tile.Y));
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
		_selectedUnitIds.Clear();
		_selectedBuildingId = 0;
		_pendingTradeRouteAId = 0;
		_hoveredResourceNodeId = 0;
		_tickAccumulator = 0.0;
		_paused = false;
		_facade.AdvanceOneTick();
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
		if (mouse.ButtonIndex == MouseButton.Left)
		{
			SelectAt(mouseWorldPosition);
			RefreshFrame();
			return;
		}

		if (mouse.ButtonIndex == MouseButton.Right && _selectedUnitIds.Count > 0)
		{
			GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(
				frame,
				LocalPlayerIndex,
				_selectedUnitIds.Count > 0,
				ScreenToRaw(mouseWorldPosition.X),
				ScreenToRaw(mouseWorldPosition.Y));

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

	private void UpdateCamera(double delta)
	{
		if (_camera == null)
		{
			return;
		}

		Vector2 direction = Vector2.Zero;
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

		if (direction == Vector2.Zero)
		{
			return;
		}

		_camera.Position += direction.Normalized() * CameraPanPixelsPerSecond * (float)delta;
	}

	private void TrainFromSelectedBuilding(int unitTypeId)
	{
		if (_selectedBuildingId == 0)
		{
			return;
		}

		if (_frame == null)
		{
			return;
		}

		GodotTrainActionState state = GodotTrainActionEvaluator.Evaluate(_frame, _selectedBuildingId, unitTypeId);
		if (state != GodotTrainActionState.Ready)
		{
			_debugEventLog.Add("train blocked state=" + state + " building=" + _selectedBuildingId + " unitType=" + unitTypeId);
			RefreshFrame();
			return;
		}

		QueueCommandAndConfirm(
			"train p=" + LocalPlayerIndex + " building=" + _selectedBuildingId + " unitType=" + unitTypeId,
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
		if (_spriteRenderer.TryDrawBuilding(this, primitive, _frame, _selectedBuildingId, ToScreen, RawToPixels))
		{
			if (isSelected)
			{
				DrawSelectionRing(primitive, Colors.Gold);
			}

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
	}

	private void DrawResource(GodotPrimitiveDto primitive)
	{
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

		if (_showDebugOverlay)
		{
			DrawDebugOverlay(uiOrigin);
		}

		if (_showHotkeyHelp)
		{
			DrawHotkeyHelpPanel(uiOrigin);
		}
	}

	private void DrawDebugOverlay(Vector2 uiOrigin)
	{
		Vector2 panelPos = uiOrigin + new Vector2(0.0f, 88.0f);
		float panelWidth = 1120.0f;
		float panelHeight = 246.0f;
		DrawRect(new Rect2(panelPos, new Vector2(panelWidth, panelHeight)), new Color(0.0f, 0.0f, 0.0f, 0.52f));

		string[] statusLines = GodotSelectedStatusBuilder.BuildLines(_frame, _selectedUnitIds, _selectedBuildingId, _hoveredResourceNodeId);
		DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 20.0f), "Debug Overlay (F10)", HorizontalAlignment.Left, -1.0f, 15, Colors.WhiteSmoke);
		for (int i = 0; i < statusLines.Length; i++)
		{
			DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 40.0f + i * 16.0f), statusLines[i], HorizontalAlignment.Left, -1.0f, 14, Colors.LightGray);
		}

		string[] events = _debugEventLog.GetLines();
		DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 84.0f), "Events", HorizontalAlignment.Left, -1.0f, 14, Colors.WhiteSmoke);
		int maxEvents = Mathf.Min(events.Length, 10);
		for (int i = 0; i < maxEvents; i++)
		{
			int eventIndex = events.Length - maxEvents + i;
			DrawString(ThemeDB.FallbackFont, panelPos + new Vector2(10.0f, 102.0f + i * 14.0f), events[eventIndex], HorizontalAlignment.Left, -1.0f, 13, Colors.LightGray);
		}
	}

	private void DrawHotkeyHelpPanel(Vector2 uiOrigin)
	{
		GodotHotkeyHelpEntry[] entries = GodotHotkeyHelpBuilder.Build(researchIsWired: true);
		Vector2 panelPos = uiOrigin + new Vector2(0.0f, 344.0f);
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

	private void DrawSelectionRing(GodotPrimitiveDto primitive, Color color)
	{
		Vector2 center = ToScreen(primitive.XRaw, primitive.YRaw);
		float radius = Mathf.Max(9.0f, RawToPixels(primitive.SizeRaw) * 0.66f);
		DrawArc(center + new Vector2(0.0f, 4.0f), radius, 0.0f, Mathf.Tau, 32, color, 2.0f);
	}

	private void DrawCommandMarker()
	{
		if (_commandMarkerTicksRemaining <= 0)
		{
			return;
		}

		float pulse = 1.0f + (_commandMarkerTicksRemaining % 6) * 0.12f;
		DrawArc(_commandMarkerWorldPosition, 10.0f * pulse, 0.0f, Mathf.Tau, 40, _commandMarkerColor, 2.0f);
		DrawString(ThemeDB.FallbackFont, _commandMarkerWorldPosition + new Vector2(14.0f, -10.0f), _commandMarkerLabel, HorizontalAlignment.Left, -1.0f, 13, _commandMarkerColor);
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
		_frame = _facade!.GetFrame(LocalPlayerIndex);
		_hoveredResourceNodeId = FindResourceAt(GetGlobalMousePosition());
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
}
