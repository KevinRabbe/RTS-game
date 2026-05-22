using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsTownCenterPlacementDiagnostics
{
	public static string BuildPreviewCommandMismatchMessage(
		Vector2I tile,
		TcPlacementPreviewResult previewResult,
		GodotLocalPlayerDto player,
		int localPlayerIndex)
	{
		return "Preview/command mismatch tile=(" + tile.X + "," + tile.Y + ")"
			+ " preview=" + previewResult
			+ " command=PlaceTownCenter"
			+ " player=" + localPlayerIndex
			+ " wood=" + player.Wood
			+ " hasCapitalPlaced=" + player.HasCapitalBeenPlaced
			+ " connected=" + player.IsConnected
			+ " defeated=" + player.IsDefeated
			+ " resigned=" + player.IsResigned;
	}
}
