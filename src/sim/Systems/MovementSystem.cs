using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Systems
{
    public sealed class MovementSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            MovementPlan[] plans = BuildPlans(state);
            MarkBlockedByStationaryUnits(state, plans);
            MarkSharedDestinationConflicts(plans);
            MarkSwapConflicts(plans);
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
                long distanceRaw = DeterministicMath.SqrtRaw(delta.LengthSquaredRaw());
                if (distanceRaw == 0 || distanceRaw <= speed.Raw)
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
                if (SpatialRules.IsBlockedByWall(state, nextPosition))
                {
                    plan.ShouldClearTarget = true;
                    plans[i] = plan;
                    continue;
                }

                plan.AttemptsMove = true;
                plan.NextPosition = nextPosition;
                plans[i] = plan;
            }

            return plans;
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
                        plans[i].ShouldClearTarget = true;
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
                            && plans[i].NextTileY == plans[other].NextTileY)
                        {
                            plans[other].Blocked = true;
                            plans[other].ShouldClearTarget = true;
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
                        plans[i].ShouldClearTarget = true;
                        plans[other].Blocked = true;
                        plans[other].ShouldClearTarget = true;
                    }
                }
            }
        }

        private static void ApplyPlans(GameState state, MovementPlan[] plans)
        {
            for (int i = 0; i < plans.Length; i++)
            {
                Unit unit = state.EntityState.Units[plans[i].UnitIndex];
                if (plans[i].Blocked || plans[i].ShouldClearTarget)
                {
                    unit.HasMoveTarget = false;
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
                }
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
