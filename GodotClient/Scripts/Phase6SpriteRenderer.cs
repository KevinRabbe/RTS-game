using System.Collections.Generic;
using Godot;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Sim.Data;

public sealed class Phase6SpriteRenderer
{
	private const string ArtRoot = "res://Art/Phase6Pack/";

	private Texture2D? _villagerSheet;
	private Texture2D? _infantrySheet;
	private Texture2D? _scoutSheet;
	private Texture2D? _tradeCartSheet;
	private Texture2D? _capitalSprite;
	private Texture2D? _wallSheet;
	private bool _useSprites = true;
	private int _loadedAssetCount;

	public string RenderModeLabel
	{
		get
		{
			if (!_useSprites)
			{
				return "Primitives";
			}

			return _loadedAssetCount > 0 ? "Sprites" : "Sprites(Missing)";
		}
	}

	public int LoadedAssetCount
	{
		get { return _loadedAssetCount; }
	}

	public void ToggleRenderMode()
	{
		_useSprites = !_useSprites;
	}

	public void LoadAssets()
	{
		_villagerSheet = ResourceLoader.Load<Texture2D>(ArtRoot + "villager_sheet.png");
		_infantrySheet = ResourceLoader.Load<Texture2D>(ArtRoot + "infantry_sheet.png");
		_scoutSheet = ResourceLoader.Load<Texture2D>(ArtRoot + "scout_sheet.png");
		_tradeCartSheet = ResourceLoader.Load<Texture2D>(ArtRoot + "trade_cart_sheet.png");
		_capitalSprite = ResourceLoader.Load<Texture2D>(ArtRoot + "capital.png");
		_wallSheet = ResourceLoader.Load<Texture2D>(ArtRoot + "wall_sheet.png");
		_loadedAssetCount = CountLoadedAssets();
	}

	public bool TryDrawUnit(
		Node2D canvas,
		GodotPrimitiveDto primitive,
		GodotFrameDto? frame,
		IReadOnlyCollection<int> selectedUnitIds,
		System.Func<long, long, Vector2> toScreen,
		System.Func<long, float> rawToPixels)
	{
		if (!_useSprites)
		{
			return false;
		}

		Texture2D? sheet = primitive.TypeId switch
		{
			(int)UnitTypeId.Villager => _villagerSheet,
			(int)UnitTypeId.Infantry => _infantrySheet,
			(int)UnitTypeId.Scout => _scoutSheet,
			(int)UnitTypeId.Cavalry => _scoutSheet,
			(int)UnitTypeId.TradeCart => _tradeCartSheet,
			_ => null
		};
		if (sheet == null)
		{
			return false;
		}

		GodotUnitStatusDto? status = FindUnitStatus(frame, primitive.EntityId);
		int directionIndex = ResolveDirectionFrameIndex(status, primitive);
		Rect2 target = GetUnitSpriteRect(primitive, toScreen, rawToPixels);
		DrawSheetFrame(canvas, sheet, 3, 3, directionIndex, target);

		if (IsSelected(selectedUnitIds, primitive.EntityId))
		{
			canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
		}

		return true;
	}

	public bool TryDrawBuilding(
		Node2D canvas,
		GodotPrimitiveDto primitive,
		GodotFrameDto? frame,
		int selectedBuildingId,
		System.Func<long, long, Vector2> toScreen,
		System.Func<long, float> rawToPixels)
	{
		if (!_useSprites)
		{
			return false;
		}

		if (primitive.TypeId == (int)BuildingTypeId.Wall && _wallSheet != null)
		{
			GodotBuildingStatusDto? status = FindBuildingStatus(frame, primitive.EntityId);
			Rect2 target = GetBuildingSpriteRect(primitive, 3.4f, toScreen, rawToPixels);
			int frameIndex = status != null && status.IsUnderConstruction ? 3 : 0;
			DrawSheetFrame(canvas, _wallSheet, 3, 2, frameIndex, target);
			if (selectedBuildingId == primitive.EntityId)
			{
				canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
			}

			return true;
		}

		if (primitive.TypeId == (int)BuildingTypeId.TownCenter && _capitalSprite != null)
		{
			Rect2 target = GetBuildingSpriteRect(primitive, primitive.IsCapital ? 3.6f : 3.1f, toScreen, rawToPixels);
			canvas.DrawTextureRect(_capitalSprite, target, false);
			if (selectedBuildingId == primitive.EntityId)
			{
				canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
			}

			return true;
		}

		return false;
	}

	private static void DrawSheetFrame(Node2D canvas, Texture2D texture, int columns, int rows, int frameIndex, Rect2 target)
	{
		int clampedFrame = Mathf.Clamp(frameIndex, 0, columns * rows - 1);
		float frameWidth = (float)texture.GetWidth() / columns;
		float frameHeight = (float)texture.GetHeight() / rows;
		int x = clampedFrame % columns;
		int y = clampedFrame / columns;
		var source = new Rect2(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
		canvas.DrawTextureRectRegion(texture, target, source);
	}

	private static Rect2 GetUnitSpriteRect(GodotPrimitiveDto primitive, System.Func<long, long, Vector2> toScreen, System.Func<long, float> rawToPixels)
	{
		Vector2 center = toScreen(primitive.XRaw, primitive.YRaw);
		float worldSize = rawToPixels(primitive.SizeRaw);
		float size = Mathf.Max(34.0f, worldSize * 3.2f);
		return new Rect2(center.X - size * 0.5f, center.Y - size * 0.72f, size, size);
	}

	private static Rect2 GetBuildingSpriteRect(GodotPrimitiveDto primitive, float scale, System.Func<long, long, Vector2> toScreen, System.Func<long, float> rawToPixels)
	{
		Vector2 center = toScreen(primitive.XRaw, primitive.YRaw);
		float worldSize = rawToPixels(primitive.SizeRaw);
		float size = Mathf.Max(84.0f, worldSize * scale);
		return new Rect2(center.X - size * 0.5f, center.Y - size * 0.74f, size, size);
	}

	private static GodotUnitStatusDto? FindUnitStatus(GodotFrameDto? frame, int unitId)
	{
		if (frame == null)
		{
			return null;
		}

		for (int i = 0; i < frame.UnitStatuses.Length; i++)
		{
			if (frame.UnitStatuses[i].UnitId == unitId)
			{
				return frame.UnitStatuses[i];
			}
		}

		return null;
	}

	private static GodotBuildingStatusDto? FindBuildingStatus(GodotFrameDto? frame, int buildingId)
	{
		if (frame == null)
		{
			return null;
		}

		for (int i = 0; i < frame.BuildingStatuses.Length; i++)
		{
			if (frame.BuildingStatuses[i].BuildingId == buildingId)
			{
				return frame.BuildingStatuses[i];
			}
		}

		return null;
	}

	private static int ResolveDirectionFrameIndex(GodotUnitStatusDto? status, GodotPrimitiveDto primitive)
	{
		if (status == null || !status.HasMoveTarget)
		{
			return 1;
		}

		long dxRaw = status.MoveTargetXRaw - primitive.XRaw;
		long dyRaw = status.MoveTargetYRaw - primitive.YRaw;
		int dx = dxRaw > 0 ? 1 : dxRaw < 0 ? -1 : 0;
		int dy = dyRaw > 0 ? 1 : dyRaw < 0 ? -1 : 0;

		return (dx, dy) switch
		{
			(-1, -1) => 0,
			(0, -1) => 1,
			(1, -1) => 2,
			(-1, 0) => 3,
			(0, 0) => 1,
			(1, 0) => 5,
			(-1, 1) => 6,
			(0, 1) => 7,
			(1, 1) => 8,
			_ => 1
		};
	}

	private static bool IsSelected(IReadOnlyCollection<int> selectedUnitIds, int entityId)
	{
		foreach (int id in selectedUnitIds)
		{
			if (id == entityId)
			{
				return true;
			}
		}

		return false;
	}

	private int CountLoadedAssets()
	{
		int count = 0;
		if (_villagerSheet != null)
		{
			count++;
		}

		if (_infantrySheet != null)
		{
			count++;
		}

		if (_scoutSheet != null)
		{
			count++;
		}

		if (_tradeCartSheet != null)
		{
			count++;
		}

		if (_capitalSprite != null)
		{
			count++;
		}

		if (_wallSheet != null)
		{
			count++;
		}

		return count;
	}
}
