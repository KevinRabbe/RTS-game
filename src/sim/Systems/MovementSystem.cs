using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Systems
{
    public sealed class MovementSystem : ISimSystem
    {
        private static readonly int[] AlternateOffsetX = new[] { 1, 0, -1, 0 };
        private static readonly int[] AlternateOffsetY = new[] { 0, 1, 0, -1 };

        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            MovementPlan[] plans = BuildPlans(state);
            MarkSharedDestinationConflicts(plans);
            MarkSwapConflicts(plans);
            MarkBlockedByStationaryUnits(state, plans);
            ApplyPlans(state, plans);
        }

        private static MovementPlan[] BuildPlans(GameState state)
        {
            var plans = new MovementPlan[state.EntityState.Units.Count];
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                var plan = new MovementPlan(i, unit.Id, unit.Position, unit.Position, false);
                plans[i] = plan;
                if (unit.IsDead || !unit.HasMoveTarget)
                {
                    continue;
                }

                Fixed speed = GameData.GetUnitMoveSpeed(unit.UnitTypeId);
                if (speed.Raw <= 0)
                {
                    plan.ShouldClearTarget = true;
                    plans[i] = plan;
                    continue;
                }

                FixedVector2 delta = unit.MoveTarget - unit.Position;
                int currentTileX = SpatialRules.GetTileX(unit.Position);
                int currentTileY = SpatialRules.GetTileY(unit.Position);
                int targetTileX = SpatialRules.GetTileX(unit.MoveTarget);
                int targetTileY = SpatialRules.GetTileY(unit.MoveTarget);
                bool nextPathTileIsTarget = true;
                int intendedNextTileX = targetTileX;
                int intendedNextTileY = targetTileY;
                if (currentTileX != targetTileX || currentTileY != targetTileY)
                {
                    if (!DeterministicPathfinder.TryFindNextTile(state, currentTileX, currentTileY, targetTileX, targetTileY, out int nextTileX, out int nextTileY))
                    {
                        plan.ShouldClearTarget = true;
                        plans[i] = plan;
                        continue;
                    }

                    nextPathTileIsTarget = nextTileX == targetTileX && nextTileY == targetTileY;
                    intendedNextTileX = nextTileX;
                    intendedNextTileY = nextTileY;
                    delta = FixedVector2.FromInts(nextTileX, nextTileY) - unit.Position;
                }

                long distanceRaw = DeterministicMath.SqrtRaw(delta.LengthSquaredRaw());
                if (distanceRaw == 0 || (nextPathTileIsTarget && distanceRaw <= speed.Raw))
                {
                    plan.AttemptsMove = true;
                    plan.NextPosition = unit.MoveTarget;
                    plan.WillReachTarget = true;
                    plans[i] = plan;
                    continue;
                }

                Fixed distance = new Fixed(distanceRaw);
                Fixed stepScale = speed / distance;
                FixedVector2 nextPosition = unit.Position + FixedVector2.Multiply(delta, stepScale);
                int projectedTileX = SpatialRules.GetTileX(nextPosition);
                int projectedTileY = SpatialRules.GetTileY(nextPosition);
                if (SpatialRules.IsTileBlockedForUnitMovement(state, projectedTileX, projectedTileY))
                {
                    plan.ShouldClearTarget = true;
                    plans[i] = plan;
                    continue;
                }

                if (projectedTileX != currentTileX
                    || projectedTileY != currentTileY)
                {
                    if (SpatialRules.IsTileOccupiedByLiveUnit(state, projectedTileX, projectedTileY, unit.Id)
                        && !nextPathTileIsTarget
                        && TryBuildAlternateStepPlan(
                            state,
                            unit,
                            currentTileX,
                            currentTileY,
                            targetTileX,
                            targetTileY,
                            intendedNextTileX,
                            intendedNextTileY,
                            speed,
                            out FixedVector2 alternatePosition))
                    {
                        nextPosition = alternatePosition;
                    }
                }

                plan.AttemptsMove = true;
                plan.NextPosition = nextPosition;
                plans[i] = plan;
            }

            return plans;
        }

        private static bool TryBuildAlternateStepPlan(
            GameState state,
            Unit unit,
            int currentTileX,
            int currentTileY,
            int targetTileX,
            int targetTileY,
            int intendedNextTileX,
            int intendedNextTileY,
            Fixed speed,
            out FixedVector2 nextPosition)
        {
            nextPosition = unit.Position;
            int bestX = 0;
            int bestY = 0;
            int bestDistance = int.MaxValue;
            int bestTurnCost = int.MaxValue;
            bool found = false;

            for (int i = 0; i < AlternateOffsetX.Length; i++)
            {
                int candidateX = currentTileX + AlternateOffsetX[i];
                int candidateY = currentTileY + AlternateOffsetY[i];
                if (!IsValidAlternateTile(state, unit, candidateX, candidateY))
                {
                    continue;
                }

                if (!DeterministicPathfinder.TryFindNextTile(state, candidateX, candidateY, targetTileX, targetTileY, out _, out _))
                {
                    continue;
                }

                int distance = Abs(candidateX - targetTileX) + Abs(candidateY - targetTileY);
                int turnCost = Abs(candidateX - intendedNextTileX) + Abs(candidateY - intendedNextTileY);
                if (!found
                    || distance < bestDistance
                    || (distance == bestDistance && turnCost < bestTurnCost)
                    || (distance == bestDistance && turnCost == bestTurnCost && CompareTile(candidateX, candidateY, bestX, bestY) < 0))
                {
                    bestX = candidateX;
                    bestY = candidateY;
                    bestDistance = distance;
                    bestTurnCost = turnCost;
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            FixedVector2 alternateTarget = FixedVector2.FromInts(bestX, bestY);
            FixedVector2 delta = alternateTarget - unit.Position;
            long distanceRaw = DeterministicMath.SqrtRaw(delta.LengthSquaredRaw());
            if (distanceRaw == 0 || distanceRaw <= speed.Raw)
            {
                nextPosition = alternateTarget;
                return true;
            }

            Fixed stepScale = speed / new Fixed(distanceRaw);
            nextPosition = unit.Position + FixedVector2.Multiply(delta, stepScale);
            return true;
        }

        private static bool IsValidAlternateTile(GameState state, Unit unit, int tileX, int tileY)
        {
            return !SpatialRules.IsTileBlockedForUnitMovement(state, tileX, tileY)
                && !SpatialRules.IsTileOccupiedByLiveUnit(state, tileX, tileY, unit.Id)
                && !SpatialRules.IsTileReservedByLiveUnit(state, tileX, tileY, unit.Id);
        }

        private static void MarkBlockedByStationaryUnits(GameState state, MovementPlan[] plans)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                if (!plans[i].AttemptsMove || !plans[i].EntersNewTile)
                {
                    continue;
                }

                for (int otherIndex = 0; otherIndex < state.EntityState.Units.Count; otherIndex++)
                {
                    if (otherIndex == plans[i].UnitIndex)
                    {
                        continue;
                    }

                    Unit other = state.EntityState.Units[otherIndex];
                    if (other.IsDead)
                    {
                        continue;
                    }

                    if (plans[i].NextTileX == SpatialRules.GetTileX(other.Position)
                        && plans[i].NextTileY == SpatialRules.GetTileY(other.Position))
                    {
                        plans[i].Blocked = true;
                        break;
                    }
                }
            }
        }

        private static void MarkSharedDestinationConflicts(MovementPlan[] plans)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                if (!plans[i].AttemptsMove || plans[i].Blocked || !plans[i].EntersNewTile)
                {
                    continue;
                }

                int winnerIndex = i;
                int contenders = 0;
                for (int other = 0; other < plans.Length; other++)
                {
                    if (!plans[other].AttemptsMove || plans[other].Blocked || !plans[other].EntersNewTile)
                    {
                        continue;
                    }

                    if (plans[i].NextTileX == plans[other].NextTileX && plans[i].NextTileY == plans[other].NextTileY)
                    {
                        contenders++;
                        if (plans[other].UnitId < plans[winnerIndex].UnitId)
                        {
                            winnerIndex = other;
                        }
                    }
                }

                if (contenders > 1)
                {
                    for (int other = 0; other < plans.Length; other++)
                    {
                        if (plans[other].AttemptsMove
                            && !plans[other].Blocked
                            && plans[other].EntersNewTile
                            && plans[i].NextTileX == plans[other].NextTileX
                            && plans[i].NextTileY == plans[other].NextTileY
                            && other != winnerIndex)
                        {
                            plans[other].Blocked = true;
                        }
                    }
                }
            }
        }

        private static void MarkSwapConflicts(MovementPlan[] plans)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                if (!plans[i].AttemptsMove || plans[i].Blocked || !plans[i].EntersNewTile)
                {
                    continue;
                }

                for (int other = i + 1; other < plans.Length; other++)
                {
                    if (!plans[other].AttemptsMove || plans[other].Blocked || !plans[other].EntersNewTile)
                    {
                        continue;
                    }

                    bool swaps = plans[i].CurrentTileX == plans[other].NextTileX
                        && plans[i].CurrentTileY == plans[other].NextTileY
                        && plans[other].CurrentTileX == plans[i].NextTileX
                        && plans[other].CurrentTileY == plans[i].NextTileY;
                    if (swaps)
                    {
                        plans[i].Blocked = true;
                        plans[other].Blocked = true;
                    }
                }
            }
        }

        private static void ApplyPlans(GameState state, MovementPlan[] plans)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                Unit unit = state.EntityState.Units[plans[i].UnitIndex];
                if (plans[i].ShouldClearTarget)
                {
                    unit.HasMoveTarget = false;
                    unit.TaskPhase = GetPhaseAfterClearedMove(unit.TaskPhase);
                    continue;
                }

                if (plans[i].Blocked)
                {
                    unit.TaskPhase = GetPhaseAfterBlockedMove(state, unit);
                    continue;
                }

                if (!plans[i].AttemptsMove)
                {
                    continue;
                }

                unit.Position = plans[i].NextPosition;
                unit.LastMovedTick = state.Tick;
                if (plans[i].WillReachTarget)
                {
                    unit.HasMoveTarget = false;
                    unit.TaskPhase = GetPhaseAfterArrivedMove(unit.TaskPhase);
                }
            }
        }

        private static WorkerTaskPhase GetPhaseAfterClearedMove(WorkerTaskPhase phase)
        {
            if (phase == WorkerTaskPhase.MovingToCommandMove)
            {
                return WorkerTaskPhase.Idle;
            }

            if (IsWorkerTaskMovementPhase(phase))
            {
                return WorkerTaskPhase.BlockedWaiting;
            }

            return phase;
        }

        private static WorkerTaskPhase GetPhaseAfterBlockedMove(GameState state, Unit unit)
        {
            if (!IsWorkerTaskMovementPhase(unit.TaskPhase))
            {
                return unit.TaskPhase;
            }

            int blockedTicks = unit.LastMovedTick < 0 ? int.MaxValue : state.Tick - unit.LastMovedTick;
            return blockedTicks >= GameData.InteractionTargetRetargetBlockedTicks ? WorkerTaskPhase.BlockedWaiting : unit.TaskPhase;
        }

        private static WorkerTaskPhase GetPhaseAfterArrivedMove(WorkerTaskPhase phase)
        {
            return phase == WorkerTaskPhase.MovingToCommandMove ? WorkerTaskPhase.Idle : phase;
        }

        private static bool IsWorkerTaskMovementPhase(WorkerTaskPhase phase)
        {
            return phase == WorkerTaskPhase.MovingToResourceSlot
                || phase == WorkerTaskPhase.MovingToDropoffSlot
                || phase == WorkerTaskPhase.MovingToBuildSlot
                || phase == WorkerTaskPhase.BlockedWaiting;
        }

        private static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        private static int CompareTile(int leftX, int leftY, int rightX, int rightY)
        {
            int yCompare = leftY.CompareTo(rightY);
            return yCompare != 0 ? yCompare : leftX.CompareTo(rightX);
        }

        private struct MovementPlan
        {
            public int UnitIndex;
            public int UnitId;
            public FixedVector2 CurrentPosition;
            public FixedVector2 NextPosition;
            public bool AttemptsMove;
            public bool WillReachTarget;
            public bool ShouldClearTarget;
            public bool Blocked;

            public MovementPlan(int unitIndex, int unitId, FixedVector2 currentPosition, FixedVector2 nextPosition, bool attemptsMove)
            {
                UnitIndex = unitIndex;
                UnitId = unitId;
                CurrentPosition = currentPosition;
                NextPosition = nextPosition;
                AttemptsMove = attemptsMove;
                WillReachTarget = false;
                ShouldClearTarget = false;
                Blocked = false;
            }

            public int CurrentTileX
            {
                get { return SpatialRules.GetTileX(CurrentPosition); }
            }

            public int CurrentTileY
            {
                get { return SpatialRules.GetTileY(CurrentPosition); }
            }

            public int NextTileX
            {
                get { return SpatialRules.GetTileX(NextPosition); }
            }

            public int NextTileY
            {
                get { return SpatialRules.GetTileY(NextPosition); }
            }

            public bool EntersNewTile
            {
                get { return CurrentTileX != NextTileX || CurrentTileY != NextTileY; }
            }
        }
    }
}
