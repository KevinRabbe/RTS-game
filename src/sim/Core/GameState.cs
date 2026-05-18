using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Commands;

namespace RtsGame.Sim.Core
{
    public sealed class GameState
    {
        public int Tick { get; set; }
        public ulong MatchSeed { get; }
        public DeterministicRandomState RngState;
        public EntityState EntityState { get; }
        public PlayerStateContainer PlayerStates { get; }
        public EconomyState EconomyState { get; }
        public PopulationState PopulationState { get; }
        public MapState MapState { get; }
        public VisibilityState VisibilityState { get; }
        public RankingState RankingState { get; }
        public MatchResultState MatchResultState { get; }
        public SpatialTileIndex SpatialTileIndex { get; }
        public ISpatialIndexService SpatialIndex { get; }
        public IPathQueryService PathQueries { get; }
        public ITrafficReservationService TrafficReservations { get; }
        public IMovementProgressPolicy MovementProgressPolicy { get; }
        public SimDebugCounters DebugCounters { get; }
        public ulong LastChecksum { get; set; }

        public GameState(ulong matchSeed, int playerCount)
        {
            Tick = 0;
            MatchSeed = matchSeed;
            RngState = new DeterministicRandomState(matchSeed);
            EntityState = new EntityState();
            PlayerStates = PlayerStateContainer.Create(playerCount);
            EconomyState = new EconomyState();
            PopulationState = new PopulationState();
            MapState = new MapState(matchSeed, GameData.MapWidthTiles, GameData.MapHeightTiles);
            VisibilityState = new VisibilityState(playerCount, GameData.MapWidthTiles, GameData.MapHeightTiles);
            RankingState = new RankingState(playerCount);
            MatchResultState = new MatchResultState();
            SpatialTileIndex = new SpatialTileIndex();
            SpatialIndex = new SpatialIndexService();
            PathQueries = new DeterministicPathQueryService();
            TrafficReservations = new DeterministicTrafficReservationService();
            MovementProgressPolicy = new DefaultMovementProgressPolicy();
            if (TrafficReservations is DeterministicTrafficReservationService deterministicTraffic)
            {
                deterministicTraffic.LanePreferenceScorer = new DeterministicTrafficLanePreferenceScorer();
            }
            DebugCounters = new SimDebugCounters();
            LastChecksum = 0UL;
        }
    }

    public sealed class SimDebugCounters
    {
        public int ExecutedCommandCount { get; set; }
        public int RejectedCommandCount { get; set; }
        public int DebugCounter { get; set; }
        public CommandType LastCommandType { get; set; }
        public CommandValidationReason LastCommandReason { get; set; }
        public bool LastCommandAccepted { get; set; }
        public int LastCommandPlayerIndex { get; set; }
        public int LastCommandTargetEntityId { get; set; }
        public int LastCommandTargetTileX { get; set; }
        public int LastCommandTargetTileY { get; set; }
        public int LastCommandUnitCount { get; set; }
        public int LastCommandFirstUnitId { get; set; }
        public int PathFindNextCalls { get; set; }
        public int PathFindCostCalls { get; set; }
        public int PathQueryBudgetExceededCount { get; set; }
        public int ReservationRetargetCount { get; set; }
        public int ReservationReleaseCount { get; set; }
    }
}
