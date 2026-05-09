using System.Collections.Generic;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.ClientInput
{
    public sealed class ClientCommandIntent
    {
        public ClientIntentType IntentType { get; }
        public FixedVector2 Position { get; }
        public IReadOnlyList<int> UnitIds { get; }
        public int TargetEntityId { get; }
        public int BuildingId { get; }
        public int ResourceNodeId { get; }
        public UnitTypeId UnitTypeId { get; }
        public int TradeCartId { get; }
        public int TradeRouteAId { get; }
        public int TradeRouteBId { get; }

        private ClientCommandIntent(
            ClientIntentType intentType,
            FixedVector2 position,
            IReadOnlyList<int> unitIds,
            int targetEntityId,
            int buildingId,
            int resourceNodeId,
            UnitTypeId unitTypeId,
            int tradeCartId,
            int tradeRouteAId,
            int tradeRouteBId)
        {
            IntentType = intentType;
            Position = position;
            UnitIds = unitIds;
            TargetEntityId = targetEntityId;
            BuildingId = buildingId;
            ResourceNodeId = resourceNodeId;
            UnitTypeId = unitTypeId;
            TradeCartId = tradeCartId;
            TradeRouteAId = tradeRouteAId;
            TradeRouteBId = tradeRouteBId;
        }

        public static ClientCommandIntent NoOp()
        {
            return new ClientCommandIntent(ClientIntentType.NoOp, FixedVector2.FromInts(0, 0), new int[0], 0, 0, 0, 0, 0, 0, 0);
        }

        public static ClientCommandIntent PlaceTownCenter(FixedVector2 position)
        {
            return new ClientCommandIntent(ClientIntentType.PlaceTownCenter, position, new int[0], 0, 0, 0, 0, 0, 0, 0);
        }

        public static ClientCommandIntent AssignBuild(int buildingId, IReadOnlyList<int> builderUnitIds)
        {
            return new ClientCommandIntent(ClientIntentType.AssignBuild, FixedVector2.FromInts(0, 0), builderUnitIds, 0, buildingId, 0, 0, 0, 0, 0);
        }

        public static ClientCommandIntent GatherResource(int resourceNodeId, IReadOnlyList<int> unitIds)
        {
            return new ClientCommandIntent(ClientIntentType.GatherResource, FixedVector2.FromInts(0, 0), unitIds, 0, 0, resourceNodeId, 0, 0, 0, 0);
        }

        public static ClientCommandIntent TrainUnit(int buildingId, UnitTypeId unitTypeId)
        {
            return new ClientCommandIntent(ClientIntentType.TrainUnit, FixedVector2.FromInts(0, 0), new int[0], 0, buildingId, 0, unitTypeId, 0, 0, 0);
        }

        public static ClientCommandIntent MoveUnits(IReadOnlyList<int> unitIds, FixedVector2 target)
        {
            return new ClientCommandIntent(ClientIntentType.MoveUnits, target, unitIds, 0, 0, 0, 0, 0, 0, 0);
        }

        public static ClientCommandIntent Attack(IReadOnlyList<int> attackerUnitIds, int targetEntityId)
        {
            return new ClientCommandIntent(ClientIntentType.Attack, FixedVector2.FromInts(0, 0), attackerUnitIds, targetEntityId, 0, 0, 0, 0, 0, 0);
        }

        public static ClientCommandIntent Resign()
        {
            return new ClientCommandIntent(ClientIntentType.Resign, FixedVector2.FromInts(0, 0), new int[0], 0, 0, 0, 0, 0, 0, 0);
        }

        public static ClientCommandIntent PlaceWall(FixedVector2 position)
        {
            return new ClientCommandIntent(ClientIntentType.PlaceWall, position, new int[0], 0, 0, 0, 0, 0, 0, 0);
        }

        public static ClientCommandIntent CreateTradeRoute(int tradeCartId, int tradeRouteAId, int tradeRouteBId)
        {
            return new ClientCommandIntent(ClientIntentType.CreateTradeRoute, FixedVector2.FromInts(0, 0), new int[0], 0, 0, 0, 0, tradeCartId, tradeRouteAId, tradeRouteBId);
        }

        public static ClientCommandIntent PlaceTradePost(FixedVector2 position)
        {
            return new ClientCommandIntent(ClientIntentType.PlaceTradePost, position, new int[0], 0, 0, 0, 0, 0, 0, 0);
        }
    }
}
