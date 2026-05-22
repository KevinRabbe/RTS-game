using RtsGame.Presentation.GodotBridge;

internal static class RtsCommandResultLogFormatter
{
	public static string BuildResultLogLine(
		GodotCommandResultKind result,
		int beforeExecuted,
		int afterExecuted,
		int beforeRejected,
		int afterRejected,
		GodotFrameDto? frame)
	{
		string rejectionDetail = string.Empty;
		if (frame != null)
		{
			GodotMatchDto match = frame.Match;
			rejectionDetail =
				" cmd=" + match.LastCommandTypeId
				+ " reason=" + match.LastCommandReasonId
				+ " accepted=" + (match.LastCommandAccepted ? "Y" : "N")
				+ " player=" + match.LastCommandPlayerIndex
				+ " targetEntity=" + match.LastCommandTargetEntityId
				+ " targetTile=(" + match.LastCommandTargetTileX + "," + match.LastCommandTargetTileY + ")"
				+ " unitCount=" + match.LastCommandUnitCount
				+ " firstUnit=" + match.LastCommandFirstUnitId;
		}

		return "result " + result
			+ " ex " + beforeExecuted + "->" + afterExecuted
			+ " rej " + beforeRejected + "->" + afterRejected
			+ rejectionDetail;
	}
}
