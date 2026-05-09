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
                    state.DebugCounters.RejectedCommandCount++;
                    continue;
                }

                if (command.Payload.IsValid(state, rules, command.Header))
                {
                    commandContext.AcceptedCommands.Add(command);
                }
                else
                {
                    state.DebugCounters.RejectedCommandCount++;
                }
            }
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
