using RtsGame.Net.Lockstep;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void EmptyTickDeterminism()
        {
            ulong first = RunNoOpSimulation(1000, 2, 123);
            ulong second = RunNoOpSimulation(1000, 2, 123);
            AssertEqual(first, second, "same empty command stream must produce same checksum");
        }

        private static void CommandOrdering()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            ulong canonical = RunDebugCommands(rules, new[]
            {
                DebugCommand(0, 0, 0, 1),
                DebugCommand(0, 1, 0, 10),
                DebugCommand(0, 0, 1, 100)
            });

            ulong shuffled = RunDebugCommands(rules, new[]
            {
                DebugCommand(0, 0, 1, 100),
                DebugCommand(0, 1, 0, 10),
                DebugCommand(0, 0, 0, 1)
            });

            AssertEqual(canonical, shuffled, "shuffled insertion must still execute deterministically");
        }

        private static void CanonicalSerialization()
        {
            var writerA = new CanonicalWriter();
            writerA.WriteInt32(-1);
            writerA.WriteUInt64(42);
            writerA.WriteBool(true);

            var writerB = new CanonicalWriter();
            writerB.WriteInt32(-1);
            writerB.WriteUInt64(42);
            writerB.WriteBool(true);

            byte[] a = writerA.ToArray();
            byte[] b = writerB.ToArray();
            AssertEqual(a.Length, b.Length, "canonical byte lengths must match");
            for (int i = 0; i < a.Length; i++)
            {
                AssertEqual(a[i], b[i], "canonical bytes must match");
            }
        }

        private static void FixedPointDeterminism()
        {
            Fixed a = Fixed.FromRatio(1, 3);
            Fixed b = Fixed.FromRatio(2, 3);
            Fixed c = a + b;
            AssertEqual(Fixed.FromInt(1).Raw - 1, c.Raw, "fixed ratios should truncate deterministically");
        }

        private static void CleanupUpdatesLookup()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = new GameState(1, 1);
            state.EntityState.Units.Add(new Unit { Id = 1, OwnerPlayerIndex = 0, HitPoints = 0, IsDead = true });
            state.EntityState.Units.Add(new Unit { Id = 2, OwnerPlayerIndex = 0, HitPoints = 1, IsDead = false });
            state.EntityState.EntityLookup[1] = new EntityRef(EntityKind.Unit, 0);
            state.EntityState.EntityLookup[2] = new EntityRef(EntityKind.Unit, 1);

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            AssertEqual(1, state.EntityState.Units.Count, "dead unit should be removed");
            AssertFalse(state.EntityState.EntityLookup.ContainsKey(1), "removed unit lookup should be gone");
            AssertEqual(0, state.EntityState.EntityLookup[2].Index, "moved unit lookup should update");
        }

        private static void MissingInputStalls()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 9);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            bool advanced = session.TryAdvanceOneTick();
            AssertFalse(advanced, "missing player input must stall");
            AssertEqual(0, session.CurrentTick, "current tick must not advance when input is missing");
        }

    }
}
