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
        private static void LockstepEmptyStream()
        {
            LockstepSession session = RunLockstep(500, 2, 3, false);
            AssertEqual(500, session.CurrentTick, "lockstep should reach requested tick");
            AssertEqual(0, session.DesyncReports.Count, "lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "peer checksums should match");
        }

        private static void LockstepArrivalReordering()
        {
            LockstepSession session = RunLockstep(250, 2, 3, true);
            AssertEqual(250, session.CurrentTick, "lockstep should reach requested tick with reordered delivery");
            AssertEqual(0, session.DesyncReports.Count, "reordered command arrival should not desync");
        }

    }
}
