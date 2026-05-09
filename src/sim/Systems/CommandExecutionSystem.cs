using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Sim.Systems
{
    public sealed class CommandExecutionSystem : ISimSystem
    {
        public void Run(GameState state, GameRules rules, TickCommandContext commandContext)
        {
            foreach (CommandEnvelope command in commandContext.AcceptedCommands)
            {
                command.Payload.Execute(state, rules, command.Header);
                state.DebugCounters.ExecutedCommandCount++;
            }
        }
    }
}
