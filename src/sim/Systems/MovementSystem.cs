using System.Collections.Generic;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Systems
{
    public sealed class MovementSystem : ISimSystem
    {
        private static readonly int[] AlternateOffsetX = new[] { 1, 0, -1, 0, 1, 1, -1, -1 };
        private static readonly int[] AlternateOffsetY = new[] { 0, 1, 0, -1, 1, -1, 1, -1 };
        private readonly MovementSolverV2 movementSolverV2 = new MovementSolverV2();

        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            MovementContext context = MovementContext.Create(state);
            MovementPlan[] plans = BuildPlans(state, context, movementSolverV2);
            MarkSharedDestinationConflicts(plans);
            MarkSwapConflicts(plans);
            MarkBlockedByStationaryUnits(context, plans);
            ApplyPlans(state, plans, movementSolverV2);
        }

        private static MovementPlan[] BuildPlans(GameState state, MovementContext context, MovementSolverV2 solverV2)
        {
            var plans = new MovementPlan[state.EntityState.Units.Count];
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                var plan = new MovementPlan(i, unit.Id, unit.Position, unit.Position, false);
                plans[i] = plan;
                if (unit.IsDead || !unit.HasMoveTarget)
                {
                    solverV2.OnIdle(unit);
                    continue;
                }

                Fixed speed = GameData.GetUnitMoveSpeed(unit.UnitTypeId);
                if (speed.Raw <= 0)
                {
                    plan.ShouldClearTarget = true;
                    plans[i] = plan;
                    solverV2.OnBlocked(state, unit, MovementBlockReason.NoPath);
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
                    if (!state.PathQueries.TryNextStep(state, unit.Id, currentTileX, currentTileY, targetTileX, targetTileY, state.Tick, out int nextTileX, out int nextTileY))
                    {
                        if (IsWorkerTaskMovementPhase(unit.TaskPhase))
                        {
                            plan.Blocked = true;
                            solverV2.OnBlocked(state, unit, MovementBlockReason.NoPath);
                        }
                        else
                        {
                            plan.ShouldClearTarget = true;
                            solverV2.OnBlocked(state, unit, MovementBlockReason.NoPath);
                        }
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
                    if (IsWorkerTaskMovementPhase(unit.TaskPhase))
                    {
                        plan.Blocked = true;
                        solverV2.OnBlocked(state, unit, MovementBlockReason.StaticBlocked);
                    }
                    else
                    {
                        plan.ShouldClearTarget = true;
                        solverV2.OnBlocked(state, unit, MovementBlockReason.StaticBlocked);
                    }
                    plans[i] = plan;
                    continue;
                }

                if (projectedTileX != currentTileX
                    || projectedTileY != currentTileY)
                {
                    if (context.IsTileOccupied(projectedTileX, projectedTileY, unit.Id)
                        && !context.IsOccupyingUnitMoving(projectedTileX, projectedTileY, unit.Id)
                        && TryBuildAlternateStepPlan(
                            state,
                            context,
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
            MovementContext context,
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
            int bestStepClass = int.MaxValue;
            int bestCongestion = int.MaxValue;
            int bestTurnCost = int.MaxValue;
            bool found = false;

            for (int i = 0; i < AlternateOffsetX.Length; i++)
            {
                int candidateX = currentTileX + AlternateOffsetX[i];
                int candidateY = currentTileY + AlternateOffsetY[i];
                if (!IsValidAlternateTile(state, context, unit, candidateX, candidateY))
                {
                    continue;
                }

                if (!state.PathQueries.TryNextStep(state, unit.Id, candidateX, candidateY, targetTileX, targetTileY, state.Tick, out _, out _))
                {
                    continue;
                }

                int distance = Abs(candidateX - targetTileX) + Abs(candidateY - targetTileY);
                int stepClass = i < 4 ? 0 : 1;
                int congestion = context.CountNearbyTraffic(candidateX, candidateY, unit.Id);
                int turnCost = Abs(candidateX - intendedNextTileX) + Abs(candidateY - intendedNextTileY);
                if (!found
                    || stepClass < bestStepClass
                    || (stepClass == bestStepClass && distance < bestDistance)
                    || (stepClass == bestStepClass && distance == bestDistance && congestion < bestCongestion)
                    || (stepClass == bestStepClass && distance == bestDistance && congestion == bestCongestion && turnCost < bestTurnCost)
                    || (stepClass == bestStepClass && distance == bestDistance && congestion == bestCongestion && turnCost == bestTurnCost && CompareTile(candidateX, candidateY, bestX, bestY) < 0))
                {
                    bestX = candidateX;
                    bestY = candidateY;
                    bestDistance = distance;
                    bestStepClass = stepClass;
                    bestCongestion = congestion;
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

        private static bool IsValidAlternateTile(GameState state, MovementContext context, Unit unit, int tileX, int tileY)
        {
            return !SpatialRules.IsTileBlockedForUnitMovement(state, tileX, tileY)
                && !context.IsTileOccupied(tileX, tileY, unit.Id)
                && !context.IsTileReserved(tileX, tileY, unit.Id);
        }

        private static void MarkBlockedByStationaryUnits(MovementContext context, MovementPlan[] plans)
        {
            var memo = new int[plans.Length];
            for (int i = 0; i < plans.Length; i++)
            {
                if (plans[i].Blocked || !plans[i].AttemptsMove || !plans[i].EntersNewTile)
                {
                    continue;
                }

                if (!CanPlanEnterNextTile(context, plans, i, memo))
                {
                    plans[i].Blocked = true;
                }
            }
        }

        private static bool CanPlanEnterNextTile(MovementContext context, MovementPlan[] plans, int planIndex, int[] memo)
        {
            if (planIndex < 0 || planIndex >= plans.Length)
            {
                return false;
            }

            MovementPlan plan = plans[planIndex];
            if (plan.Blocked || !plan.AttemptsMove || !plan.EntersNewTile)
            {
                return false;
            }

            if (memo[planIndex] == 1)
            {
                return true;
            }

            if (memo[planIndex] == 2 || memo[planIndex] == 3)
            {
                return false;
            }

            memo[planIndex] = 3;
            if (!context.TryGetOccupyingUnitId(plan.NextTileX, plan.NextTileY, plan.UnitId, out int occupantUnitId))
            {
                memo[planIndex] = 1;
                return true;
            }

            if (!context.TryGetPlanIndex(occupantUnitId, out int occupantPlanIndex)
                || !CanPlanEnterNextTile(context, plans, occupantPlanIndex, memo))
            {
                memo[planIndex] = 2;
                return false;
            }

            memo[planIndex] = 1;
            return true;
        }

        private static void MarkSharedDestinationConflicts(MovementPlan[] plans)
        {
            var contenderCounts = new Dictionary<int, int>();
            var winnerIndices = new Dictionary<int, int>();
            for (int i = 0; i < plans.Length; i++)
            {
                if (!plans[i].AttemptsMove || plans[i].Blocked || !plans[i].EntersNewTile)
                {
                    continue;
                }

                int key = EncodeTileKey(plans[i].NextTileX, plans[i].NextTileY);
                contenderCounts.TryGetValue(key, out int count);
                contenderCounts[key] = count + 1;
                if (!winnerIndices.TryGetValue(key, out int winnerIndex) || plans[i].UnitId < plans[winnerIndex].UnitId)
                {
                    winnerIndices[key] = i;
                }
            }

            for (int i = 0; i < plans.Length; i++)
            {
                if (!plans[i].AttemptsMove || plans[i].Blocked || !plans[i].EntersNewTile)
                {
                    continue;
                }

                int key = EncodeTileKey(plans[i].NextTileX, plans[i].NextTileY);
                if (contenderCounts[key] > 1 && winnerIndices[key] != i)
                {
                    plans[i].Blocked = true;
                }
            }
        }

        private static void MarkSwapConflicts(MovementPlan[] plans)
        {
            var planByCurrentTile = new Dictionary<int, int>();
            for (int i = 0; i < plans.Length; i++)
            {
                if (plans[i].AttemptsMove && !plans[i].Blocked && plans[i].EntersNewTile)
                {
                    planByCurrentTile[EncodeTileKey(plans[i].CurrentTileX, plans[i].CurrentTileY)] = i;
                }
            }

            for (int i = 0; i < plans.Length; i++)
            {
                if (!plans[i].AttemptsMove || plans[i].Blocked || !plans[i].EntersNewTile)
                {
                    continue;
                }

                int nextKey = EncodeTileKey(plans[i].NextTileX, plans[i].NextTileY);
                if (!planByCurrentTile.TryGetValue(nextKey, out int other) || other <= i)
                {
                    continue;
                }

                bool swaps = plans[i].CurrentTileX == plans[other].NextTileX
                    && plans[i].CurrentTileY == plans[other].NextTileY;
                if (swaps)
                {
                    plans[i].Blocked = true;
                    plans[other].Blocked = true;
                }
            }
        }

        private static void ApplyPlans(GameState state, MovementPlan[] plans, MovementSolverV2 solverV2)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                Unit unit = state.EntityState.Units[plans[i].UnitIndex];
                if (plans[i].ShouldClearTarget)
                {
                    unit.HasMoveTarget = false;
                    unit.TaskPhase = GetPhaseAfterClearedMove(unit.TaskPhase);
                    ClearMoveDestinationReservation(state, unit);
                    solverV2.OnIdle(unit);
                    continue;
                }

                if (plans[i].Blocked)
                {
                    unit.TaskPhase = GetPhaseAfterBlockedMove(state, unit);
                    if (unit.MovementBlockedReason == MovementBlockReason.None)
                    {
                        solverV2.OnBlocked(state, unit, MovementBlockReason.OccupiedNextTile);
                    }
                    continue;
                }

                if (!plans[i].AttemptsMove)
                {
                    solverV2.OnIdle(unit);
                    continue;
                }

                unit.Position = plans[i].NextPosition;
                bool shouldRecordProgressTick = ShouldRecordProgressTick(state, unit, plans[i]);
                if (shouldRecordProgressTick)
                {
                    unit.LastMovedTick = state.Tick;
                    solverV2.OnProgress(state, unit);
                }
                if (plans[i].WillReachTarget)
                {
                    unit.HasMoveTarget = false;
                    unit.TaskPhase = GetPhaseAfterArrivedMove(unit.TaskPhase);
                    ClearMoveDestinationReservation(state, unit);
                    solverV2.OnIdle(unit);
                }
            }
        }

        private static void ClearMoveDestinationReservation(GameState state, Unit unit)
        {
            if (unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)
            {
                SpatialRules.ClearInteractionReservation(state, unit);
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

            return state.MovementProgressPolicy.IsNoProgressTimedOut(state, unit) ? WorkerTaskPhase.BlockedWaiting : unit.TaskPhase;
        }

        private static WorkerTaskPhase GetPhaseAfterArrivedMove(WorkerTaskPhase phase)
        {
            return phase == WorkerTaskPhase.MovingToCommandMove ? WorkerTaskPhase.Idle : phase;
        }

        private static bool ShouldRecordProgressTick(GameState state, Unit unit, MovementPlan plan)
        {
            if (unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot
                || unit.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot
                || unit.TaskPhase == WorkerTaskPhase.MovingToBuildSlot
                || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting)
            {
                if (!unit.HasMoveTarget)
                {
                    return plan.WillReachTarget;
                }

                int targetTileX = SpatialRules.GetTileX(unit.MoveTarget);
                int targetTileY = SpatialRules.GetTileY(unit.MoveTarget);
                int beforeTileX = SpatialRules.GetTileX(plan.CurrentPosition);
                int beforeTileY = SpatialRules.GetTileY(plan.CurrentPosition);
                int afterTileX = plan.NextTileX;
                int afterTileY = plan.NextTileY;
                int beforeDistance = Abs(beforeTileX - targetTileX) + Abs(beforeTileY - targetTileY);
                int afterDistance = Abs(afterTileX - targetTileX) + Abs(afterTileY - targetTileY);

                // Worker no-progress needs approach progress, not sideways jitter.
                return afterDistance < beforeDistance || plan.WillReachTarget;
            }

            // Command-move retains policy-driven tile progress semantics.
            return state.MovementProgressPolicy.ShouldRecordProgressTick(unit, plan.EntersNewTile, plan.WillReachTarget);
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

        private static int EncodeTileKey(int tileX, int tileY)
        {
            return (tileY << 16) ^ (tileX & 0xFFFF);
        }

        private sealed class MovementContext
        {
            private readonly Dictionary<int, int> occupiedTileToUnitId = new Dictionary<int, int>();
            private readonly Dictionary<int, int> reservedTileToUnitId = new Dictionary<int, int>();
            private readonly Dictionary<int, int> unitIdToPlanIndex = new Dictionary<int, int>();
            private readonly HashSet<int> movingUnitIds = new HashSet<int>();

            private MovementContext()
            {
            }

            public static MovementContext Create(GameState state)
            {
                var context = new MovementContext();
                for (int i = 0; i < state.EntityState.Units.Count; i++)
                {
                    Unit unit = state.EntityState.Units[i];
                    context.unitIdToPlanIndex[unit.Id] = i;
                    if (unit.IsDead)
                    {
                        continue;
                    }

                    if (unit.HasMoveTarget)
                    {
                        context.movingUnitIds.Add(unit.Id);
                    }

                    int occupiedKey = EncodeTileKey(SpatialRules.GetTileX(unit.Position), SpatialRules.GetTileY(unit.Position));
                    if (!context.occupiedTileToUnitId.ContainsKey(occupiedKey) || unit.Id < context.occupiedTileToUnitId[occupiedKey])
                    {
                        context.occupiedTileToUnitId[occupiedKey] = unit.Id;
                    }

                    if (unit.ReservedInteractionKind != InteractionReservationKind.None)
                    {
                        int reservedKey = EncodeTileKey(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                        if (!context.reservedTileToUnitId.ContainsKey(reservedKey) || unit.Id < context.reservedTileToUnitId[reservedKey])
                        {
                            context.reservedTileToUnitId[reservedKey] = unit.Id;
                        }
                    }
                }

                return context;
            }

            public bool IsTileOccupied(int tileX, int tileY, int ignoredUnitId)
            {
                return TryGetOccupyingUnitId(tileX, tileY, ignoredUnitId, out _);
            }

            public bool TryGetOccupyingUnitId(int tileX, int tileY, int ignoredUnitId, out int unitId)
            {
                unitId = 0;
                if (!occupiedTileToUnitId.TryGetValue(EncodeTileKey(tileX, tileY), out int occupyingUnitId) || occupyingUnitId == ignoredUnitId)
                {
                    return false;
                }

                unitId = occupyingUnitId;
                return true;
            }

            public bool IsTileReserved(int tileX, int tileY, int ignoredUnitId)
            {
                if (!reservedTileToUnitId.TryGetValue(EncodeTileKey(tileX, tileY), out int reservingUnitId))
                {
                    return false;
                }

                return reservingUnitId != ignoredUnitId;
            }

            public bool TryGetPlanIndex(int unitId, out int planIndex)
            {
                return unitIdToPlanIndex.TryGetValue(unitId, out planIndex);
            }

            public bool IsOccupyingUnitMoving(int tileX, int tileY, int ignoredUnitId)
            {
                return TryGetOccupyingUnitId(tileX, tileY, ignoredUnitId, out int unitId)
                    && movingUnitIds.Contains(unitId);
            }

            public int CountNearbyTraffic(int tileX, int tileY, int ignoredUnitId)
            {
                int count = 0;
                for (int y = tileY - 1; y <= tileY + 1; y++)
                {
                    for (int x = tileX - 1; x <= tileX + 1; x++)
                    {
                        if (x == tileX && y == tileY)
                        {
                            continue;
                        }

                        if (IsTileOccupied(x, y, ignoredUnitId) || IsTileReserved(x, y, ignoredUnitId))
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
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


