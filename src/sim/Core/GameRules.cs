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
        public bool EnableMovementSolverV2Villagers { get; }

        public GameRules(
            uint rulesVersion,
            int maxPlayers,
            int inputDelayTicks,
            int checksumIntervalTicks,
            int tickRate = DefaultTickRate,
            bool enableMovementSolverV2Villagers = false)
        {
            RulesVersion = rulesVersion;
            MaxPlayers = maxPlayers;
            InputDelayTicks = inputDelayTicks;
            ChecksumIntervalTicks = checksumIntervalTicks;
            TickRate = tickRate;
            EnableMovementSolverV2Villagers = enableMovementSolverV2Villagers;
        }

        public static GameRules CreatePhaseZeroDefaults(int playerCount)
        {
            return new GameRules(1, playerCount, 2, 20);
        }

        public GameRules WithMovementSolverV2Villagers(bool enabled)
        {
            return new GameRules(
                RulesVersion,
                MaxPlayers,
                InputDelayTicks,
                ChecksumIntervalTicks,
                TickRate,
                enabled);
        }
    }
}
