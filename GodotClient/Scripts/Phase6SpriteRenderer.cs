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
	
	public void DrawTerrain(Node2D canvas, string mapName, int widthTiles, int heightTiles, float tilePixels)
	{
		if (mapName != "DryArabiaTest01")
		{
			return;
		}

		Texture2D? grass = _useSprites ? GetAsset(GodotSpriteAssetId.GrassTile) : null;
		Texture2D? dirt = _useSprites ? GetAsset(GodotSpriteAssetId.DirtTile) : null;
		Texture2D? rock = _useSprites ? GetAsset(GodotSpriteAssetId.RockBlocker) : null;

		float size = tilePixels;

		if (grass != null && GodotSpriteSheetLayout.TryGetMetadata(GodotSpriteAssetId.GrassTile, out var grassMetadata))
		{
			// Base layer: Calm sandy dry-ground color
			canvas.DrawRect(new Rect2(0, 0, widthTiles * size, heightTiles * size), new Color(0.82f, 0.75f, 0.55f));

			// --- Environmental Decoration Clusters ---
			float patchSize = size * 6.0f;
			
			// Scattered desert patches
			DrawSheetFrame(canvas, grass, grassMetadata, 0, new Rect2(12 * size, 15 * size, patchSize, patchSize));
			DrawSheetFrame(canvas, grass, grassMetadata, 1, new Rect2(85 * size, 22 * size, patchSize, patchSize));
			DrawSheetFrame(canvas, grass, grassMetadata, 2, new Rect2(42 * size, 75 * size, patchSize, patchSize));
			DrawSheetFrame(canvas, grass, grassMetadata, 0, new Rect2(105 * size, 65 * size, patchSize, patchSize));
			DrawSheetFrame(canvas, grass, grassMetadata, 1, new Rect2(20 * size, 55 * size, patchSize, patchSize));

			// --- Starting Area Polish (Base Zones) ---
			// Player 0 (Start area near 24, 48)
			DrawSheetFrame(canvas, grass, grassMetadata, 1, new Rect2(18 * size, 42 * size, patchSize * 1.2f, patchSize * 1.2f));
			DrawSheetFrame(canvas, grass, grassMetadata, 2, new Rect2(28 * size, 54 * size, patchSize, patchSize));
			
			// Player 1 (Start area near 104, 48)
			DrawSheetFrame(canvas, grass, grassMetadata, 1, new Rect2(110 * size, 42 * size, patchSize * 1.2f, patchSize * 1.2f));
			DrawSheetFrame(canvas, grass, grassMetadata, 2, new Rect2(98 * size, 54 * size, patchSize, patchSize));

			// --- Contested Center Identity ---
			if (dirt != null && GodotSpriteSheetLayout.TryGetMetadata(GodotSpriteAssetId.DirtTile, out var dirtMetadata))
			{
				int midY = heightTiles / 2;
				float pathSize = size * 4.0f;
				
				// Central trails
				DrawSheetFrame(canvas, dirt, dirtMetadata, 0, new Rect2(40 * size, (midY - 2) * size, pathSize, pathSize));
				DrawSheetFrame(canvas, dirt, dirtMetadata, 0, new Rect2(80 * size, (midY - 2) * size, pathSize, pathSize));
				
				// Center focus area (large dirt patch)
				DrawSheetFrame(canvas, dirt, dirtMetadata, 1, new Rect2(54 * size, (midY - 5) * size, size * 20, size * 10));
			}

			// --- Decorative rock clusters ---
			if (rock != null && GodotSpriteSheetLayout.TryGetMetadata(GodotSpriteAssetId.RockBlocker, out var rockMetadata))
			{
				float rockSize = size * 2.5f;
				
				// Center cluster (Contested identity)
				DrawSheetFrame(canvas, rock, rockMetadata, 0, new Rect2(58 * size, 42 * size, rockSize, rockSize));
				DrawSheetFrame(canvas, rock, rockMetadata, 1, new Rect2(66 * size, 50 * size, rockSize, rockSize));
				DrawSheetFrame(canvas, rock, rockMetadata, 2, new Rect2(62 * size, 46 * size, rockSize * 1.5f, rockSize * 1.5f));
				
				// Scatter rocks near start area borders
				DrawSheetFrame(canvas, rock, rockMetadata, 0, new Rect2(35 * size, 30 * size, rockSize, rockSize));
				DrawSheetFrame(canvas, rock, rockMetadata, 1, new Rect2(90 * size, 65 * size, rockSize, rockSize));
				
				// Corner outliers
				DrawSheetFrame(canvas, rock, rockMetadata, 0, new Rect2(10 * size, 80 * size, rockSize, rockSize));
				DrawSheetFrame(canvas, rock, rockMetadata, 1, new Rect2(110 * size, 10 * size, rockSize, rockSize));
			}
		}
		else
		{
			// Primitive fallback
			canvas.DrawRect(new Rect2(0, 0, widthTiles * size, heightTiles * size), new Color(0.76f, 0.70f, 0.50f));
			// Simple path line
			canvas.DrawLine(new Vector2(32 * size, (heightTiles / 2.0f) * size), new Vector2(96 * size, (heightTiles / 2.0f) * size), new Color(0.6f, 0.5f, 0.3f), 2.0f);
		}
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

		GodotBuildingStatusDto? status = FindBuildingStatus(frame, primitive.EntityId);

		// Priority 1: Construction Scaffold
		if (status != null && status.IsUnderConstruction)
		{
			Texture2D? scaffold = GetAsset(GodotSpriteAssetId.BuildingScaffold);
			if (scaffold != null)
			{
				Rect2 target = GetBuildingSpriteRect(primitive, 2.4f, toScreen, rawToPixels);
				canvas.DrawTextureRect(scaffold, target, false);
				if (selectedBuildingId == primitive.EntityId)
				{
					canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
				}
				return true;
			}
		}

		// Priority 2: Special Wall Sheet
		if (primitive.TypeId == (int)BuildingTypeId.Wall &&
			GodotSpriteSheetLayout.TryResolveBuildingAsset(primitive.TypeId, out GodotSpriteAssetId wallAssetId) &&
			GodotSpriteSheetLayout.TryGetMetadata(wallAssetId, out GodotSpriteSheetMetadata wallMetadata))
		{
			Texture2D? wallSheet = GetAsset(wallAssetId);
			if (wallSheet != null)
			{
				Rect2 target = GetBuildingSpriteRect(primitive, 2.7f, toScreen, rawToPixels);
				int frameIndex = wallMetadata.DefaultFrameIndex;
				DrawSheetFrame(canvas, wallSheet, wallMetadata, frameIndex, target);
				if (selectedBuildingId == primitive.EntityId)
				{
					canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
				}
				return true;
			}
		}

		// Priority 3: Capital vs TownCenter vs Other
		GodotSpriteAssetId assetId;
		float scale = 2.6f;
		if (primitive.TypeId == (int)BuildingTypeId.TownCenter)
		{
			assetId = primitive.IsCapital ? GodotSpriteAssetId.Capital : GodotSpriteAssetId.TownCenter;
			scale = primitive.IsCapital ? 3.8f : 3.2f;
		}
		else if (!GodotSpriteSheetLayout.TryResolveBuildingAsset(primitive.TypeId, out assetId))
		{
			return false;
		}

		Texture2D? sprite = GetAsset(assetId);
		if (sprite != null)
		{
			Rect2 target = GetBuildingSpriteRect(primitive, scale, toScreen, rawToPixels);
			canvas.DrawTextureRect(sprite, target, false);
			if (selectedBuildingId == primitive.EntityId)
			{
				canvas.DrawRect(target.Grow(2.0f), Colors.White, false, 2.0f);
			}
			return true;
		}

		return false;
	}

	public bool TryDrawResource(
		Node2D canvas,
		GodotPrimitiveDto primitive,
		System.Func<long, long, Vector2> toScreen,
		System.Func<long, float> rawToPixels)
	{
		if (!_useSprites)
		{
			return false;
		}

		if (!GodotSpriteSheetLayout.TryResolveResourceAsset(primitive.TypeId, out GodotSpriteAssetId assetId))
		{
			return false;
		}

		Texture2D? sprite = GetAsset(assetId);
		if (sprite == null)
		{
			return false;
		}

		Vector2 center = toScreen(primitive.XRaw, primitive.YRaw);
		float worldSize = rawToPixels(primitive.SizeRaw);
		float size = Mathf.Max(38.0f, worldSize * 2.4f);
		var target = new Rect2(center.X - size * 0.5f, center.Y - size * 0.70f, size, size);
		canvas.DrawTextureRect(sprite, target, false);
		return true;
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
		float size = Mathf.Max(42.0f, worldSize * 3.8f);
		return new Rect2(center.X - size * 0.5f, center.Y - size * 0.85f, size, size);
	}

	private static Rect2 GetBuildingSpriteRect(GodotPrimitiveDto primitive, float scale, System.Func<long, long, Vector2> toScreen, System.Func<long, float> rawToPixels)
	{
		Vector2 center = toScreen(primitive.XRaw, primitive.YRaw);
		float worldSize = rawToPixels(primitive.SizeRaw);
		float size = Mathf.Max(80.0f, worldSize * scale);
		return new Rect2(center.X - size * 0.5f, center.Y - size * 0.80f, size, size);
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
