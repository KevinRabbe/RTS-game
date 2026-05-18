using System.Collections.Generic;

namespace RtsGame.Sim.Core
{
    public sealed class DeterministicPathQueryService : IPathQueryService
    {
        public IPathCandidateShortlistStrategy? CandidateShortlistStrategy { get; set; }

        private readonly Dictionary<long, PathStepResult> _stepCache = new Dictionary<long, PathStepResult>();
        private readonly Dictionary<long, PathCostResult> _costCache = new Dictionary<long, PathCostResult>();
        private int _cacheTick = int.MinValue;

        public bool TryNextStep(GameState state, int unitId, int fromX, int fromY, int toX, int toY, int contextVersion, out int nextX, out int nextY)
        {
            EnsureTick(state.Tick);
            state.DebugCounters.PathFindNextCalls++;
            long key = ComposeKey(fromX, fromY, toX, toY, contextVersion);
            if (_stepCache.TryGetValue(key, out PathStepResult cached))
            {
                nextX = cached.NextX;
                nextY = cached.NextY;
                return cached.Found;
            }

            bool found = DeterministicPathfinder.TryFindNextTile(state, fromX, fromY, toX, toY, out nextX, out nextY);
            _stepCache[key] = new PathStepResult(found, nextX, nextY);
            return found;
        }

        public bool TryPathCost(GameState state, int fromX, int fromY, int toX, int toY, int contextVersion, out int cost)
        {
            EnsureTick(state.Tick);
            state.DebugCounters.PathFindCostCalls++;
            long key = ComposeKey(fromX, fromY, toX, toY, contextVersion);
            if (_costCache.TryGetValue(key, out PathCostResult cached))
            {
                cost = cached.Cost;
                return cached.Found;
            }

            bool found = DeterministicPathfinder.TryFindPathCost(state, fromX, fromY, toX, toY, out cost);
            _costCache[key] = new PathCostResult(found, cost);
            return found;
        }

        private void EnsureTick(int tick)
        {
            if (_cacheTick == tick)
            {
                return;
            }

            _cacheTick = tick;
            _stepCache.Clear();
            _costCache.Clear();
        }

        private static long ComposeKey(int fromX, int fromY, int toX, int toY, int contextVersion)
        {
            long key = contextVersion;
            key = (key * 73856093L) ^ fromX;
            key = (key * 19349663L) ^ fromY;
            key = (key * 83492791L) ^ toX;
            key = (key * 2654435761L) ^ toY;
            return key;
        }

        private readonly struct PathStepResult
        {
            public bool Found { get; }
            public int NextX { get; }
            public int NextY { get; }

            public PathStepResult(bool found, int nextX, int nextY)
            {
                Found = found;
                NextX = nextX;
                NextY = nextY;
            }
        }

        private readonly struct PathCostResult
        {
            public bool Found { get; }
            public int Cost { get; }

            public PathCostResult(bool found, int cost)
            {
                Found = found;
                Cost = cost;
            }
        }
    }
}
