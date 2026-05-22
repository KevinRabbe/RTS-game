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
        private readonly MovementEngineV2 movementEngineV2 = new MovementEngineV2();

        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            if (rules.EnableMovementEngineV2)
            {
                movementEngineV2.Run(state, rules, commandContext, () => RunLegacyPipeline(state, rules));
                return;
            }

            RunLegacyPipeline(state, rules);
        }

        private void RunLegacyPipeline(GameState state, GameRules rules)
        {
            MovementContext context = MovementContext.Create(state);
            MovementPlan[] plans = BuildPlans(state, rules, context, movementSolverV2);
            MarkSharedDestinationConflicts(plans);
            MarkSwapConflicts(plans);
            MarkBlockedByStationaryUnits(context, plans);
            ResolveBlockedAlternateSteps(state, context, plans);
            MarkSharedDestinationConflicts(plans);
            MarkSwapConflicts(plans);
            MarkBlockedByStationaryUnits(context, plans);
            ApplyPlans(state, plans, movementSolverV2);
        }

        private static MovementPlan[] BuildPlans(GameState state, GameRules rules, MovementContext context, MovementSolverV2 solverV2)
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

                RetargetStaleCommandMoveDestination(state, unit);
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
                if (solverV2.IsEnabledFor(rules, unit)
                    && TryBuildMovementSolverV2Plan(
                        state,
                        context,
                        i,
                        unit,
                        speed,
                        currentTileX,
                        currentTileY,
                        targetTileX,
                        targetTileY,
                        out MovementPlan v2Plan,
                        out MovementBlockReason v2BlockReason))
                {
                    plans[i] = v2Plan;
                    if (v2Plan.Blocked || v2Plan.ShouldClearTarget)
                    {
                        solverV2.OnBlocked(state, unit, v2BlockReason);
                    }

                    continue;
                }

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
                            plan.BlockReason = MovementBlockReason.NoPath;
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
                bool willReachTarget = nextPathTileIsTarget && distanceRaw <= speed.Raw;
                FixedVector2 nextPosition;
                if (distanceRaw == 0 || distanceRaw <= speed.Raw)
                {
                    nextPosition = willReachTarget
                        ? unit.MoveTarget
                        : FixedVector2.FromInts(intendedNextTileX, intendedNextTileY);
                }
                else
                {
                    Fixed distance = new Fixed(distanceRaw);
                    Fixed stepScale = speed / distance;
                    nextPosition = unit.Position + FixedVector2.Multiply(delta, stepScale);
                }

                int projectedTileX = SpatialRules.GetTileX(nextPosition);
                int projectedTileY = SpatialRules.GetTileY(nextPosition);
                if (SpatialRules.IsTileBlockedForUnitMovement(state, projectedTileX, projectedTileY))
                {
                    if (IsWorkerTaskMovementPhase(unit.TaskPhase))
                    {
                        plan.Blocked = true;
                        plan.BlockReason = MovementBlockReason.StaticBlocked;
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
                        willReachTarget = false;
                    }
                }

                plan.AttemptsMove = true;
                plan.NextPosition = nextPosition;
                plan.WillReachTarget = willReachTarget;
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
            int bestBacktrackClass = int.MaxValue;
            bool found = false;
            int previousTileX = SpatialRules.GetTileX(unit.Position - unit.Velocity);
            int previousTileY = SpatialRules.GetTileY(unit.Position - unit.Velocity);

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
                int backtrackClass = candidateX == previousTileX && candidateY == previousTileY ? 1 : 0;
                if (!found
                    || backtrackClass < bestBacktrackClass
                    || (backtrackClass == bestBacktrackClass && stepClass < bestStepClass)
                    || (backtrackClass == bestBacktrackClass && stepClass == bestStepClass && distance < bestDistance)
                    || (backtrackClass == bestBacktrackClass && stepClass == bestStepClass && distance == bestDistance && congestion < bestCongestion)
                    || (backtrackClass == bestBacktrackClass && stepClass == bestStepClass && distance == bestDistance && congestion == bestCongestion && turnCost < bestTurnCost)
                    || (backtrackClass == bestBacktrackClass && stepClass == bestStepClass && distance == bestDistance && congestion == bestCongestion && turnCost == bestTurnCost && CompareTile(candidateX, candidateY, bestX, bestY) < 0))
                {
                    bestX = candidateX;
                    bestY = candidateY;
                    bestDistance = distance;
                    bestBacktrackClass = backtrackClass;
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
                    if (plans[i].BlockReason == MovementBlockReason.None)
                    {
                        plans[i].BlockReason = MovementBlockReason.OccupiedNextTile;
                    }
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

        private static void ResolveBlockedAlternateSteps(GameState state, MovementContext context, MovementPlan[] plans)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                if (!plans[i].Blocked || !plans[i].AttemptsMove || !plans[i].EntersNewTile)
                {
                    continue;
                }

                Unit unit = state.EntityState.Units[plans[i].UnitIndex];
                if (unit.IsDead || !unit.HasMoveTarget)
                {
                    continue;
                }

                Fixed speed = GameData.GetUnitMoveSpeed(unit.UnitTypeId);
                if (speed.Raw <= 0)
                {
                    continue;
                }

                int currentTileX = plans[i].CurrentTileX;
                int currentTileY = plans[i].CurrentTileY;
                int targetTileX = SpatialRules.GetTileX(unit.MoveTarget);
                int targetTileY = SpatialRules.GetTileY(unit.MoveTarget);
                if (TryBuildAlternateStepPlan(
                    state,
                    context,
                    unit,
                    currentTileX,
                    currentTileY,
                    targetTileX,
                    targetTileY,
                    plans[i].NextTileX,
                    plans[i].NextTileY,
                    speed,
                    out FixedVector2 alternatePosition))
                {
                    plans[i].NextPosition = alternatePosition;
                    plans[i].WillReachTarget = false;
                    plans[i].Blocked = false;
                    plans[i].BlockReason = MovementBlockReason.None;
                }
            }
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
                    plans[i].BlockReason = MovementBlockReason.SharedDestinationConflict;
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
                    plans[i].BlockReason = MovementBlockReason.SwapConflict;
                    plans[other].Blocked = true;
                    plans[other].BlockReason = MovementBlockReason.SwapConflict;
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
                    unit.Velocity = new FixedVector2(new Fixed(0), new Fixed(0));
                    unit.TaskPhase = GetPhaseAfterClearedMove(unit.TaskPhase);
                    ClearMoveDestinationReservation(state, unit);
                    solverV2.OnIdle(unit);
                    continue;
                }

                if (plans[i].Blocked)
                {
                    unit.TaskPhase = GetPhaseAfterBlockedMove(state, unit);
                    solverV2.OnBlocked(state, unit, plans[i].BlockReason == MovementBlockReason.None ? MovementBlockReason.OccupiedNextTile : plans[i].BlockReason);
                    continue;
                }

                if (!plans[i].AttemptsMove)
                {
                    solverV2.OnIdle(unit);
                    unit.Velocity = new FixedVector2(new Fixed(0), new Fixed(0));
                    continue;
                }

                unit.Velocity = plans[i].NextPosition - plans[i].CurrentPosition;
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
                    unit.Velocity = new FixedVector2(new Fixed(0), new Fixed(0));
                    solverV2.OnIdle(unit);
                }
            }
        }

        private static bool TryBuildMovementSolverV2Plan(
            GameState state,
            MovementContext context,
            int unitIndex,
            Unit unit,
            Fixed speed,
            int currentTileX,
            int currentTileY,
            int targetTileX,
            int targetTileY,
            out MovementPlan plan,
            out MovementBlockReason blockReason)
        {
            plan = new MovementPlan(unitIndex, unit.Id, unit.Position, unit.Position, false);
            blockReason = MovementBlockReason.None;

            int targetKey = EncodeTileKey(targetTileX, targetTileY);
            if (unit.CorridorVersion != targetKey)
            {
                unit.CorridorVersion = targetKey;
                unit.CorridorStepIndex = 0;
                unit.LastSteeringDecisionTick = 0;
                unit.RetargetCooldownUntilTick = 0;
            }

            int nextTileX = targetTileX;
            int nextTileY = targetTileY;
            if (currentTileX != targetTileX || currentTileY != targetTileY)
            {
                if (!state.PathQueries.TryNextStep(state, unit.Id, currentTileX, currentTileY, targetTileX, targetTileY, state.Tick, out nextTileX, out nextTileY))
                {
                    plan.Blocked = IsWorkerTaskMovementPhase(unit.TaskPhase);
                    plan.ShouldClearTarget = !IsWorkerTaskMovementPhase(unit.TaskPhase);
                    blockReason = MovementBlockReason.NoPath;
                    return true;
                }
            }

            FixedVector2 tileCenter = FixedVector2.FromInts(nextTileX, nextTileY);
            FixedVector2 desiredDirection = tileCenter - unit.Position;
            long desiredDistanceRaw = DeterministicMath.SqrtRaw(desiredDirection.LengthSquaredRaw());
            if (desiredDistanceRaw == 0)
            {
                plan.AttemptsMove = true;
                plan.NextPosition = unit.MoveTarget;
                plan.WillReachTarget = true;
                return true;
            }

            bool nextTileIsTarget = nextTileX == targetTileX && nextTileY == targetTileY;
            FixedVector2 nextPosition;
            if (desiredDistanceRaw <= speed.Raw)
            {
                nextPosition = nextTileIsTarget ? unit.MoveTarget : tileCenter;
            }
            else
            {
                Fixed maxVelocity = speed;
                Fixed desiredScale = maxVelocity / new Fixed(desiredDistanceRaw);
                FixedVector2 desiredVelocity = FixedVector2.Multiply(desiredDirection, desiredScale);
                bool shouldRecomputeSteering =
                    unit.LastSteeringDecisionTick == 0
                    || state.Tick - unit.LastSteeringDecisionTick >= GameData.MovementSteeringDecisionCadenceTicks;
                if (shouldRecomputeSteering)
                {
                    FixedVector2 separationVelocity = ComputeSeparationVelocity(context, unit, currentTileX, currentTileY, speed);
                    desiredVelocity = desiredVelocity + separationVelocity;
                    unit.LastSteeringDecisionTick = state.Tick;
                }
                else
                {
                    // Hysteresis: preserve committed short-horizon steering between decision ticks.
                    desiredVelocity = unit.Velocity;
                }

                Fixed maxAcceleration = speed / Fixed.FromRatio(2, 1);
                FixedVector2 nextVelocity = MoveTowardVelocity(unit.Velocity, desiredVelocity, maxAcceleration);
                nextPosition = unit.Position + nextVelocity;
            }

            int projectedTileX = SpatialRules.GetTileX(nextPosition);
            int projectedTileY = SpatialRules.GetTileY(nextPosition);
            if (SpatialRules.IsTileBlockedForUnitMovement(state, projectedTileX, projectedTileY))
            {
                if (!TryStaticSlide(
                        state,
                        unit,
                        currentTileX,
                        currentTileY,
                        targetTileX,
                        targetTileY,
                        speed,
                        out nextPosition))
                {
                    plan.Blocked = IsWorkerTaskMovementPhase(unit.TaskPhase);
                    plan.ShouldClearTarget = !IsWorkerTaskMovementPhase(unit.TaskPhase);
                    blockReason = MovementBlockReason.StaticBlocked;
                    return true;
                }

                projectedTileX = SpatialRules.GetTileX(nextPosition);
                projectedTileY = SpatialRules.GetTileY(nextPosition);
            }

            if ((projectedTileX != currentTileX || projectedTileY != currentTileY)
                && context.IsTileOccupied(projectedTileX, projectedTileY, unit.Id)
                && !context.IsOccupyingUnitMoving(projectedTileX, projectedTileY, unit.Id))
            {
                if (state.Tick < unit.RetargetCooldownUntilTick)
                {
                    plan.Blocked = true;
                    blockReason = MovementBlockReason.OccupiedNextTile;
                    return true;
                }

                if (TryBuildAlternateStepPlan(
                    state,
                    context,
                    unit,
                    currentTileX,
                    currentTileY,
                    targetTileX,
                    targetTileY,
                    nextTileX,
                    nextTileY,
                    speed,
                    out FixedVector2 alternatePosition))
                {
                    nextPosition = alternatePosition;
                    projectedTileX = SpatialRules.GetTileX(nextPosition);
                    projectedTileY = SpatialRules.GetTileY(nextPosition);
                }
                else
                {
                    plan.Blocked = true;
                    blockReason = MovementBlockReason.OccupiedNextTile;
                    unit.RetargetCooldownUntilTick = state.Tick + GameData.MovementRetargetCooldownTicks;
                    return true;
                }
            }

            bool reachingTarget = projectedTileX == targetTileX
                && projectedTileY == targetTileY
                && DistanceRaw(unit.Position, unit.MoveTarget) <= speed.Raw;
            plan.AttemptsMove = true;
            plan.NextPosition = reachingTarget ? unit.MoveTarget : nextPosition;
            plan.WillReachTarget = reachingTarget;
            if (plan.EntersNewTile)
            {
                unit.CorridorStepIndex++;
            }
            return true;
        }

        private static bool TryStaticSlide(
            GameState state,
            Unit unit,
            int currentTileX,
            int currentTileY,
            int targetTileX,
            int targetTileY,
            Fixed speed,
            out FixedVector2 nextPosition)
        {
            nextPosition = unit.Position;
            int xDir = targetTileX > currentTileX ? 1 : targetTileX < currentTileX ? -1 : 0;
            int yDir = targetTileY > currentTileY ? 1 : targetTileY < currentTileY ? -1 : 0;
            int dx = Abs(targetTileX - currentTileX);
            int dy = Abs(targetTileY - currentTileY);

            int firstX = dx >= dy ? xDir : 0;
            int firstY = dx >= dy ? 0 : yDir;
            int secondX = dx >= dy ? 0 : xDir;
            int secondY = dx >= dy ? yDir : 0;

            if (TrySlideToTile(state, unit, currentTileX + firstX, currentTileY + firstY, speed, out nextPosition))
            {
                return true;
            }

            if (TrySlideToTile(state, unit, currentTileX + secondX, currentTileY + secondY, speed, out nextPosition))
            {
                return true;
            }

            return false;
        }

        private static bool TrySlideToTile(GameState state, Unit unit, int tileX, int tileY, Fixed speed, out FixedVector2 nextPosition)
        {
            nextPosition = unit.Position;
            if ((tileX == SpatialRules.GetTileX(unit.Position) && tileY == SpatialRules.GetTileY(unit.Position))
                || SpatialRules.IsTileBlockedForUnitMovement(state, tileX, tileY))
            {
                return false;
            }

            FixedVector2 target = FixedVector2.FromInts(tileX, tileY);
            FixedVector2 delta = target - unit.Position;
            long distanceRaw = DeterministicMath.SqrtRaw(delta.LengthSquaredRaw());
            if (distanceRaw == 0 || distanceRaw <= speed.Raw)
            {
                nextPosition = target;
                return true;
            }

            Fixed stepScale = speed / new Fixed(distanceRaw);
            nextPosition = unit.Position + FixedVector2.Multiply(delta, stepScale);
            return true;
        }

        private static FixedVector2 ComputeSeparationVelocity(MovementContext context, Unit unit, int currentTileX, int currentTileY, Fixed speed)
        {
            long pushXRaw = 0;
            long pushYRaw = 0;
            for (int y = currentTileY - 1; y <= currentTileY + 1; y++)
            {
                for (int x = currentTileX - 1; x <= currentTileX + 1; x++)
                {
                    if ((x == currentTileX && y == currentTileY)
                        || !context.IsTileOccupied(x, y, unit.Id))
                    {
                        continue;
                    }

                    pushXRaw += (currentTileX - x);
                    pushYRaw += (currentTileY - y);
                }
            }

            if (pushXRaw == 0 && pushYRaw == 0)
            {
                return new FixedVector2(new Fixed(0), new Fixed(0));
            }

            Fixed scale = speed / Fixed.FromRatio(4, 1);
            return new FixedVector2(new Fixed(pushXRaw * scale.Raw), new Fixed(pushYRaw * scale.Raw));
        }

        private static FixedVector2 MoveTowardVelocity(FixedVector2 current, FixedVector2 desired, Fixed maxDelta)
        {
            return new FixedVector2(
                MoveTowardFixed(current.X, desired.X, maxDelta),
                MoveTowardFixed(current.Y, desired.Y, maxDelta));
        }

        private static Fixed MoveTowardFixed(Fixed current, Fixed desired, Fixed maxDelta)
        {
            long deltaRaw = desired.Raw - current.Raw;
            long limitRaw = maxDelta.Raw;
            if (deltaRaw > limitRaw)
            {
                deltaRaw = limitRaw;
            }
            else if (deltaRaw < -limitRaw)
            {
                deltaRaw = -limitRaw;
            }

            return new Fixed(current.Raw + deltaRaw);
        }

        private static long DistanceRaw(FixedVector2 from, FixedVector2 to)
        {
            return DeterministicMath.SqrtRaw((to - from).LengthSquaredRaw());
        }

        private static void ClearMoveDestinationReservation(GameState state, Unit unit)
        {
            if (unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)
            {
                SpatialRules.ClearInteractionReservation(state, unit);
            }
        }

        private static void RetargetStaleCommandMoveDestination(GameState state, Unit unit)
        {
            if (unit.TaskPhase != WorkerTaskPhase.MovingToCommandMove
                || unit.ReservedInteractionKind != InteractionReservationKind.MoveDestination
                || !state.MovementProgressPolicy.IsNoProgressTimedOut(state, unit))
            {
                return;
            }

            int reservationAge = unit.LastReservationRetargetTick < 0 ? int.MaxValue : state.Tick - unit.LastReservationRetargetTick;
            if (reservationAge < GameData.ReservationRetargetCadenceTicks)
            {
                return;
            }

            int commandTargetX = DecodeTileX(unit.ReservedInteractionTargetId);
            int commandTargetY = DecodeTileY(unit.ReservedInteractionTargetId);
            var excludedTile = new SpatialRules.TileCoord(unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
            SpatialRules.ClearInteractionReservation(state, unit, ReservationReleaseReason.Timeout);
            if (SpatialRules.TryReserveNearestReachableMoveDestinationTile(
                state,
                unit,
                commandTargetX,
                commandTargetY,
                5,
                true,
                excludedTile,
                out SpatialRules.TileCoord replacement))
            {
                unit.MoveTarget = FixedVector2.FromInts(replacement.X, replacement.Y);
                unit.LastMovedTick = state.Tick;
                unit.LastMovementRetargetReason = MovementRetargetReason.NoProgressTimeout;
                return;
            }

            SpatialRules.ReserveInteractionSlot(
                state,
                unit,
                InteractionReservationKind.MoveDestination,
                EncodeTileKey(commandTargetX, commandTargetY),
                excludedTile);
            unit.MoveTarget = FixedVector2.FromInts(excludedTile.X, excludedTile.Y);
            unit.LastMovementRetargetReason = MovementRetargetReason.PathUnreachable;
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

        private static int DecodeTileX(int key)
        {
            return key & 0xFFFF;
        }

        private static int DecodeTileY(int key)
        {
            return key >> 16;
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
            public MovementBlockReason BlockReason;

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
                BlockReason = MovementBlockReason.None;
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


