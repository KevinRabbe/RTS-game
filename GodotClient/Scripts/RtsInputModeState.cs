internal enum RtsInputModeKind
{
	Normal = 0,
	AttackMoveTargeting = 1,
	TownCenterPlacement = 2,
	TradeRouteTargeting = 3
}

internal sealed class RtsInputModeState
{
	public RtsInputModeKind CurrentMode { get; private set; } = RtsInputModeKind.Normal;

	public bool IsAttackMoveTargeting
	{
		get { return CurrentMode == RtsInputModeKind.AttackMoveTargeting; }
	}

	public bool IsTownCenterPlacement
	{
		get { return CurrentMode == RtsInputModeKind.TownCenterPlacement; }
	}

	public void EnterAttackMoveTargeting()
	{
		CurrentMode = RtsInputModeKind.AttackMoveTargeting;
	}

	public void EnterTownCenterPlacement()
	{
		CurrentMode = RtsInputModeKind.TownCenterPlacement;
	}

	public void EnterTradeRouteTargeting()
	{
		CurrentMode = RtsInputModeKind.TradeRouteTargeting;
	}

	public void ExitToNormal()
	{
		CurrentMode = RtsInputModeKind.Normal;
	}
}
