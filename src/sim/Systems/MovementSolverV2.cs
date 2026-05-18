using RtsGame.Sim.Data;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Systems
{
    /// <summary>
    /// Deterministic movement-v2 extension scaffold.
    /// This class is intentionally low-impact while the rewrite window is in setup mode.
    /// Future milestones replace internals, but callers keep stable contracts.
    /// </summary>
    public sealed class MovementSolverV2
    {
        public bool IsEnabledFor(GameRules rules, Unit unit)
        {
            return unit.UnitTypeId == UnitTypeId.Villager && rules.EnableMovementSolverV2Villagers;
        }

        public void OnBlocked(GameState state, Unit unit, MovementBlockReason reason)
        {
            unit.MovementBlockedReason = reason;
            if (unit.BlockedSinceTick == 0)
            {
                unit.BlockedSinceTick = state.Tick;
            }
        }

        public void OnProgress(GameState state, Unit unit)
        {
            unit.MovementBlockedReason = MovementBlockReason.None;
            unit.BlockedSinceTick = 0;
            unit.LastMeaningfulProgressTick = state.Tick;
        }

        public void OnIdle(Unit unit)
        {
            unit.MovementBlockedReason = MovementBlockReason.None;
            unit.BlockedSinceTick = 0;
        }
    }
}
