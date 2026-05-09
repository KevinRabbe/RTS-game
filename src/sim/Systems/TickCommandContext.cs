using System.Collections.Generic;
using RtsGame.Sim.Commands;

namespace RtsGame.Sim.Systems
{
    public sealed class TickCommandContext
    {
        public List<CommandEnvelope> Commands { get; }
        public List<CommandEnvelope> AcceptedCommands { get; } = new List<CommandEnvelope>();

        public TickCommandContext(List<CommandEnvelope> commands)
        {
            Commands = commands;
        }
    }
}
