using RtsGame.Sim.Commands;

namespace RtsGame.Net.Lockstep
{
    public sealed class PeerCommandInbox
    {
        public CommandBuffer Commands { get; } = new CommandBuffer();

        public void Receive(CommandEnvelope command)
        {
            Commands.Add(command);
        }
    }
}
