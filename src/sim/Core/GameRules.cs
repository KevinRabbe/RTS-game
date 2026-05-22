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
        public bool EnableMovementEngineV2 { get; }
        public bool EnableGatherEngineV2 { get; }

        public GameRules(
            uint rulesVersion,
            int maxPlayers,
            int inputDelayTicks,
            int checksumIntervalTicks,
            int tickRate = DefaultTickRate,
            bool enableMovementSolverV2Villagers = false,
            bool enableMovementEngineV2 = false,
            bool enableGatherEngineV2 = false)
        {
            RulesVersion = rulesVersion;
            MaxPlayers = maxPlayers;
            InputDelayTicks = inputDelayTicks;
            ChecksumIntervalTicks = checksumIntervalTicks;
            TickRate = tickRate;
            EnableMovementSolverV2Villagers = enableMovementSolverV2Villagers;
            EnableMovementEngineV2 = enableMovementEngineV2;
            EnableGatherEngineV2 = enableGatherEngineV2;
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
                enabled,
                EnableMovementEngineV2,
                EnableGatherEngineV2);
        }

        public GameRules WithMovementEngineV2(bool enabled)
        {
            return new GameRules(
                RulesVersion,
                MaxPlayers,
                InputDelayTicks,
                ChecksumIntervalTicks,
                TickRate,
                EnableMovementSolverV2Villagers,
                enabled,
                EnableGatherEngineV2);
        }

        public GameRules WithGatherEngineV2(bool enabled)
        {
            return new GameRules(
                RulesVersion,
                MaxPlayers,
                InputDelayTicks,
                ChecksumIntervalTicks,
                TickRate,
                EnableMovementSolverV2Villagers,
                EnableMovementEngineV2,
                enabled);
        }
    }
}
