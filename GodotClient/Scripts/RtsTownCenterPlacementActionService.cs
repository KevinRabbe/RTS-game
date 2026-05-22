using System;
using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsTownCenterPlacementActionService
{
	internal static void ConfirmPlacement(
		GodotClientFacade? facade,
		GodotFrameDto? frame,
		RtsTownCenterPlacementState placementState,
		int localPlayerIndex,
		Vector2I tile,
		Func<int, long> tileToRaw,
		Func<long, long, Vector2> toScreen,
		Action<string> addDebugEvent,
		Action queueRedraw,
		Action<string, Action<GodotClientFacade>> queueCommandAndConfirm,
		RtsCommandMarker commandMarker,
		Func<int> getExecutedCount,
		Func<int> getRejectedCount)
	{
		if (facade == null || frame == null)
		{
			return;
		}

		placementState.Cancel();
		GodotFrameDto frameBefore = frame;
		TcPlacementPreviewResult previewBefore = placementState.PreviewResult;

		if (placementState.PreviewResult != TcPlacementPreviewResult.Valid)
		{
			addDebugEvent("TC placement rejected by preview: " + placementState.PreviewResult);
			queueRedraw();
			return;
		}

		commandMarker.Set("TC", toScreen(tileToRaw(tile.X), tileToRaw(tile.Y)), Colors.LightBlue);
		int beforeExecuted = frameBefore.Match.ExecutedCommandCount;
		int beforeRejected = frameBefore.Match.RejectedCommandCount;
		queueCommandAndConfirm(
			"place town center p=" + localPlayerIndex + " tile=(" + tile.X + "," + tile.Y + ")",
			f => f.QueuePlaceTownCenter(localPlayerIndex, tile.X, tile.Y));

		int afterExecuted = getExecutedCount();
		int afterRejected = getRejectedCount();
		GodotCommandResultKind result = GodotCommandResultClassifier.Classify(beforeExecuted, beforeRejected, afterExecuted, afterRejected);
		if (previewBefore == TcPlacementPreviewResult.Valid && result == GodotCommandResultKind.Rejected)
		{
			addDebugEvent(
				RtsTownCenterPlacementDiagnostics.BuildPreviewCommandMismatchMessage(
					tile,
					previewBefore,
					frameBefore.LocalPlayer,
					localPlayerIndex));
		}
	}
}
