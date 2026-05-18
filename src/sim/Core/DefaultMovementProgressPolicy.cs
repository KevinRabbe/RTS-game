using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public sealed class DefaultMovementProgressPolicy : IMovementProgressPolicy
    {
        public IMovementSteeringPolicy? SteeringPolicy { get; set; }

        public bool ShouldRecordProgressTick(Unit unit, bool entersNewTile, bool reachesTarget)
        {
            if (unit.TaskPhase != WorkerTaskPhase.MovingToCommandMove)
            {
                return true;
            }

            return entersNewTile || reachesTarget;
        }

        public bool IsNoProgressTimedOut(GameState state, Unit unit)
        {
            int blockedTicks = unit.LastMovedTick < 0 ? int.MaxValue : state.Tick - unit.LastMovedTick;
            return blockedTicks >= GameData.NoProgressTimeoutTicks;
        }
    }
}
