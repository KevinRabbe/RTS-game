using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class CommandEnvelope
    {
        public CommandHeader Header { get; }
        public ICommand Payload { get; }

        public CommandEnvelope(CommandHeader header, ICommand payload)
        {
            Header = header;
            Payload = payload;
        }

        public void Write(CanonicalWriter writer)
        {
            Header.Write(writer);
            Payload.WritePayload(writer);
        }
    }
}
