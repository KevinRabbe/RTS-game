using System.Collections.Generic;
using RtsGame.Net.Lockstep;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;

namespace RtsGame.Stress
{
    public sealed class StressScenarioRunner
    {
        public StressScenarioResult RunChaosV1(int ticks, ulong seed)
        {
            var scenario = new ChaosV1Scenario(ticks);
            return RunScenario(scenario, seed);
        }

        public StressScenarioResult RunChaosV2(int ticks, ulong seed)
        {
            var scenario = new ChaosV2Scenario(ticks);
            return RunScenario(scenario, seed);
        }

        public StressScenarioResult RunChaosV3(int ticks, ulong seed)
        {
            var scenario = new ChaosV3Scenario(ticks);
            return RunScenario(scenario, seed);
        }

        private StressScenarioResult RunScenario(IStressScenario scenario, ulong seed)
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(scenario.PlayerCount);
            var session = new LockstepSession(rules, seed, true);
            for (int i = 0; i < session.Peers.Count; i++)
            {
                scenario.PrepareInitialState(session.Peers[i].LocalState);
            }

            int commandCount = 0;
            for (int tick = 0; tick < scenario.Ticks; tick++)
            {
                List<CommandEnvelope> commands = BuildTickCommands(scenario, tick);
                commandCount += commands.Count;
                for (int i = 0; i < commands.Count; i++)
                {
                    session.Broadcast(commands[i]);
                }

                if (!session.TryAdvanceOneTick())
                {
                    var stalled = new StressScenarioResult(scenario.ScenarioName, scenario.ScenarioVersion, session.CurrentTick, session.Peers[0].LocalState.LastChecksum, commandCount, session.DesyncReports.Count);
                    stalled.InvariantFailures.Add("lockstep stalled at tick " + tick);
                    return stalled;
                }
            }

            ulong checksum = session.Peers[0].LocalState.LastChecksum;
            var result = new StressScenarioResult(scenario.ScenarioName, scenario.ScenarioVersion, session.CurrentTick, checksum, commandCount, session.DesyncReports.Count);
            ValidatePeerChecksums(session, result);
            ValidateReplayAgreement(scenario, rules, seed, scenario.Ticks, checksum, result);
            ValidateState(session.Peers[0].LocalState, rules, result);
            return result;
        }

        private static List<CommandEnvelope> BuildTickCommands(IStressScenario scenario, int tick)
        {
            var commands = new List<CommandEnvelope>();
            var hasPlayerCommand = new bool[scenario.PlayerCount];
            IReadOnlyList<CommandEnvelope> scripted = scenario.GetCommandsForTick(tick);
            for (int i = 0; i < scripted.Count; i++)
            {
                CommandEnvelope command = scripted[i];
                commands.Add(command);
                if (command.Header.PlayerIndex >= 0 && command.Header.PlayerIndex < hasPlayerCommand.Length)
                {
                    hasPlayerCommand[command.Header.PlayerIndex] = true;
                }
            }

            for (int player = 0; player < hasPlayerCommand.Length; player++)
            {
                if (!hasPlayerCommand[player])
                {
                    uint sequence = (uint)(1000000 + tick);
                    commands.Add(new CommandEnvelope(new CommandHeader(tick, player, sequence, CommandType.NoOp), new NoOpCommand()));
                }
            }

            return commands;
        }

        private static void ValidatePeerChecksums(LockstepSession session, StressScenarioResult result)
        {
            ulong checksum = session.Peers[0].LocalState.LastChecksum;
            for (int i = 1; i < session.Peers.Count; i++)
            {
                if (session.Peers[i].LocalState.LastChecksum != checksum)
                {
                    result.InvariantFailures.Add("peer checksum mismatch at peer " + i);
                }
            }
        }

        private static void ValidateReplayAgreement(IStressScenario scenario, GameRules rules, ulong seed, int ticks, ulong expectedChecksum, StressScenarioResult result)
        {
            ulong first = RunSingleSimulation(scenario, rules, seed, ticks);
            ulong second = RunSingleSimulation(scenario, rules, seed, ticks);
            if (first != second)
            {
                result.InvariantFailures.Add("single-run replay checksums differ");
            }

            if (first != expectedChecksum)
            {
                result.InvariantFailures.Add("single-run replay checksum differs from lockstep checksum");
            }
        }

        private static ulong RunSingleSimulation(IStressScenario scenario, GameRules rules, ulong seed, int ticks)
        {
            GameState state = GameInitializer.CreateNomadStart(seed, scenario.PlayerCount);
            scenario.PrepareInitialState(state);
            var runner = new TickRunner();
            var buffer = new CommandBuffer();
            for (int tick = 0; tick < ticks; tick++)
            {
                List<CommandEnvelope> commands = BuildTickCommands(scenario, tick);
                for (int i = 0; i < commands.Count; i++)
                {
                    buffer.Add(commands[i]);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            return state.LastChecksum;
        }

        private static void ValidateState(GameState state, GameRules rules, StressScenarioResult result)
        {
            if (!state.MatchResultState.IsFinished || state.MatchResultState.WinnerPlayerIndex != 5)
            {
                result.InvariantFailures.Add("match result did not finish with player 5 as winner");
            }

            if (state.RankingState.PlacementOrder.Count != rules.MaxPlayers)
            {
                result.InvariantFailures.Add("ranking placement order does not include all players");
            }

            ValidateOwners(state, result);
            ValidateLookup(state, result);
            ValidatePopulation(state, result);
            ValidateResources(state, result);
            ValidateTradeRoutes(state, result);
            ValidateUnitTileOccupancy(state, result);
            ValidateUnitWallOccupancy(state, result);
        }

        private static void ValidateOwners(GameState state, StressScenarioResult result)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead)
                {
                    result.InvariantFailures.Add("dead unit remains in entity list: " + unit.Id);
                }

                if (unit.OwnerPlayerIndex < GameData.NeutralOwnerPlayerIndex || unit.OwnerPlayerIndex >= state.PlayerStates.Players.Count)
                {
                    result.InvariantFailures.Add("unit has invalid owner: " + unit.Id);
                }
            }

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead)
                {
                    result.InvariantFailures.Add("dead building remains in entity list: " + building.Id);
                }

                if (building.OwnerPlayerIndex < GameData.NeutralOwnerPlayerIndex || building.OwnerPlayerIndex >= state.PlayerStates.Players.Count)
                {
                    result.InvariantFailures.Add("building has invalid owner: " + building.Id);
                }
            }
        }

        private static void ValidateLookup(GameState state, StressScenarioResult result)
        {
            int expectedCount = state.EntityState.Units.Count + state.EntityState.Buildings.Count;
            if (state.EntityState.EntityLookup.Count != expectedCount)
            {
                result.InvariantFailures.Add("entity lookup count does not match entity lists");
            }

            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!state.EntityState.EntityLookup.TryGetValue(unit.Id, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit || entityRef.Index != i)
                {
                    result.InvariantFailures.Add("unit lookup mismatch: " + unit.Id);
                }
            }

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!state.EntityState.EntityLookup.TryGetValue(building.Id, out EntityRef entityRef) || entityRef.Kind != EntityKind.Building || entityRef.Index != i)
                {
                    result.InvariantFailures.Add("building lookup mismatch: " + building.Id);
                }
            }
        }

        private static void ValidatePopulation(GameState state, StressScenarioResult result)
        {
            var expectedPopulation = new int[state.PlayerStates.Players.Count];
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.OwnerPlayerIndex >= 0 && unit.OwnerPlayerIndex < expectedPopulation.Length)
                {
                    expectedPopulation[unit.OwnerPlayerIndex] += GameData.GetUnitPopulation(unit.UnitTypeId);
                }
            }

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.OwnerPlayerIndex < 0 || building.OwnerPlayerIndex >= expectedPopulation.Length)
                {
                    continue;
                }

                for (int q = 0; q < building.TrainingQueue.Count; q++)
                {
                    expectedPopulation[building.OwnerPlayerIndex] += GameData.GetUnitPopulation(building.TrainingQueue[q].UnitTypeId);
                }
            }

            for (int player = 0; player < state.PlayerStates.Players.Count; player++)
            {
                PlayerState playerState = state.PlayerStates.Players[player];
                if (playerState.PopulationUsed < 0)
                {
                    result.InvariantFailures.Add("negative population for player " + player);
                }

                if (playerState.PopulationUsed != expectedPopulation[player])
                {
                    result.InvariantFailures.Add("population mismatch for player " + player);
                }
            }
        }

        private static void ValidateResources(GameState state, StressScenarioResult result)
        {
            for (int player = 0; player < state.PlayerStates.Players.Count; player++)
            {
                ResourceStockpile resources = state.PlayerStates.Players[player].Resources;
                if (resources.Food < 0 || resources.Wood < 0 || resources.Gold < 0)
                {
                    result.InvariantFailures.Add("negative resources for player " + player);
                }
            }
        }

        private static void ValidateTradeRoutes(GameState state, StressScenarioResult result)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.TradeRouteAId == 0 && unit.TradeRouteBId == 0 && unit.TradeDestinationId == 0)
                {
                    continue;
                }

                if (!HasCompletedTradePost(state, unit.TradeRouteAId) || !HasCompletedTradePost(state, unit.TradeRouteBId) || !HasCompletedTradePost(state, unit.TradeDestinationId))
                {
                    result.InvariantFailures.Add("trade route references missing endpoint for unit " + unit.Id);
                }
            }
        }

        private static bool HasCompletedTradePost(GameState state, int buildingId)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.Id == buildingId && !building.IsDead && !building.IsUnderConstruction && building.BuildingTypeId == BuildingTypeId.TradePost)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateUnitTileOccupancy(GameState state, StressScenarioResult result)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead)
                {
                    continue;
                }

                int tileX = SpatialRules.GetTileX(unit.Position);
                int tileY = SpatialRules.GetTileY(unit.Position);
                for (int other = i + 1; other < state.EntityState.Units.Count; other++)
                {
                    Unit otherUnit = state.EntityState.Units[other];
                    if (otherUnit.IsDead)
                    {
                        continue;
                    }

                    if (tileX == SpatialRules.GetTileX(otherUnit.Position) && tileY == SpatialRules.GetTileY(otherUnit.Position))
                    {
                        result.InvariantFailures.Add("two living units occupy tile " + tileX + "," + tileY + ": " + unit.Id + " and " + otherUnit.Id);
                    }
                }
            }
        }

        private static void ValidateUnitWallOccupancy(GameState state, StressScenarioResult result)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!unit.IsDead && SpatialRules.IsBlockedByWall(state, unit.Position))
                {
                    result.InvariantFailures.Add("living unit occupies wall-blocked tile: " + unit.Id);
                }
            }
        }
    }
}
