using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsVisualStyleColors
{
	public static Color Resolve(GodotVisualStyle style)
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
}
