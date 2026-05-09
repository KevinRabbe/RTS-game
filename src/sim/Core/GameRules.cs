namespace RtsGame.Sim.Core
{
    public sealed class GameRules
    {
        public const int DefaultTickRate = 20;

        public uint RulesVersion { get; }
        public int TickRate { get; }
        public int MaxPlayers { get; }
        public int InputDelayTicks { get; }
        public int ChecksumIntervalTicks { get; }

        public GameRules(uint rulesVersion, int maxPlayers, int inputDelayTicks, int checksumIntervalTicks, int tickRate = DefaultTickRate)
        {
            RulesVersion = rulesVersion;
            MaxPlayers = maxPlayers;
            InputDelayTicks = inputDelayTicks;
            ChecksumIntervalTicks = checksumIntervalTicks;
            TickRate = tickRate;
        }

        public static GameRules CreatePhaseZeroDefaults(int playerCount)
        {
            return new GameRules(1, playerCount, 2, 20);
        }
    }
}
