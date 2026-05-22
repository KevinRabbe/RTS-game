using System;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    // Stage A/B wrapper: isolates policy seams while preserving deterministic legacy behavior.
    public sealed class MovementEngineV2
    {
        private readonly MovementV2PlanBuilder _planBuilder = new MovementV2PlanBuilder();
        private readonly MovementV2ConflictResolver _conflictResolver = new MovementV2ConflictResolver();
        private readonly MovementV2LocalAvoidanceSelector _localAvoidanceSelector = new MovementV2LocalAvoidanceSelector();
        private readonly MovementV2PlanApplier _planApplier = new MovementV2PlanApplier();
        private readonly MovementV2ProgressTracker _progressTracker = new MovementV2ProgressTracker();
        private readonly MovementV2RetargetPolicy _retargetPolicy = new MovementV2RetargetPolicy();

        public void Run(GameState state, GameRules rules, TickCommandContext commandContext, Action runLegacyPipeline)
        {
            _planBuilder.Prepare(state, rules);
            _retargetPolicy.BeforeRun(state, rules);
            _localAvoidanceSelector.BeforeRun(state, rules);

            // Stage A: preserve proven behavior while surfacing deterministic seam modules.
            runLegacyPipeline();

            _conflictResolver.AfterRun(state, rules);
            _planApplier.AfterRun(state, rules);
            _progressTracker.AfterRun(state, rules);
            _retargetPolicy.AfterRun(state, rules);
        }
    }

    internal sealed class MovementV2PlanBuilder
    {
        public void Prepare(GameState state, GameRules rules)
        {
        }
    }

    internal sealed class MovementV2ConflictResolver
    {
        public void AfterRun(GameState state, GameRules rules)
        {
        }
    }

    internal sealed class MovementV2LocalAvoidanceSelector
    {
        private static readonly DeterministicTrafficLanePreferenceScorerV2 ScorerV2 = new DeterministicTrafficLanePreferenceScorerV2();

        public void BeforeRun(GameState state, GameRules rules)
        {
            if (state.TrafficReservations is DeterministicTrafficReservationService traffic)
            {
                traffic.LanePreferenceScorer = ScorerV2;
            }
        }
    }

    internal sealed class MovementV2PlanApplier
    {
        public void AfterRun(GameState state, GameRules rules)
        {
        }
    }

    internal sealed class MovementV2ProgressTracker
    {
        public void AfterRun(GameState state, GameRules rules)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.LastMovedTick < 0)
                {
                    unit.LastNoProgressTicksWindow = 0;
                    continue;
                }

                int noProgressTicks = state.Tick - unit.LastMovedTick;
                unit.LastNoProgressTicksWindow = noProgressTicks > 0 ? noProgressTicks : 0;
            }
        }
    }

    internal sealed class MovementV2RetargetPolicy
    {
        public void BeforeRun(GameState state, GameRules rules)
        {
        }

        public void AfterRun(GameState state, GameRules rules)
        {
        }
    }
}
