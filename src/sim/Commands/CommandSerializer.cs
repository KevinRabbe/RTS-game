using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public static class CommandSerializer
    {
        public static byte[] Serialize(CommandEnvelope command)
        {
            var writer = new CanonicalWriter();
            command.Write(writer);
            return writer.ToArray();
        }
    }
}
