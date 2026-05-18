using RtsGame.Sim.Commands;
using RtsGame.Sim.Systems;

namespace RtsGame.Sim.Core
{
    public sealed class TickRunner
    {
        private readonly ISimSystem[] _systems;

        public TickRunner()
        {
            _systems = new ISimSystem[]
            {
                new CommandValidationSystem(),
                new CommandExecutionSystem(),
                new DespawnSystem(),
                new ResignationSystem(),
                new MovementSystem(),
                new VisibilitySystem(),
                new ConstructionSystem(),
                new ResourceGatherSystem(),
                new ResourceDepositSystem(),
                new TrainingSystem(),
                new ResearchSystem(),
                new TradeRouteSystem(),
                new SiegeSetupSystem(),
                new SiegeAttackSystem(),
                new AreaDamageSystem(),
                new CombatResolutionSystem(),
                new DeathMarkSystem(),
                new CapitalSystem(),
                new EliminationSystem(),
                new RankingSystem(),
                new MatchEndSystem(),
                new CleanupSystem(),
                new ChecksumSystem()
            };
        }

        public void AdvanceOneTick(GameState state, GameRules rules, CommandBuffer commandBuffer)
        {
            state.DebugCounters.PathFindNextCalls = 0;
            state.DebugCounters.PathFindCostCalls = 0;
            state.DebugCounters.PathQueryBudgetExceededCount = 0;
            state.DebugCounters.ReservationRetargetCount = 0;
            state.DebugCounters.ReservationReleaseCount = 0;
            var context = new TickCommandContext(commandBuffer.GetCommandsForTick(state.Tick));
            for (int i = 0; i < _systems.Length; i++)
            {
                state.SpatialTileIndex.Rebuild(state);
                _systems[i].Run(state, rules, context);
            }

            state.Tick++;
            commandBuffer.ClearBeforeTick(state.Tick);
        }
    }
}
