using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void AssertDistinctReservations(GameState state, int[] unitIds, InteractionReservationKind kind, int targetId, string message)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                AssertEqual(kind, unit.ReservedInteractionKind, message + " kind for unit " + unit.Id);
                AssertEqual(targetId, unit.ReservedInteractionTargetId, message + " target for unit " + unit.Id);
                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, seen.Add(key), message + " should not duplicate tile " + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY);
            }
        }

        private static bool TryReservationsAreDistinct(GameState state, int[] unitIds, InteractionReservationKind kind, int targetId)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (unit.ReservedInteractionKind != kind || unit.ReservedInteractionTargetId != targetId)
                {
                    continue;
                }

                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                if (!seen.Add(key))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertNoDuplicateFinalPurposeReservations(GameState state, string message)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.ReservedInteractionKind == InteractionReservationKind.None)
                {
                    continue;
                }

                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, seen.Add(key), message + " duplicate reserved tile " + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY);
            }
        }

        private static void AssertNoLiveUnitStacking(GameState state, string message)
        {
            var occupied = new HashSet<int>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead)
                {
                    continue;
                }

                int tileX = SpatialRules.GetTileX(unit.Position);
                int tileY = SpatialRules.GetTileY(unit.Position);
                int key = (tileY << 16) ^ (tileX & 0xFFFF);
                AssertEqual(true, occupied.Add(key), message + " at " + tileX + "," + tileY);
            }
        }

        private static void AssertNoEndlessWorkerPhase(GameState state, int[] unitIds, WorkerTaskPhase phase, int maxNoProgressTicks, string message)
        {
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (unit.TaskPhase != phase)
                {
                    continue;
                }

                int stalledFor = unit.LastMovedTick < 0 ? 0 : state.Tick - unit.LastMovedTick;
                AssertEqual(true, stalledFor <= maxNoProgressTicks, message + " unit=" + unit.Id + " phase=" + phase + " stalled=" + stalledFor);
            }
        }

        private static Dictionary<int, GatherReservationChurnState> CreateGatherReservationChurnTracker(int[] unitIds)
        {
            var tracker = new Dictionary<int, GatherReservationChurnState>();
            for (int i = 0; i < unitIds.Length; i++)
            {
                tracker[unitIds[i]] = new GatherReservationChurnState();
            }

            return tracker;
        }

        private static void AssertBoundedGatherReservationChurn(
            GameState state,
            int[] unitIds,
            Dictionary<int, GatherReservationChurnState> tracker,
            string message)
        {
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                GatherReservationChurnState? churnState;
                if (!tracker.TryGetValue(unit.Id, out churnState))
                {
                    churnState = new GatherReservationChurnState();
                }

                bool activeGatherIntent = unit.CurrentResourceAreaId != 0
                    && unit.CurrentResourceNodeId != 0
                    && unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode;

                long reservationKey = EncodeReservationKey(unit);
                bool sameTargetAsPrevious = churnState.HasPrevious
                    && churnState.LastAreaId == unit.CurrentResourceAreaId
                    && churnState.LastNodeId == unit.CurrentResourceNodeId;
                bool sameReservationAsPrevious = churnState.HasPrevious && churnState.LastReservationKey == reservationKey;

                if (activeGatherIntent && sameTargetAsPrevious && !sameReservationAsPrevious)
                {
                    churnState.ChangeTicks.Enqueue(state.Tick);
                }

                while (churnState.ChangeTicks.Count > 0
                    && state.Tick - churnState.ChangeTicks.Peek() >= GameData.GatherReservationChurnWindowTicks)
                {
                    churnState.ChangeTicks.Dequeue();
                }

                if (activeGatherIntent)
                {
                    AssertEqual(
                        true,
                        churnState.ChangeTicks.Count <= GameData.GatherReservationChurnMaxPerWindow,
                        message + " unit=" + unit.Id + " churn=" + churnState.ChangeTicks.Count + " window=" + GameData.GatherReservationChurnWindowTicks + " node=" + unit.CurrentResourceNodeId + " area=" + unit.CurrentResourceAreaId);
                }

                churnState.LastAreaId = unit.CurrentResourceAreaId;
                churnState.LastNodeId = unit.CurrentResourceNodeId;
                churnState.LastReservationKey = reservationKey;
                churnState.HasPrevious = true;
                tracker[unit.Id] = churnState;
            }
        }

        private static long EncodeReservationKey(Unit unit)
        {
            if (unit.ReservedInteractionKind == InteractionReservationKind.None)
            {
                return -1L;
            }

            unchecked
            {
                long result = (int)unit.ReservedInteractionKind;
                result = (result * 397L) ^ unit.ReservedInteractionTargetId;
                result = (result * 397L) ^ unit.ReservedInteractionTileX;
                result = (result * 397L) ^ unit.ReservedInteractionTileY;
                return result;
            }
        }

        private static long EncodeMoveTargetKey(Unit unit)
        {
            if (!unit.HasMoveTarget)
            {
                return -1L;
            }

            unchecked
            {
                return (unit.MoveTarget.X.Raw * 397L) ^ unit.MoveTarget.Y.Raw;
            }
        }

        private sealed class GatherReservationChurnState
        {
            public int LastAreaId { get; set; }
            public int LastNodeId { get; set; }
            public long LastReservationKey { get; set; } = long.MinValue;
            public bool HasPrevious { get; set; }
            public Queue<int> ChangeTicks { get; } = new Queue<int>();
        }

        private readonly struct WorkerDiagnosticSample
        {
            public WorkerTaskPhase Phase { get; }
            public long PositionXRaw { get; }
            public long MoveTargetKey { get; }
            public long ReservationKey { get; }

            private WorkerDiagnosticSample(WorkerTaskPhase phase, long positionXRaw, long moveTargetKey, long reservationKey)
            {
                Phase = phase;
                PositionXRaw = positionXRaw;
                MoveTargetKey = moveTargetKey;
                ReservationKey = reservationKey;
            }

            public static WorkerDiagnosticSample Capture(Unit unit)
            {
                long moveTargetKey = unit.HasMoveTarget ? CombineRaw(unit.MoveTarget.X.Raw, unit.MoveTarget.Y.Raw) : -1L;
                long reservationKey = unit.ReservedInteractionKind == InteractionReservationKind.None
                    ? -1L
                    : CombineInts((int)unit.ReservedInteractionKind, unit.ReservedInteractionTargetId, unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                return new WorkerDiagnosticSample(unit.TaskPhase, unit.Position.X.Raw, moveTargetKey, reservationKey);
            }

            private static long CombineRaw(long xRaw, long yRaw)
            {
                unchecked
                {
                    return (xRaw * 397L) ^ yRaw;
                }
            }

            private static long CombineInts(int a, int b, int c, int d)
            {
                unchecked
                {
                    long result = a;
                    result = (result * 397L) ^ b;
                    result = (result * 397L) ^ c;
                    result = (result * 397L) ^ d;
                    return result;
                }
            }
        }
    }
}
