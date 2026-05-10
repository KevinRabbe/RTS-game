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
	private Camera2D? _camera;
	private GodotClientFacade? _facade;
	private GodotFrameDto? _frame;
	private readonly Phase6SpriteRenderer _spriteRenderer = new Phase6SpriteRenderer();
	private double _tickAccumulator;
	private bool _paused;
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
	}

	public override void _Process(double delta)
	{
		if (_facade == null)
		{
			return;
		}

		UpdateCamera(delta);
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
			return;
		}

		if (key.Keycode == Key.F6)
		{
			StartLocalMatch(6);
			return;
		}

		if (key.Keycode == Key.Space)
		{
			_paused = !_paused;
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.F9)
		{
			_spriteRenderer.ToggleRenderMode();
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.C)
		{
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			_facade!.QueuePlaceTownCenter(LocalPlayerIndex, tile.X, tile.Y);
			_facade.AdvanceOneTick();
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.W)
		{
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			_facade!.QueuePlaceWall(LocalPlayerIndex, tile.X, tile.Y);
			_facade.AdvanceOneTick();
			RefreshFrame();
			return;
		}

		if (key.Keycode == Key.T)
		{
			Vector2I tile = ScreenToTile(GetGlobalMousePosition());
			_facade!.QueuePlaceTradePost(LocalPlayerIndex, tile.X, tile.Y);
			_facade.AdvanceOneTick();
			RefreshFrame();
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
		GodotClientFacade? facade = _facade;
		GodotFrameDto? frame = _frame;
		if (facade == null || frame == null)
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
				facade.QueueAttack(LocalPlayerIndex, _selectedUnitIds.ToArray(), intent.TargetEntityId);
			}
			else if (intent.Kind == GodotInteractionIntentKind.AssignBuild)
			{
				facade.QueueAssignBuild(LocalPlayerIndex, intent.TargetEntityId, _selectedUnitIds.ToArray());
			}
			else if (intent.Kind == GodotInteractionIntentKind.GatherResource)
			{
				facade.QueueGatherResource(LocalPlayerIndex, intent.ResourceNodeId, _selectedUnitIds.ToArray());
			}
			else if (intent.Kind == GodotInteractionIntentKind.Move)
			{
				facade.QueueMoveUnits(LocalPlayerIndex, _selectedUnitIds.ToArray(), tile.X, tile.Y);
			}

			facade.AdvanceOneTick();
			RefreshFrame();
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
			RefreshFrame();
			return;
		}

		_facade!.QueueTrainUnit(LocalPlayerIndex, _selectedBuildingId, unitTypeId);
		_facade.AdvanceOneTick();
		RefreshFrame();
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
			RefreshFrame();
			return;
		}

		_facade!.QueueResearchTech(LocalPlayerIndex, _selectedBuildingId, techId);
		_facade.AdvanceOneTick();
		RefreshFrame();
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
			return;
		}

		int tradePostId = GodotTradeRouteRouter.FindLocalTradePostAt(
			_frame,
			LocalPlayerIndex,
			ScreenToRaw(screenPosition.X),
			ScreenToRaw(screenPosition.Y));

		if (tradePostId == 0)
		{
			return;
		}

		if (_pendingTradeRouteAId == 0 || _pendingTradeRouteAId == tradePostId)
		{
			_pendingTradeRouteAId = tradePostId;
			RefreshFrame();
			return;
		}

		_facade!.QueueCreateTradeRoute(LocalPlayerIndex, tradeCartId, _pendingTradeRouteAId, tradePostId);
		_pendingTradeRouteAId = 0;
		_facade.AdvanceOneTick();
		RefreshFrame();
	}

	private void SelectAt(Vector2 screenPosition)
	{
		_selectedUnitIds.Clear();
		_selectedBuildingId = 0;
		_pendingTradeRouteAId = 0;
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
		}
		else if (selection.Kind == GodotSelectionKind.Building)
		{
			_selectedBuildingId = selection.EntityId;
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
		if (_spriteRenderer.TryDrawUnit(this, primitive, _frame, _selectedUnitIds, ToScreen, RawToPixels))
		{
			return;
		}

		Rect2 rect = PrimitiveRect(primitive);
		Color color = GetStyleColor(GodotVisualStyleResolver.ResolveUnit(primitive, LocalPlayerIndex));
		DrawRect(rect, color);
		if (_selectedUnitIds.Contains(primitive.EntityId))
		{
			DrawRect(rect.Grow(2.0f), Colors.White, false, 2.0f);
		}
	}

	private void DrawBuilding(GodotPrimitiveDto primitive)
	{
		if (_spriteRenderer.TryDrawBuilding(this, primitive, _frame, _selectedBuildingId, ToScreen, RawToPixels))
		{
			return;
		}

		Rect2 rect = PrimitiveRect(primitive);
		Color color = GetStyleColor(GodotVisualStyleResolver.ResolveBuilding(primitive));
		DrawRect(rect, color);
		if (_selectedBuildingId == primitive.EntityId)
		{
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

        string[] lines = GodotHudTextBuilder.BuildLines(
            _frame,
            _selectedUnitIds.ToArray(),
            _selectedBuildingId,
            _hoveredResourceNodeId,
            _paused);

		for (int i = 0; i < lines.Length; i++)
		{
			DrawString(ThemeDB.FallbackFont, new Vector2(12.0f, 20.0f + i * 18.0f), lines[i], HorizontalAlignment.Left, -1.0f, 16, Colors.White);
		}

		DrawString(ThemeDB.FallbackFont, new Vector2(12.0f, 56.0f), "Render " + _spriteRenderer.RenderModeLabel + " (F9)  Assets " + _spriteRenderer.LoadedAssetCount + "/6", HorizontalAlignment.Left, -1.0f, 16, Colors.White);
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
