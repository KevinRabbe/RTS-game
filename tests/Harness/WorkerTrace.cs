using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void CaptureWorkerTraceTick(GameState state, int[] unitIds, Queue<string> traces, int maxEntries)
        {
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                int tileX = SpatialRules.GetTileX(unit.Position);
                int tileY = SpatialRules.GetTileY(unit.Position);
                int moveTileX = unit.HasMoveTarget ? SpatialRules.GetTileX(unit.MoveTarget) : -1;
                int moveTileY = unit.HasMoveTarget ? SpatialRules.GetTileY(unit.MoveTarget) : -1;
                int noProgressTicks = unit.LastMovedTick < 0 ? 0 : state.Tick - unit.LastMovedTick;
                string line =
                    "t=" + state.Tick
                    + " u=" + unit.Id
                    + " tile=(" + tileX + "," + tileY + ")"
                    + " raw=(" + unit.Position.X.Raw + "," + unit.Position.Y.Raw + ")"
                    + " phase=" + unit.TaskPhase
                    + " move=(" + moveTileX + "," + moveTileY + ")"
                    + " moveRaw=(" + (unit.HasMoveTarget ? unit.MoveTarget.X.Raw.ToString() : "-") + "," + (unit.HasMoveTarget ? unit.MoveTarget.Y.Raw.ToString() : "-") + ")"
                    + " reserve=" + unit.ReservedInteractionKind + ":" + unit.ReservedInteractionTargetId + "@(" + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY + ")"
                    + " area=" + unit.CurrentResourceAreaId
                    + " node=" + unit.CurrentResourceNodeId
                    + " build=" + unit.CurrentBuildTargetId
                    + " carry=" + unit.CarriedResourceType + ":" + unit.CarriedAmount
                    + " noProgress=" + noProgressTicks
                    + " lastMoved=" + unit.LastMovedTick
                    + " cmd=" + state.DebugCounters.LastCommandType
                    + " cmdAccepted=" + state.DebugCounters.LastCommandAccepted
                    + " cmdReason=" + state.DebugCounters.LastCommandReason;
                traces.Enqueue(line);
                while (traces.Count > maxEntries)
                {
                    traces.Dequeue();
                }
            }
        }

        private static string BuildTraceFailureMessage(string header, Queue<string> traces)
        {
            if (traces.Count == 0)
            {
                return header;
            }

            string summary = BuildFirstStallSummary(traces);
            if (summary.Length == 0)
            {
                return header + Environment.NewLine + string.Join(Environment.NewLine, traces);
            }

            return header + Environment.NewLine + summary + Environment.NewLine + string.Join(Environment.NewLine, traces);
        }

        private static string BuildFirstStallSummary(Queue<string> traces)
        {
            int worstNoProgress = -1;
            string worstLine = string.Empty;
            foreach (string line in traces)
            {
                if (!(line.Contains("phase=MovingToResourceSlot")
                    || line.Contains("phase=MovingToDropoffSlot")
                    || line.Contains("phase=MovingToBuildSlot")
                    || line.Contains("phase=MovingToCommandMove")))
                {
                    continue;
                }

                int marker = line.IndexOf("noProgress=", StringComparison.Ordinal);
                if (marker < 0)
                {
                    continue;
                }

                marker += "noProgress=".Length;
                int end = line.IndexOf(' ', marker);
                if (end < 0)
                {
                    end = line.Length;
                }

                if (!int.TryParse(line.Substring(marker, end - marker), out int parsed))
                {
                    continue;
                }

                if (parsed > worstNoProgress)
                {
                    worstNoProgress = parsed;
                    worstLine = line;
                }
            }

            if (worstNoProgress < 0)
            {
                return string.Empty;
            }

            return "firstStallSummary noProgress=" + worstNoProgress + " trace=\"" + worstLine + "\"";
        }
    }
}
