using System.Collections.Generic;
using Godot;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Sim.Data;

public sealed class Phase6SpriteRenderer
{
	private const string ArtRoot = "res://Art/Phase6Pack/";
	private readonly Dictionary<GodotSpriteAssetId, Texture2D?> _assetRegistry = new Dictionary<GodotSpriteAssetId, Texture2D?>();

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

	public int ExpectedAssetCount
	{
		get { return GodotSpriteSheetLayout.ExpectedAssetCount; }
	}

	public string MissingAssetsLabel
	{
		get
		{
			List<string> missing = new List<string>();
			GodotSpriteSheetMetadata[] metadata = GodotSpriteSheetLayout.GetAllMetadata();
			for (int i = 0; i < metadata.Length; i++)
			{
				GodotSpriteSheetMetadata asset = metadata[i];
				if (!_assetRegistry.TryGetValue(asset.Id, out Texture2D? texture) || texture == null)
				{
					missing.Add(asset.DisplayName);
				}
			}

			return missing.Count == 0 ? "None" : string.Join(", ", missing);
		}
	}

	public void ToggleRenderMode()
	{
		_useSprites = !_useSprites;
	}

	public void LoadAssets()
	{
		_assetRegistry.Clear();
		GodotSpriteSheetMetadata[] metadata = GodotSpriteSheetLayout.GetAllMetadata();
		for (int i = 0; i < metadata.Length; i++)
		{
			GodotSpriteSheetMetadata asset = metadata[i];
			_assetRegistry[asset.Id] = ResourceLoader.Load<Texture2D>(ArtRoot + asset.FileName);
		}

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

		if (!GodotSpriteSheetLayout.TryResolveUnitAsset(primitive.TypeId, out GodotSpriteAssetId assetId))
		{
			return false;
		}

		Texture2D? sheet = GetAsset(assetId);
		if (sheet == null || !GodotSpriteSheetLayout.TryGetMetadata(assetId, out GodotSpriteSheetMetadata metadata))
		{
			return false;
		}

		GodotUnitStatusDto? status = FindUnitStatus(frame, primitive.EntityId);
		int dx = 0;
		int dy = 0;
		bool hasMoveTarget = status != null && status.HasMoveTarget;
		if (hasMoveTarget && status != null)
		{
			long dxRaw = status.MoveTargetXRaw - primitive.XRaw;
			long dyRaw = status.MoveTargetYRaw - primitive.YRaw;
			dx = dxRaw > 0 ? 1 : dxRaw < 0 ? -1 : 0;
			dy = dyRaw > 0 ? 1 : dyRaw < 0 ? -1 : 0;
		}

		int frameIndex = GodotSpriteSheetLayout.ResolveDirectionalFrameIndex(metadata, hasMoveTarget, dx, dy);
		Rect2 target = GetUnitSpriteRect(primitive, toScreen, rawToPixels);
		DrawSheetFrame(canvas, sheet, metadata, frameIndex, target);

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

		if (primitive.TypeId == (int)BuildingTypeId.Wall &&
			GodotSpriteSheetLayout.TryResolveBuildingAsset(primitive.TypeId, out GodotSpriteAssetId wallAssetId) &&
			GodotSpriteSheetLayout.TryGetMetadata(wallAssetId, out GodotSpriteSheetMetadata wallMetadata))
		{
			Texture2D? wallSheet = GetAsset(wallAssetId);
			if (wallSheet == null)
			{
				return false;
			}

			GodotBuildingStatusDto? status = FindBuildingStatus(frame, primitive.EntityId);
			Rect2 target = GetBuildingSpriteRect(primitive, 3.4f, toScreen, rawToPixels);
			int frameIndex = status != null && status.IsUnderConstruction ? wallMetadata.UnderConstructionFrameIndex : wallMetadata.DefaultFrameIndex;
			DrawSheetFrame(canvas, wallSheet, wallMetadata, frameIndex, target);
			if (selectedBuildingId == primitive.EntityId)
			{
				canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
			}

			return true;
		}

		Texture2D? capitalSprite = GetAsset(GodotSpriteAssetId.Capital);
		if (primitive.TypeId == (int)BuildingTypeId.TownCenter && capitalSprite != null)
		{
			Rect2 target = GetBuildingSpriteRect(primitive, primitive.IsCapital ? 3.6f : 3.1f, toScreen, rawToPixels);
			canvas.DrawTextureRect(capitalSprite, target, false);
			if (selectedBuildingId == primitive.EntityId)
			{
				canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
			}

			return true;
		}

		return false;
	}

	private static void DrawSheetFrame(Node2D canvas, Texture2D texture, GodotSpriteSheetMetadata metadata, int frameIndex, Rect2 target)
	{
		GodotSpriteFrameRect frame = GodotSpriteSheetLayout.ResolveFrameRect(metadata, texture.GetWidth(), texture.GetHeight(), frameIndex);
		var source = new Rect2(frame.X, frame.Y, frame.Width, frame.Height);
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

	private Texture2D? GetAsset(GodotSpriteAssetId key)
	{
		_assetRegistry.TryGetValue(key, out Texture2D? texture);
		return texture;
	}

	private int CountLoadedAssets()
	{
		int count = 0;
		foreach (KeyValuePair<GodotSpriteAssetId, Texture2D?> entry in _assetRegistry)
		{
			if (entry.Value != null)
			{
				count++;
			}
		}

		return count;
	}

}
