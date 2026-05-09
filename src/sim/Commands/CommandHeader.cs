using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public readonly struct CommandHeader
    {
        public int Tick { get; }
        public int PlayerIndex { get; }
        public uint Sequence { get; }
        public CommandType CommandType { get; }

        public CommandHeader(int tick, int playerIndex, uint sequence, CommandType commandType)
        {
            Tick = tick;
            PlayerIndex = playerIndex;
            Sequence = sequence;
            CommandType = commandType;
        }

        public void Write(CanonicalWriter writer)
        {
            writer.WriteInt32(Tick);
            writer.WriteInt32(PlayerIndex);
            writer.WriteUInt32(Sequence);
            writer.WriteUInt16((ushort)CommandType);
        }
    }
}
