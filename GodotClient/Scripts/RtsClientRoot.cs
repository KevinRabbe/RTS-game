using System;
using System.Collections.Generic;
using Godot;
using RtsGame.Presentation.GodotBridge;

public partial class RtsClientRoot : Node2D
{
    private const int LocalPlayerIndex = 0;
    private const float TilePixels = 16.0f;
    private const long FixedOneRaw = 1L << 16;
    private const double TickSeconds = 1.0 / 20.0;
    private const int VillagerUnitTypeId = 1;
    private const int InfantryUnitTypeId = 3;

    private readonly List<int> _selectedUnitIds = new List<int>();
    private GodotClientFacade? _facade;
    private GodotFrameDto? _frame;
    private double _tickAccumulator;
    private bool _paused;
    private int _hoveredResourceNodeId;
    private int _selectedBuildingId;

    public override void _Ready()
    {
        _facade = GodotClientFacade.CreateLocal1v1(12345UL);
        _facade.AdvanceOneTick();
        RefreshFrame();
    }

    public override void _Process(double delta)
    {
        if (_facade == null || _paused)
        {
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
        if (key.Keycode == Key.Space)
        {
            _paused = !_paused;
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

        if (key.Keycode == Key.V)
        {
            TrainFromSelectedBuilding(VillagerUnitTypeId);
            return;
        }

        if (key.Keycode == Key.I)
        {
            TrainFromSelectedBuilding(InfantryUnitTypeId);
        }
    }

    private void HandleMouse(InputEventMouseButton mouse)
    {
        Vector2I tile = ScreenToTile(mouse.Position);
        if (mouse.ButtonIndex == MouseButton.Left)
        {
            SelectAt(mouse.Position);
            RefreshFrame();
            return;
        }

        if (mouse.ButtonIndex == MouseButton.Right && _selectedUnitIds.Count > 0)
        {
            int attackTargetId = FindEnemyTargetAt(mouse.Position);
            int resourceNodeId = FindResourceAt(mouse.Position);
            if (attackTargetId != 0)
            {
                _facade!.QueueAttack(LocalPlayerIndex, _selectedUnitIds.ToArray(), attackTargetId);
            }
            else if (resourceNodeId != 0)
            {
                _facade!.QueueGatherResource(LocalPlayerIndex, resourceNodeId, _selectedUnitIds.ToArray());
            }
            else
            {
                _facade!.QueueMoveUnits(LocalPlayerIndex, _selectedUnitIds.ToArray(), tile.X, tile.Y);
            }

            _facade.AdvanceOneTick();
            RefreshFrame();
        }
    }

    private void TrainFromSelectedBuilding(int unitTypeId)
    {
        if (_selectedBuildingId == 0)
        {
            return;
        }

        _facade!.QueueTrainUnit(LocalPlayerIndex, _selectedBuildingId, unitTypeId);
        _facade.AdvanceOneTick();
        RefreshFrame();
    }

    private void SelectAt(Vector2 screenPosition)
    {
        _selectedUnitIds.Clear();
        _selectedBuildingId = 0;
        if (_frame == null)
        {
            return;
        }

        for (int i = 0; i < _frame.Primitives.Length; i++)
        {
            GodotPrimitiveDto primitive = _frame.Primitives[i];
            if (primitive.Kind != 1 || primitive.OwnerPlayerIndex != LocalPlayerIndex)
            {
                continue;
            }

            Rect2 rect = PrimitiveRect(primitive);
            if (rect.HasPoint(screenPosition))
            {
                _selectedUnitIds.Add(primitive.EntityId);
                return;
            }
        }

        for (int i = 0; i < _frame.Primitives.Length; i++)
        {
            GodotPrimitiveDto primitive = _frame.Primitives[i];
            if ((primitive.Kind != 2 && primitive.Kind != 3) || primitive.OwnerPlayerIndex != LocalPlayerIndex)
            {
                continue;
            }

            Rect2 rect = PrimitiveRect(primitive);
            if (rect.HasPoint(screenPosition))
            {
                _selectedBuildingId = primitive.EntityId;
                return;
            }
        }
    }

    private void DrawPrimitive(GodotPrimitiveDto primitive)
    {
        switch (primitive.Kind)
        {
            case 1:
                DrawUnit(primitive);
                break;
            case 2:
            case 3:
                DrawBuilding(primitive);
                break;
            case 4:
                DrawTradeRoute(primitive);
                break;
            case 5:
                DrawHealthBar(primitive);
                break;
            case 6:
                DrawFogOverlay();
                break;
            case 7:
            case 8:
            case 9:
                DrawResource(primitive);
                break;
        }
    }

    private void DrawUnit(GodotPrimitiveDto primitive)
    {
        Rect2 rect = PrimitiveRect(primitive);
        Color color = primitive.OwnerPlayerIndex == LocalPlayerIndex ? Colors.DeepSkyBlue : Colors.IndianRed;
        DrawRect(rect, color);
        if (_selectedUnitIds.Contains(primitive.EntityId))
        {
            DrawRect(rect.Grow(2.0f), Colors.White, false, 2.0f);
        }
    }

    private void DrawBuilding(GodotPrimitiveDto primitive)
    {
        Rect2 rect = PrimitiveRect(primitive);
        Color color = primitive.IsCapital ? Colors.Gold : Colors.SlateGray;
        if (primitive.Kind == 3)
        {
            color = Colors.DarkGray;
        }

        DrawRect(rect, color);
        if (_selectedBuildingId == primitive.EntityId)
        {
            DrawRect(rect.Grow(2.0f), Colors.White, false, 2.0f);
        }
    }

    private void DrawResource(GodotPrimitiveDto primitive)
    {
        Rect2 rect = PrimitiveRect(primitive);
        Color color = Colors.ForestGreen;
        if (primitive.Kind == 8)
        {
            color = Colors.SaddleBrown;
        }
        else if (primitive.Kind == 9)
        {
            color = Colors.Goldenrod;
        }

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

        GodotLocalPlayerDto player = _frame.LocalPlayer;
        string selected = _selectedUnitIds.Count == 0 ? "-" : string.Join(",", _selectedUnitIds);
        string selectedBuilding = _selectedBuildingId == 0 ? "-" : _selectedBuildingId.ToString();
        string hoveredResource = _hoveredResourceNodeId == 0 ? "-" : _hoveredResourceNodeId.ToString();
        string text = "Tick " + _frame.Tick
            + "  Food " + player.Food
            + "  Wood " + player.Wood
            + "  Gold " + player.Gold
            + "  Pop " + player.PopulationUsed + "/" + player.PopulationCap
            + "  Selected " + selected
            + "  Building " + selectedBuilding
            + "  Resource " + hoveredResource
            + (_paused ? "  Paused" : "");

        DrawString(ThemeDB.FallbackFont, new Vector2(12.0f, 20.0f), text, HorizontalAlignment.Left, -1.0f, 16, Colors.White);
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
        return (float)((raw / (double)FixedOneRaw) * TilePixels);
    }

    private static Vector2I ScreenToTile(Vector2 screenPosition)
    {
        int x = Mathf.FloorToInt(screenPosition.X / TilePixels);
        int y = Mathf.FloorToInt(screenPosition.Y / TilePixels);
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

        for (int i = 0; i < _frame.Primitives.Length; i++)
        {
            GodotPrimitiveDto primitive = _frame.Primitives[i];
            if (primitive.Kind < 7 || primitive.Kind > 9)
            {
                continue;
            }

            if (PrimitiveRect(primitive).HasPoint(screenPosition))
            {
                return primitive.EntityId;
            }
        }

        return 0;
    }

    private int FindEnemyTargetAt(Vector2 screenPosition)
    {
        if (_frame == null)
        {
            return 0;
        }

        for (int i = 0; i < _frame.Primitives.Length; i++)
        {
            GodotPrimitiveDto primitive = _frame.Primitives[i];
            if ((primitive.Kind != 1 && primitive.Kind != 2 && primitive.Kind != 3)
                || primitive.OwnerPlayerIndex == LocalPlayerIndex
                || primitive.OwnerPlayerIndex < 0)
            {
                continue;
            }

            if (PrimitiveRect(primitive).HasPoint(screenPosition))
            {
                return primitive.EntityId;
            }
        }

        return 0;
    }
}
