using System;
using RtsGame.Presentation.GodotBridge;

internal static class RtsSessionBootstrap
{
	internal static GodotClientFacade StartLocalMatch(ulong seed, int playerCount)
	{
		return GodotClientFacade.CreateLocal(seed, playerCount);
	}

	internal static GodotClientFacade StartDryArabiaTest01(ulong seed)
	{
		return GodotClientFacade.CreateDryArabiaTest01(seed);
	}

	internal static GodotClientFacade StartCombatTest01(ulong seed)
	{
		return GodotClientFacade.CreateCombatTest01(seed);
	}

	internal static void ResetRuntimeState(
		GodotClientFacade facade,
		RtsSelectionController selectionController,
		RtsTradeRouteSelection tradeRouteSelection,
		RtsTownCenterPlacementState tcPlacementState,
		RtsCameraController cameraController,
		Action refreshFrame,
		ref int hoveredResourceNodeId,
		ref double tickAccumulator,
		ref bool paused)
	{
		selectionController.Reset();
		tradeRouteSelection.Clear();
		hoveredResourceNodeId = 0;
		tcPlacementState.Reset();
		cameraController.EndMiddleDrag();
		tickAccumulator = 0.0;
		paused = false;
		facade.AdvanceOneTick();
		refreshFrame();
	}
}
