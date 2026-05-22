using System;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Systems
{
    // Stage A/B wrapper for gather policy seams; delegates to legacy core path until V2 upgrades are promoted.
    public sealed class GatherEngineV2
    {
        private readonly GatherV2IntentResolver _intentResolver = new GatherV2IntentResolver();
        private readonly GatherV2ApproachController _approachController = new GatherV2ApproachController();
        private readonly GatherV2ReservationRecoveryPolicy _reservationRecovery = new GatherV2ReservationRecoveryPolicy();
        private readonly GatherV2Execution _execution = new GatherV2Execution();
        private readonly GatherV2ContinuationPolicy _continuationPolicy = new GatherV2ContinuationPolicy();

        public void Run(GameState state, GameRules rules, TickCommandContext commandContext, Action runLegacyPipeline)
        {
            _intentResolver.Prepare(state, rules);
            _approachController.Prepare(state, rules);
            _reservationRecovery.Prepare(state, rules);

            // Stage A: keep behavior parity while explicit seams are introduced.
            runLegacyPipeline();

            _execution.AfterRun(state, rules);
            _continuationPolicy.AfterRun(state, rules);
        }
    }

    internal sealed class GatherV2IntentResolver
    {
        public void Prepare(GameState state, GameRules rules)
        {
        }
    }

    internal sealed class GatherV2ApproachController
    {
        public void Prepare(GameState state, GameRules rules)
        {
        }
    }

    internal sealed class GatherV2ReservationRecoveryPolicy
    {
        public void Prepare(GameState state, GameRules rules)
        {
        }
    }

    internal sealed class GatherV2Execution
    {
        public void AfterRun(GameState state, GameRules rules)
        {
        }
    }

    internal sealed class GatherV2ContinuationPolicy
    {
        public void AfterRun(GameState state, GameRules rules)
        {
        }
    }
}
