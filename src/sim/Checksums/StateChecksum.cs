using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Checksums
{
    public static class StateChecksum
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Compute(GameState state, GameRules rules)
        {
            var writer = new CanonicalWriter();
            WriteState(writer, state, rules);
            return Fnv1A64(writer.ToArray());
        }

        private static void WriteState(CanonicalWriter writer, GameState state, GameRules rules)
        {
            writer.WriteUInt32(rules.RulesVersion);
            writer.WriteInt32(rules.TickRate);
            writer.WriteInt32(rules.MaxPlayers);
            writer.WriteInt32(rules.InputDelayTicks);
            writer.WriteInt32(rules.ChecksumIntervalTicks);
            writer.WriteInt32(state.Tick);
            writer.WriteUInt64(state.MatchSeed);
            writer.WriteUInt64(state.RngState.Value);

            writer.WriteInt32(state.EntityState.NextEntityId);
            writer.WriteListCount(state.EntityState.Units.Count);
            foreach (Unit unit in state.EntityState.Units)
            {
                writer.WriteInt32(unit.Id);
                writer.WriteInt32(unit.OwnerPlayerIndex);
                writer.WriteUInt16((ushort)unit.UnitTypeId);
                writer.WriteFixed(unit.Position.X);
                writer.WriteFixed(unit.Position.Y);
                writer.WriteBool(unit.HasMoveTarget);
                writer.WriteFixed(unit.MoveTarget.X);
                writer.WriteFixed(unit.MoveTarget.Y);
                writer.WriteFixed(unit.Velocity.X);
                writer.WriteFixed(unit.Velocity.Y);
                writer.WriteInt32(unit.LastSteeringDecisionTick);
                writer.WriteInt32(unit.CorridorVersion);
                writer.WriteInt32(unit.CorridorStepIndex);
                writer.WriteInt32(unit.RetargetCooldownUntilTick);
                writer.WriteUInt16((ushort)unit.MovementBlockedReason);
                writer.WriteInt32(unit.BlockedSinceTick);
                writer.WriteInt32(unit.LastMeaningfulProgressTick);
                writer.WriteInt32(unit.LastMovedTick);
                writer.WriteInt32(unit.HitPoints);
                writer.WriteInt32(unit.CurrentBuildTargetId);
                writer.WriteInt32(unit.CurrentResourceAreaId);
                writer.WriteInt32(unit.CurrentResourceNodeId);
                writer.WriteUInt16((ushort)unit.TaskPhase);
                writer.WriteUInt16((ushort)unit.ReservedInteractionKind);
                writer.WriteInt32(unit.ReservedInteractionTargetId);
                writer.WriteInt32(unit.ReservedInteractionTileX);
                writer.WriteInt32(unit.ReservedInteractionTileY);
                writer.WriteInt32(unit.LastReservationRetargetTick);
                writer.WriteUInt16((ushort)unit.LastReservationFailureReason);
                writer.WriteInt32(unit.LastReservationFailureTick);
                writer.WriteUInt16((ushort)unit.CarriedResourceType);
                writer.WriteInt32(unit.CarriedAmount);
                writer.WriteInt32(unit.AttackTargetId);
                writer.WriteInt32(unit.AttackCooldownTicksRemaining);
                writer.WriteBool(unit.IsSiegeDeployed);
                writer.WriteInt32(unit.SiegeSetupTicksRemaining);
                writer.WriteInt32(unit.SiegeReloadTicksRemaining);
                writer.WriteInt32(unit.TradeRouteAId);
                writer.WriteInt32(unit.TradeRouteBId);
                writer.WriteInt32(unit.TradeDestinationId);
                writer.WriteInt32(unit.TradeIncomePerTrip);
                writer.WriteInt32(unit.DespawnTicksRemaining);
                writer.WriteBool(unit.IsDead);
            }

            writer.WriteListCount(state.EntityState.Buildings.Count);
            foreach (Building building in state.EntityState.Buildings)
            {
                writer.WriteInt32(building.Id);
                writer.WriteInt32(building.OwnerPlayerIndex);
                writer.WriteUInt16((ushort)building.BuildingTypeId);
                writer.WriteFixed(building.Position.X);
                writer.WriteFixed(building.Position.Y);
                writer.WriteInt32(building.HitPoints);
                writer.WriteBool(building.IsUnderConstruction);
                writer.WriteInt32(building.BuildProgressTicks);
                writer.WriteListCount(building.AssignedBuilderIds.Count);
                foreach (int builderId in building.AssignedBuilderIds)
                {
                    writer.WriteInt32(builderId);
                }

                writer.WriteListCount(building.TrainingQueue.Count);
                foreach (TrainingQueueItem item in building.TrainingQueue)
                {
                    writer.WriteUInt16((ushort)item.UnitTypeId);
                    writer.WriteInt32(item.ProgressTicks);
                    writer.WriteInt32(item.RequiredTicks);
                }

                writer.WriteBool(building.IsCapital);
                writer.WriteInt32(building.DespawnTicksRemaining);
                writer.WriteBool(building.IsDead);
            }

            writer.WriteListCount(state.PlayerStates.Players.Count);
            foreach (PlayerState player in state.PlayerStates.Players)
            {
                writer.WriteInt32(player.PlayerIndex);
                writer.WriteBool(player.IsConnected);
                writer.WriteBool(player.IsDefeated);
                writer.WriteBool(player.IsResigned);
                writer.WriteInt32(player.Placement);
                writer.WriteInt32(player.PopulationUsed);
                writer.WriteInt32(player.PopulationCap);
                writer.WriteInt32(player.Resources.Food);
                writer.WriteInt32(player.Resources.Wood);
                writer.WriteInt32(player.Resources.Gold);
                writer.WriteBool(player.CapitalStatus.HasCapitalBeenPlaced);
                writer.WriteInt32(player.CapitalStatus.CapitalBuildingId);
                writer.WriteBool(player.CapitalStatus.IsCapitalAlive);
                writer.WriteBool(player.CapitalStatus.CapitalBonusActive);
                writer.WriteListCount(player.TechState.CompletedTechs.Count);
                foreach (TechId techId in player.TechState.CompletedTechs)
                {
                    writer.WriteUInt16((ushort)techId);
                }

                writer.WriteListCount(player.TechState.ResearchQueue.Count);
                foreach (ResearchQueueItem item in player.TechState.ResearchQueue)
                {
                    writer.WriteUInt16((ushort)item.TechId);
                    writer.WriteInt32(item.ProgressTicks);
                    writer.WriteInt32(item.RequiredTicks);
                }

                writer.WriteListCount(player.TechState.Modifiers.Count);
                foreach (PlayerModifier modifier in player.TechState.Modifiers)
                {
                    writer.WriteUInt16((ushort)modifier.ModifierId);
                    writer.WriteInt32(modifier.Value);
                }
            }

            writer.WriteInt32(state.RankingState.NextPlacement);
            writer.WriteListCount(state.RankingState.PlacementOrder.Count);
            foreach (int playerIndex in state.RankingState.PlacementOrder)
            {
                writer.WriteInt32(playerIndex);
            }

            writer.WriteBool(state.MatchResultState.IsFinished);
            writer.WriteInt32(state.MatchResultState.WinnerPlayerIndex);
            writer.WriteInt32(state.MatchResultState.FinishedTick);

            writer.WriteUInt32(state.EconomyState.PlaceholderVersion);
            writer.WriteInt32(state.EconomyState.NextResourceAreaId);
            writer.WriteListCount(state.EconomyState.ResourceAreas.Count);
            foreach (ResourceArea area in state.EconomyState.ResourceAreas)
            {
                writer.WriteInt32(area.Id);
                writer.WriteUInt16((ushort)area.AreaType);
                writer.WriteUInt16((ushort)area.ResourceType);
                writer.WriteUInt16((ushort)area.GatherProfileId);
                writer.WriteFixed(area.Position.X);
                writer.WriteFixed(area.Position.Y);
            }

            writer.WriteInt32(state.EconomyState.NextResourceNodeId);
            writer.WriteListCount(state.EconomyState.ResourceNodes.Count);
            foreach (ResourceNode node in state.EconomyState.ResourceNodes)
            {
                writer.WriteInt32(node.Id);
                writer.WriteInt32(node.ResourceAreaId);
                writer.WriteUInt16((ushort)node.ResourceType);
                writer.WriteUInt16((ushort)node.NodeType);
                writer.WriteUInt16((ushort)node.GatherProfileId);
                writer.WriteFixed(node.Position.X);
                writer.WriteFixed(node.Position.Y);
                writer.WriteInt32(node.RemainingAmount);
            }

            writer.WriteUInt32(state.PopulationState.PlaceholderVersion);
            writer.WriteUInt64(state.MapState.MapSeed);
            writer.WriteInt32(state.MapState.WidthTiles);
            writer.WriteInt32(state.MapState.HeightTiles);
            writer.WriteUInt32(state.MapState.PlaceholderVersion);
            writer.WriteUInt32(state.VisibilityState.PlaceholderVersion);
            writer.WriteInt32(state.VisibilityState.WidthTiles);
            writer.WriteInt32(state.VisibilityState.HeightTiles);
            writer.WriteListCount(state.VisibilityState.Players.Count);
            foreach (PlayerVisibility visibility in state.VisibilityState.Players)
            {
                writer.WriteListCount(visibility.VisibleTiles.Length);
                for (int i = 0; i < visibility.VisibleTiles.Length; i++)
                {
                    writer.WriteBool(visibility.VisibleTiles[i]);
                }

                writer.WriteListCount(visibility.ExploredTiles.Length);
                for (int i = 0; i < visibility.ExploredTiles.Length; i++)
                {
                    writer.WriteBool(visibility.ExploredTiles[i]);
                }
            }

            writer.WriteInt32(state.DebugCounters.ExecutedCommandCount);
            writer.WriteInt32(state.DebugCounters.RejectedCommandCount);
            writer.WriteInt32(state.DebugCounters.DebugCounter);
            writer.WriteUInt16((ushort)state.DebugCounters.LastCommandType);
            writer.WriteUInt16((ushort)state.DebugCounters.LastCommandReason);
            writer.WriteBool(state.DebugCounters.LastCommandAccepted);
            writer.WriteInt32(state.DebugCounters.LastCommandPlayerIndex);
            writer.WriteInt32(state.DebugCounters.LastCommandTargetEntityId);
            writer.WriteInt32(state.DebugCounters.LastCommandTargetTileX);
            writer.WriteInt32(state.DebugCounters.LastCommandTargetTileY);
            writer.WriteInt32(state.DebugCounters.LastCommandUnitCount);
            writer.WriteInt32(state.DebugCounters.LastCommandFirstUnitId);
        }

        private static ulong Fnv1A64(byte[] bytes)
        {
            ulong hash = Offset;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= Prime;
            }

            return hash;
        }
    }
}
