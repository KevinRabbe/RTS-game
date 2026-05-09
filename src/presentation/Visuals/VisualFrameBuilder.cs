using System.Collections.Generic;
using RtsGame.Presentation.Snapshots;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.Visuals
{
    public static class VisualFrameBuilder
    {
        private static readonly Fixed UnitSize = Fixed.FromRatio(7, 10);
        private static readonly Fixed BuildingSize = Fixed.FromInt(2);
        private static readonly Fixed CapitalSize = Fixed.FromInt(3);
        private static readonly Fixed WallSize = Fixed.FromInt(1);
        private static readonly Fixed HealthBarSize = Fixed.FromRatio(9, 10);

        public static VisualFrame Build(GameSnapshot snapshot)
        {
            var primitives = new List<VisualPrimitive>();
            AddFogOverlay(primitives);
            for (int i = 0; i < snapshot.Buildings.Count; i++)
            {
                AddBuilding(snapshot.Buildings[i], primitives);
            }

            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                AddUnit(snapshot.Units[i], primitives);
            }

            AddTradeRoutes(snapshot, primitives);
            return new VisualFrame(snapshot.Tick, primitives);
        }

        private static void AddFogOverlay(List<VisualPrimitive> primitives)
        {
            primitives.Add(new VisualPrimitive(
                VisualPrimitiveKind.FogOverlay,
                0,
                GameData.NeutralOwnerPlayerIndex,
                FixedVector2.FromInts(0, 0),
                FixedVector2.FromInts(0, 0),
                Fixed.FromInt(0),
                0,
                0,
                false));
        }

        private static void AddUnit(UnitSnapshot unit, List<VisualPrimitive> primitives)
        {
            primitives.Add(new VisualPrimitive(
                VisualPrimitiveKind.UnitSquare,
                unit.Id,
                unit.OwnerPlayerIndex,
                unit.Position,
                unit.Position,
                UnitSize,
                unit.HitPoints,
                GameData.GetUnitHitPoints(unit.UnitTypeId),
                false));
            AddHealthBar(unit.Id, unit.OwnerPlayerIndex, unit.Position, unit.HitPoints, GameData.GetUnitHitPoints(unit.UnitTypeId), primitives);
        }

        private static void AddBuilding(BuildingSnapshot building, List<VisualPrimitive> primitives)
        {
            VisualPrimitiveKind kind = building.BuildingTypeId == BuildingTypeId.Wall ? VisualPrimitiveKind.WallRectangle : VisualPrimitiveKind.BuildingRectangle;
            Fixed size = building.BuildingTypeId == BuildingTypeId.Wall ? WallSize : (building.IsCapital ? CapitalSize : BuildingSize);
            int maxHitPoints = GameData.GetBuildingCompletedHitPoints(building.BuildingTypeId, building.IsCapital);
            primitives.Add(new VisualPrimitive(
                kind,
                building.Id,
                building.OwnerPlayerIndex,
                building.Position,
                building.Position,
                size,
                building.HitPoints,
                maxHitPoints,
                building.IsCapital));
            AddHealthBar(building.Id, building.OwnerPlayerIndex, building.Position, building.HitPoints, maxHitPoints, primitives);
        }

        private static void AddHealthBar(int entityId, int ownerPlayerIndex, FixedVector2 position, int hitPoints, int maxHitPoints, List<VisualPrimitive> primitives)
        {
            primitives.Add(new VisualPrimitive(
                VisualPrimitiveKind.HealthBar,
                entityId,
                ownerPlayerIndex,
                position,
                position,
                HealthBarSize,
                hitPoints,
                maxHitPoints,
                false));
        }

        private static void AddTradeRoutes(GameSnapshot snapshot, List<VisualPrimitive> primitives)
        {
            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                UnitSnapshot unit = snapshot.Units[i];
                if (unit.TradeRouteAId == 0 || unit.TradeRouteBId == 0)
                {
                    continue;
                }

                if (!TryFindBuilding(snapshot, unit.TradeRouteAId, out BuildingSnapshot first) || !TryFindBuilding(snapshot, unit.TradeRouteBId, out BuildingSnapshot second))
                {
                    continue;
                }

                primitives.Add(new VisualPrimitive(
                    VisualPrimitiveKind.TradeRouteLine,
                    unit.Id,
                    unit.OwnerPlayerIndex,
                    first.Position,
                    second.Position,
                    Fixed.FromInt(0),
                    0,
                    0,
                    false));
            }
        }

        private static bool TryFindBuilding(GameSnapshot snapshot, int buildingId, out BuildingSnapshot building)
        {
            for (int i = 0; i < snapshot.Buildings.Count; i++)
            {
                if (snapshot.Buildings[i].Id == buildingId)
                {
                    building = snapshot.Buildings[i];
                    return true;
                }
            }

            building = default(BuildingSnapshot);
            return false;
        }
    }
}
