using System;
using System.Collections.Generic;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.Snapshots
{
    public static class GameSnapshotBuilder
    {
        public static GameSnapshot Build(GameState state, int localPlayerIndex)
        {
            if (localPlayerIndex < 0 || localPlayerIndex >= state.PlayerStates.Players.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(localPlayerIndex), "Local player index must exist in GameState.");
            }

            var units = new List<UnitSnapshot>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || !IsVisibleToLocalPlayer(state, localPlayerIndex, unit.Position))
                {
                    continue;
                }

                units.Add(new UnitSnapshot(
                    unit.Id,
                    unit.OwnerPlayerIndex,
                    unit.UnitTypeId,
                    unit.Position,
                    unit.HitPoints,
                    unit.HasMoveTarget,
                    unit.MoveTarget,
                    unit.CurrentBuildTargetId,
                    unit.CurrentResourceNodeId,
                    unit.TaskPhase,
                    unit.ReservedInteractionKind,
                    unit.ReservedInteractionTargetId,
                    unit.ReservedInteractionTileX,
                    unit.ReservedInteractionTileY,
                    IsInResourceInteractionRange(state, unit),
                    IsInDropoffInteractionRange(state, unit),
                    IsInBuildInteractionRange(state, unit),
                    unit.LastMovedTick,
                    unit.CarriedResourceType,
                    unit.CarriedAmount,
                    unit.AttackTargetId,
                    unit.AttackCooldownTicksRemaining,
                    unit.TradeRouteAId,
                    unit.TradeRouteBId));
            }

            var buildings = new List<BuildingSnapshot>();
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.IsDead || !IsVisibleToLocalPlayer(state, localPlayerIndex, building.Position))
                {
                    continue;
                }

                buildings.Add(new BuildingSnapshot(
                    building.Id,
                    building.OwnerPlayerIndex,
                    building.BuildingTypeId,
                    building.Position,
                    building.HitPoints,
                    building.IsUnderConstruction,
                    building.BuildProgressTicks,
                    GetRequiredBuildTicks(building.BuildingTypeId),
                    building.TrainingQueue.Count,
                    GetTrainingUnitTypeId(building),
                    GetTrainingProgressTicks(building),
                    GetTrainingRequiredTicks(building),
                    building.IsCapital));
            }

            var resources = new List<ResourceNodeSnapshot>();
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted || !IsVisibleToLocalPlayer(state, localPlayerIndex, node.Position))
                {
                    continue;
                }

                resources.Add(new ResourceNodeSnapshot(
                    node.Id,
                    node.ResourceAreaId,
                    node.ResourceType,
                    node.NodeType,
                    node.GatherProfileId,
                    node.Position,
                    node.RemainingAmount));
            }

            PlayerState player = state.PlayerStates.Players[localPlayerIndex];
            var localPlayer = new LocalPlayerSnapshot(
                player.Resources.Food,
                player.Resources.Wood,
                player.Resources.Gold,
                player.PopulationUsed,
                player.PopulationCap,
                player.CapitalStatus.HasCapitalBeenPlaced,
                player.CapitalStatus.IsCapitalAlive,
                player.CapitalStatus.CapitalBonusActive,
                player.IsConnected,
                player.IsDefeated,
                player.IsResigned,
                BuildCompletedTechs(player),
                BuildResearchQueue(player),
                BuildModifiers(player));

            var match = new MatchSnapshot(
                state.MatchResultState.IsFinished,
                state.MatchResultState.WinnerPlayerIndex,
                state.MatchResultState.FinishedTick);

            return new GameSnapshot(state.Tick, localPlayerIndex, units, buildings, resources, localPlayer, match);
        }

        private static IReadOnlyList<TechId> BuildCompletedTechs(PlayerState player)
        {
            var completed = new List<TechId>();
            for (int i = 0; i < player.TechState.CompletedTechs.Count; i++)
            {
                completed.Add(player.TechState.CompletedTechs[i]);
            }

            return completed;
        }

        private static IReadOnlyList<ResearchSnapshot> BuildResearchQueue(PlayerState player)
        {
            var queue = new List<ResearchSnapshot>();
            for (int i = 0; i < player.TechState.ResearchQueue.Count; i++)
            {
                ResearchQueueItem item = player.TechState.ResearchQueue[i];
                queue.Add(new ResearchSnapshot(item.TechId, item.ProgressTicks, item.RequiredTicks));
            }

            return queue;
        }

        private static IReadOnlyList<ModifierSnapshot> BuildModifiers(PlayerState player)
        {
            var modifiers = new List<ModifierSnapshot>();
            for (int i = 0; i < player.TechState.Modifiers.Count; i++)
            {
                PlayerModifier modifier = player.TechState.Modifiers[i];
                modifiers.Add(new ModifierSnapshot(modifier.ModifierId, modifier.Value));
            }

            return modifiers;
        }

        private static bool IsVisibleToLocalPlayer(GameState state, int localPlayerIndex, FixedVector2 position)
        {
            PlayerVisibility visibility = state.VisibilityState.Players[localPlayerIndex];
            int tileX = SpatialRules.GetTileX(position);
            int tileY = SpatialRules.GetTileY(position);
            if (tileX < 0 || tileY < 0 || tileX >= state.VisibilityState.WidthTiles || tileY >= state.VisibilityState.HeightTiles)
            {
                return false;
            }

            return visibility.VisibleTiles[state.VisibilityState.GetIndex(tileX, tileY)];
        }

        private static bool IsInResourceInteractionRange(GameState state, Unit unit)
        {
            if (unit.CurrentResourceNodeId == 0)
            {
                return false;
            }

            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.Id == unit.CurrentResourceNodeId && !node.IsDepleted)
                {
                    return SpatialRules.IsUnitInResourceInteractionRange(unit, node);
                }
            }

            return false;
        }

        private static bool IsInDropoffInteractionRange(GameState state, Unit unit)
        {
            if (unit.CarriedAmount <= 0 || unit.CarriedResourceType == ResourceType.None)
            {
                return false;
            }

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsDead
                    && !building.IsUnderConstruction
                    && building.OwnerPlayerIndex == unit.OwnerPlayerIndex
                    && building.BuildingTypeId == BuildingTypeId.TownCenter
                    && SpatialRules.IsUnitInBuildingInteractionRange(unit, building))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsInBuildInteractionRange(GameState state, Unit unit)
        {
            if (unit.CurrentBuildTargetId == 0)
            {
                return false;
            }

            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.Id == unit.CurrentBuildTargetId
                    && !building.IsDead
                    && building.IsUnderConstruction)
                {
                    return SpatialRules.IsUnitInBuildInteractionRange(unit, building);
                }
            }

            return false;
        }

        private static int GetRequiredBuildTicks(BuildingTypeId buildingTypeId)
        {
            switch (buildingTypeId)
            {
                case BuildingTypeId.TownCenter:
                    return GameData.TownCenterBuildTicks;
                case BuildingTypeId.Wall:
                    return GameData.WallBuildTicks;
                case BuildingTypeId.TradePost:
                    return GameData.TradePostBuildTicks;
                default:
                    return 0;
            }
        }

        private static UnitTypeId GetTrainingUnitTypeId(Building building)
        {
            if (building.TrainingQueue.Count == 0)
            {
                return 0;
            }

            return building.TrainingQueue[0].UnitTypeId;
        }

        private static int GetTrainingProgressTicks(Building building)
        {
            if (building.TrainingQueue.Count == 0)
            {
                return 0;
            }

            return building.TrainingQueue[0].ProgressTicks;
        }

        private static int GetTrainingRequiredTicks(Building building)
        {
            if (building.TrainingQueue.Count == 0)
            {
                return 0;
            }

            return building.TrainingQueue[0].RequiredTicks;
        }
    }
}
