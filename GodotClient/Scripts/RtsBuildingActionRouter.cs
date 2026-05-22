using System;
using Godot;
using RtsGame.Presentation.GodotBridge;

internal static class RtsBuildingActionRouter
{
	internal static void TrainFromSelectedBuilding(
		GodotFrameDto? frame,
		int localPlayerIndex,
		int selectedBuildingId,
		int unitTypeId,
		Action<string> addDebugEvent,
		Action refreshFrame,
		Action<string, Action<GodotClientFacade>> queueCommandAndConfirm)
	{
		if (frame == null)
		{
			return;
		}

		if (!RtsBuildingActionGuard.CanTrainFromSelectedBuilding(frame, selectedBuildingId, unitTypeId, out GodotTrainActionState state))
		{
			if (selectedBuildingId == 0)
			{
				addDebugEvent("train blocked reason=building not selected unit=" + GodotBuildingDebugStatusBuilder.ResolveUnitTypeLabel(unitTypeId));
				return;
			}

			addDebugEvent(
				"train blocked reason=" + GodotBuildingDebugStatusBuilder.ResolveTrainBlockedReason(state)
				+ " building=" + selectedBuildingId
				+ " unit=" + GodotBuildingDebugStatusBuilder.ResolveUnitTypeLabel(unitTypeId));
			refreshFrame();
			return;
		}

		queueCommandAndConfirm(
			"train p=" + localPlayerIndex + " " + GodotBuildingDebugStatusBuilder.BuildTrainIntentText(selectedBuildingId, unitTypeId),
			facade => facade.QueueTrainUnit(localPlayerIndex, selectedBuildingId, unitTypeId));
	}

	internal static void ResearchFromSelectedBuilding(
		GodotFrameDto? frame,
		int localPlayerIndex,
		int selectedBuildingId,
		int techId,
		Action<string> addDebugEvent,
		Action refreshFrame,
		Action<string, Action<GodotClientFacade>> queueCommandAndConfirm)
	{
		if (frame == null)
		{
			return;
		}

		if (!RtsBuildingActionGuard.CanResearchInfantryAttackFromSelectedBuilding(frame, selectedBuildingId, out GodotResearchActionState state))
		{
			if (selectedBuildingId == 0)
			{
				return;
			}

			addDebugEvent("research blocked state=" + state + " building=" + selectedBuildingId + " tech=" + techId);
			refreshFrame();
			return;
		}

		queueCommandAndConfirm(
			"research p=" + localPlayerIndex + " building=" + selectedBuildingId + " tech=" + techId,
			facade => facade.QueueResearchTech(localPlayerIndex, selectedBuildingId, techId));
	}

	internal static void TryCreateTradeRoute(
		GodotFrameDto? frame,
		int localPlayerIndex,
		bool hasSelectedUnits,
		int[] selectedUnitIdsSorted,
		RtsTradeRouteSelection tradeRouteSelection,
		Vector2 screenPosition,
		Func<float, long> screenToRaw,
		Func<long, long, Vector2> toScreen,
		Func<int, long> tileToRaw,
		Func<Vector2, Vector2I> screenToTile,
		Action<string> addDebugEvent,
		Action refreshFrame,
		Action<string, Action<GodotClientFacade>> queueCommandAndConfirm,
		RtsCommandMarker commandMarker)
	{
		if (frame == null || !hasSelectedUnits)
		{
			return;
		}

		int tradeCartId = GodotTradeRouteRouter.FindSelectedTradeCart(frame, selectedUnitIdsSorted);
		if (tradeCartId == 0)
		{
			addDebugEvent("trade route blocked: no selected trade cart");
			return;
		}

		int tradePostId = GodotTradeRouteRouter.FindLocalTradePostAt(
			frame,
			localPlayerIndex,
			screenToRaw(screenPosition.X),
			screenToRaw(screenPosition.Y));

		if (tradePostId == 0)
		{
			addDebugEvent("trade route blocked: no local trade post under cursor");
			return;
		}

		if (tradeRouteSelection.TrySetFirstEndpoint(tradePostId))
		{
			addDebugEvent("trade route step A set to tradePost=" + tradePostId);
			refreshFrame();
			return;
		}

		if (!tradeRouteSelection.TryConsumeRoute(tradePostId, out int routeA))
		{
			refreshFrame();
			return;
		}

		queueCommandAndConfirm(
			"trade route p=" + localPlayerIndex + " cart=" + tradeCartId + " A=" + routeA + " B=" + tradePostId,
			facade => facade.QueueCreateTradeRoute(localPlayerIndex, tradeCartId, routeA, tradePostId));
		commandMarker.Set("TradeRoute", screenPosition, Colors.Gold);
	}
}
