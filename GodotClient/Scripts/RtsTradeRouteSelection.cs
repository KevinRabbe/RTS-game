internal sealed class RtsTradeRouteSelection
{
	private int _pendingTradePostAId;

	public void Clear()
	{
		_pendingTradePostAId = 0;
	}

	public bool TrySetFirstEndpoint(int tradePostId)
	{
		if (_pendingTradePostAId == 0 || _pendingTradePostAId == tradePostId)
		{
			_pendingTradePostAId = tradePostId;
			return true;
		}

		return false;
	}

	public bool TryConsumeRoute(int tradePostIdB, out int tradePostIdA)
	{
		if (_pendingTradePostAId == 0 || _pendingTradePostAId == tradePostIdB)
		{
			tradePostIdA = 0;
			return false;
		}

		tradePostIdA = _pendingTradePostAId;
		_pendingTradePostAId = 0;
		return true;
	}
}
