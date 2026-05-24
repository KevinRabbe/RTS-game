using System;
using RtsGame.Presentation.ClientInput;
using RtsGame.Presentation.LocalPlay;
using RtsGame.Presentation.Snapshots;
using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.GodotBridge
{
    public sealed class GodotClientFacade
    {
        private readonly LocalPlaySession _session;

        private GodotClientFacade(LocalPlaySession session)
        {
            _session = session;
        }

        public int CurrentTick
        {
            get { return _session.CurrentTick; }
        }

        public int RejectedCommandCount
        {
            get { return _session.RejectedCommandCount; }
        }

        public int ExecutedCommandCount
        {
            get { return _session.ExecutedCommandCount; }
        }

        public int PlayerCount
        {
            get { return _session.PlayerCount; }
        }

        public string MapName
        {
            get { return _session.MapName; }
        }

        public int MapWidthTiles
        {
            get { return GameData.MapWidthTiles; }
        }

        public int MapHeightTiles
        {
            get { return GameData.MapHeightTiles; }
        }

        public static GodotClientFacade CreateLocal1v1(ulong matchSeed)
        {
            return new GodotClientFacade(LocalPlaySession.Create1v1(matchSeed));
        }

        public static GodotClientFacade CreateDryArabiaTest01(ulong matchSeed)
        {
            return new GodotClientFacade(LocalPlaySession.CreateDryArabiaTest01(matchSeed));
        }

        public static GodotClientFacade CreateCombatTest01(ulong matchSeed)
        {
            return new GodotClientFacade(LocalPlaySession.CreateCombatTest01(matchSeed));
        }

        public static GodotClientFacade CreateLocal6PlayerFfa(ulong matchSeed)
        {
            return new GodotClientFacade(LocalPlaySession.Create6PlayerFfa(matchSeed));
        }

        public static GodotClientFacade CreateLocal(ulong matchSeed, int playerCount)
        {
            return new GodotClientFacade(LocalPlaySession.Create(matchSeed, playerCount));
        }

        public void AdvanceOneTick()
        {
            _session.AdvanceOneTick();
        }

        public void AdvanceTicks(int tickCount)
        {
            _session.AdvanceTicks(tickCount);
        }

        public void QueueNoOp(int playerIndex)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.NoOp());
        }

        public void QueuePlaceTownCenter(int playerIndex, int tileX, int tileY)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(tileX, tileY)));
        }

        public void QueueAssignBuild(int playerIndex, int buildingId, int[] builderUnitIds)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.AssignBuild(buildingId, builderUnitIds));
        }

        public void QueueGatherResource(int playerIndex, int resourceNodeId, int[] unitIds)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.GatherResource(resourceNodeId, unitIds));
        }

        public void QueueTrainUnit(int playerIndex, int buildingId, int unitTypeId)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.TrainUnit(buildingId, ToUnitTypeId(unitTypeId)));
        }

        public void QueueResearchTech(int playerIndex, int buildingId, int techId)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.ResearchTech(buildingId, ToTechId(techId)));
        }

        public void QueueMoveUnits(int playerIndex, int[] unitIds, int tileX, int tileY)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.MoveUnits(unitIds, FixedVector2.FromInts(tileX, tileY)));
        }

        public void QueueAttack(int playerIndex, int[] attackerUnitIds, int targetEntityId)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.Attack(attackerUnitIds, targetEntityId));
        }

        public void QueueAttackMove(int playerIndex, int[] attackerUnitIds, int tileX, int tileY)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.AttackMove(attackerUnitIds, FixedVector2.FromInts(tileX, tileY)));
        }

        public void QueuePlaceWall(int playerIndex, int tileX, int tileY)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.PlaceWall(FixedVector2.FromInts(tileX, tileY)));
        }

        public void QueuePlaceTradePost(int playerIndex, int tileX, int tileY)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.PlaceTradePost(FixedVector2.FromInts(tileX, tileY)));
        }

        public void QueueCreateTradeRoute(int playerIndex, int tradeCartId, int tradeRouteAId, int tradeRouteBId)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.CreateTradeRoute(tradeCartId, tradeRouteAId, tradeRouteBId));
        }

        public void QueueResign(int playerIndex)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.Resign());
        }

        public GodotFrameDto GetFrame(int localPlayerIndex)
        {
            GameSnapshot snapshot = _session.GetSnapshot(localPlayerIndex);
            VisualFrame frame = _session.GetVisualFrame(localPlayerIndex);
            var primitives = new GodotPrimitiveDto[frame.Primitives.Count];
            for (int i = 0; i < frame.Primitives.Count; i++)
            {
                primitives[i] = ToPrimitiveDto(frame.Primitives[i]);
            }

            var unitStatuses = new GodotUnitStatusDto[snapshot.Units.Count];
            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                unitStatuses[i] = ToUnitStatusDto(snapshot.Units[i]);
            }

            var buildingStatuses = new GodotBuildingStatusDto[snapshot.Buildings.Count];
            for (int i = 0; i < snapshot.Buildings.Count; i++)
            {
                buildingStatuses[i] = ToBuildingStatusDto(snapshot.Buildings[i]);
            }

            return new GodotFrameDto(
                frame.Tick,
                _session.MapName,
                snapshot.LocalPlayerIndex,
                ToLocalPlayerDto(snapshot.LocalPlayer),
                ToMatchDto(
                    snapshot.Match,
                    _session.ExecutedCommandCount,
                    _session.RejectedCommandCount,
                    _session.LastCommandType,
                    _session.LastCommandReason,
                    _session.LastCommandAccepted,
                    _session.LastCommandPlayerIndex,
                    _session.LastCommandTargetEntityId,
                    _session.LastCommandTargetTileX,
                    _session.LastCommandTargetTileY,
                    _session.LastCommandUnitCount,
                    _session.LastCommandFirstUnitId),
                primitives,
                unitStatuses,
                buildingStatuses);
        }

        private static UnitTypeId ToUnitTypeId(int unitTypeId)
        {
            if (unitTypeId < ushort.MinValue || unitTypeId > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(unitTypeId), "Unit type id is outside the valid range.");
            }

            ushort rawUnitTypeId = (ushort)unitTypeId;
            if (!Enum.IsDefined(typeof(UnitTypeId), rawUnitTypeId))
            {
                throw new ArgumentOutOfRangeException(nameof(unitTypeId), "Unit type id is not defined.");
            }

            return (UnitTypeId)rawUnitTypeId;
        }

        private static TechId ToTechId(int techId)
        {
            if (techId < ushort.MinValue || techId > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(techId), "Tech id is outside the valid range.");
            }

            ushort rawTechId = (ushort)techId;
            if (!Enum.IsDefined(typeof(TechId), rawTechId))
            {
                throw new ArgumentOutOfRangeException(nameof(techId), "Tech id is not defined.");
            }

            return (TechId)rawTechId;
        }

        private static GodotPrimitiveDto ToPrimitiveDto(VisualPrimitive primitive)
        {
            return new GodotPrimitiveDto(
                (int)primitive.Kind,
                primitive.EntityId,
                primitive.TypeId,
                primitive.OwnerPlayerIndex,
                primitive.Position.X.Raw,
                primitive.Position.Y.Raw,
                primitive.EndPosition.X.Raw,
                primitive.EndPosition.Y.Raw,
                primitive.Size.Raw,
                primitive.Width.Raw,
                primitive.Height.Raw,
                primitive.CurrentHitPoints,
                primitive.MaxHitPoints,
                primitive.IsCapital);
        }

        private static GodotLocalPlayerDto ToLocalPlayerDto(LocalPlayerSnapshot snapshot)
        {
            return new GodotLocalPlayerDto(
                snapshot.Food,
                snapshot.Wood,
                snapshot.Gold,
                snapshot.PopulationUsed,
                snapshot.PopulationCap,
                snapshot.HasCapitalBeenPlaced,
                snapshot.IsCapitalAlive,
                snapshot.CapitalBonusActive,
                snapshot.IsConnected,
                snapshot.IsDefeated,
                snapshot.IsResigned,
                ToCompletedTechIds(snapshot),
                ToResearchStatusDtos(snapshot),
                ToModifierStatusDtos(snapshot));
        }

        private static int[] ToCompletedTechIds(LocalPlayerSnapshot snapshot)
        {
            var ids = new int[snapshot.CompletedTechs.Count];
            for (int i = 0; i < snapshot.CompletedTechs.Count; i++)
            {
                ids[i] = (int)snapshot.CompletedTechs[i];
            }

            return ids;
        }

        private static GodotResearchStatusDto[] ToResearchStatusDtos(LocalPlayerSnapshot snapshot)
        {
            var research = new GodotResearchStatusDto[snapshot.ResearchQueue.Count];
            for (int i = 0; i < snapshot.ResearchQueue.Count; i++)
            {
                ResearchSnapshot item = snapshot.ResearchQueue[i];
                research[i] = new GodotResearchStatusDto((int)item.TechId, item.ProgressTicks, item.RequiredTicks);
            }

            return research;
        }

        private static GodotModifierStatusDto[] ToModifierStatusDtos(LocalPlayerSnapshot snapshot)
        {
            var modifiers = new GodotModifierStatusDto[snapshot.Modifiers.Count];
            for (int i = 0; i < snapshot.Modifiers.Count; i++)
            {
                ModifierSnapshot modifier = snapshot.Modifiers[i];
                modifiers[i] = new GodotModifierStatusDto((int)modifier.ModifierId, modifier.Value);
            }

            return modifiers;
        }

        private static GodotUnitStatusDto ToUnitStatusDto(UnitSnapshot snapshot)
        {
            return new GodotUnitStatusDto(
                snapshot.Id,
                (int)snapshot.UnitTypeId,
                snapshot.HitPoints,
                GameData.GetUnitHitPoints(snapshot.UnitTypeId),
                snapshot.HasMoveTarget,
                snapshot.MoveTarget.X.Raw,
                snapshot.MoveTarget.Y.Raw,
                snapshot.CurrentBuildTargetId,
                snapshot.CurrentResourceNodeId,
                SpatialRules.GetTileX(snapshot.Position),
                SpatialRules.GetTileY(snapshot.Position),
                snapshot.Position.X.Raw,
                snapshot.Position.Y.Raw,
                (int)snapshot.TaskPhase,
                (int)snapshot.ReservedInteractionKind,
                snapshot.ReservedInteractionTargetId,
                snapshot.ReservedInteractionTileX,
                snapshot.ReservedInteractionTileY,
                snapshot.InResourceInteractionRange,
                snapshot.InDropoffInteractionRange,
                snapshot.InBuildInteractionRange,
                snapshot.LastMovedTick,
                (int)snapshot.CarriedResourceType,
                snapshot.CarriedAmount,
                snapshot.AttackTargetId,
                snapshot.AttackCooldownTicksRemaining,
                snapshot.HasAttackMoveTarget,
                snapshot.AttackMoveTarget.X.Raw,
                snapshot.AttackMoveTarget.Y.Raw,
                snapshot.NextAttackMoveAcquireTick);
        }

        private static GodotBuildingStatusDto ToBuildingStatusDto(BuildingSnapshot snapshot)
        {
            return new GodotBuildingStatusDto(
                snapshot.Id,
                (int)snapshot.BuildingTypeId,
                snapshot.HitPoints,
                GameData.GetBuildingCompletedHitPoints(snapshot.BuildingTypeId, snapshot.IsCapital),
                snapshot.IsUnderConstruction,
                snapshot.BuildProgressTicks,
                snapshot.RequiredBuildTicks,
                snapshot.TrainingQueueCount,
                (int)snapshot.TrainingUnitTypeId,
                snapshot.TrainingProgressTicks,
                snapshot.TrainingRequiredTicks);
        }

        private static GodotMatchDto ToMatchDto(
            MatchSnapshot snapshot,
            int executedCommandCount,
            int rejectedCommandCount,
            CommandType lastCommandType,
            CommandValidationReason lastCommandReason,
            bool lastCommandAccepted,
            int lastCommandPlayerIndex,
            int lastCommandTargetEntityId,
            int lastCommandTargetTileX,
            int lastCommandTargetTileY,
            int lastCommandUnitCount,
            int lastCommandFirstUnitId)
        {
            return new GodotMatchDto(
                snapshot.IsFinished,
                snapshot.WinnerPlayerIndex,
                snapshot.FinishedTick,
                executedCommandCount,
                rejectedCommandCount,
                (int)lastCommandType,
                (int)lastCommandReason,
                lastCommandAccepted,
                lastCommandPlayerIndex,
                lastCommandTargetEntityId,
                lastCommandTargetTileX,
                lastCommandTargetTileY,
                lastCommandUnitCount,
                lastCommandFirstUnitId);
        }
    }
}
