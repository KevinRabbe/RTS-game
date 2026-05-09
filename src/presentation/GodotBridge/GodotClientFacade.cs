using System;
using RtsGame.Presentation.ClientInput;
using RtsGame.Presentation.LocalPlay;
using RtsGame.Presentation.Snapshots;
using RtsGame.Presentation.Visuals;
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

        public static GodotClientFacade CreateLocal1v1(ulong matchSeed)
        {
            return new GodotClientFacade(LocalPlaySession.Create1v1(matchSeed));
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

        public void QueueMoveUnits(int playerIndex, int[] unitIds, int tileX, int tileY)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.MoveUnits(unitIds, FixedVector2.FromInts(tileX, tileY)));
        }

        public void QueueAttack(int playerIndex, int[] attackerUnitIds, int targetEntityId)
        {
            _session.QueueIntent(playerIndex, ClientCommandIntent.Attack(attackerUnitIds, targetEntityId));
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

            var buildingStatuses = new GodotBuildingStatusDto[snapshot.Buildings.Count];
            for (int i = 0; i < snapshot.Buildings.Count; i++)
            {
                buildingStatuses[i] = ToBuildingStatusDto(snapshot.Buildings[i]);
            }

            return new GodotFrameDto(
                frame.Tick,
                snapshot.LocalPlayerIndex,
                ToLocalPlayerDto(snapshot.LocalPlayer),
                ToMatchDto(snapshot.Match),
                primitives,
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
                snapshot.CapitalBonusActive);
        }

        private static GodotBuildingStatusDto ToBuildingStatusDto(BuildingSnapshot snapshot)
        {
            return new GodotBuildingStatusDto(
                snapshot.Id,
                (int)snapshot.BuildingTypeId,
                snapshot.IsUnderConstruction,
                snapshot.BuildProgressTicks,
                snapshot.RequiredBuildTicks,
                snapshot.TrainingQueueCount,
                (int)snapshot.TrainingUnitTypeId,
                snapshot.TrainingProgressTicks,
                snapshot.TrainingRequiredTicks);
        }

        private static GodotMatchDto ToMatchDto(MatchSnapshot snapshot)
        {
            return new GodotMatchDto(snapshot.IsFinished, snapshot.WinnerPlayerIndex, snapshot.FinishedTick);
        }
    }
}
