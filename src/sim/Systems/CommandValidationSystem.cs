using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Systems
{
    public sealed class CommandValidationSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            foreach (CommandEnvelope command in commandContext.Commands)
            {
                if (IsBlockedByPlayerState(state, command))
                {
                    RecordCommandResult(
                        state,
                        command,
                        new CommandValidationReport(
                            false,
                            CommandValidationReason.PlayerStateBlocked,
                            0,
                            0,
                            0,
                            0,
                            0));
                    state.DebugCounters.RejectedCommandCount++;
                    continue;
                }

                CommandValidationReport report = CommandValidationInspector.Evaluate(state, rules, command);
                RecordCommandResult(state, command, report);
                if (report.Accepted)
                {
                    commandContext.AcceptedCommands.Add(command);
                }
                else
                {
                    state.DebugCounters.RejectedCommandCount++;
                }
            }
        }

        private static void RecordCommandResult(GameState state, CommandEnvelope command, CommandValidationReport report)
        {
            if (command.Header.CommandType == CommandType.NoOp)
            {
                return;
            }

            state.DebugCounters.LastCommandType = command.Header.CommandType;
            state.DebugCounters.LastCommandReason = report.Reason;
            state.DebugCounters.LastCommandAccepted = report.Accepted;
            state.DebugCounters.LastCommandPlayerIndex = command.Header.PlayerIndex;
            state.DebugCounters.LastCommandTargetEntityId = report.TargetEntityId;
            state.DebugCounters.LastCommandTargetTileX = report.TargetTileX;
            state.DebugCounters.LastCommandTargetTileY = report.TargetTileY;
            state.DebugCounters.LastCommandUnitCount = report.UnitCount;
            state.DebugCounters.LastCommandFirstUnitId = report.FirstUnitId;
        }

        private static bool IsBlockedByPlayerState(GameState state, CommandEnvelope command)
        {
            int playerIndex = command.Header.PlayerIndex;
            if (playerIndex < 0 || playerIndex >= state.PlayerStates.Players.Count)
            {
                return false;
            }

            if (command.Header.CommandType == CommandType.NoOp || command.Header.CommandType == CommandType.Resign)
            {
                return false;
            }

            return state.PlayerStates.Players[playerIndex].IsResigned || state.PlayerStates.Players[playerIndex].IsDefeated;
        }
    }
}
