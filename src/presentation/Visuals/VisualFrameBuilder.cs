using System.Collections.Generic;
using RtsGame.Presentation.Snapshots;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Presentation.Visuals
{
    public static class VisualFrameBuilder
    {
        private static readonly Fixed UnitSize = Fixed.FromRatio(7, 10);
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

            for (int i = 0; i < snapshot.Resources.Count; i++)
            {
                AddResource(snapshot.Resources[i], primitives);
            }

            AddTradeRoutes(snapshot, primitives);
            return new VisualFrame(snapshot.Tick, primitives);
        }

        private static void AddFogOverlay(List<VisualPrimitive> primitives)
        {
            primitives.Add(new VisualPrimitive(
                VisualPrimitiveKind.FogOverlay,
                0,
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
                (int)unit.UnitTypeId,
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
            Fixed size = GetBuildingFootprintSize(building.BuildingTypeId);
            int maxHitPoints = GameData.GetBuildingCompletedHitPoints(building.BuildingTypeId, building.IsCapital);
            primitives.Add(new VisualPrimitive(
                kind,
                building.Id,
                (int)building.BuildingTypeId,
                building.OwnerPlayerIndex,
                building.Position,
                building.Position,
                size,
                building.HitPoints,
                maxHitPoints,
                building.IsCapital));
            AddHealthBar(building.Id, building.OwnerPlayerIndex, building.Position, building.HitPoints, maxHitPoints, primitives);
        }

        private static void AddResource(ResourceNodeSnapshot resource, List<VisualPrimitive> primitives)
        {
            Fixed size = GetResourceVisualSize(resource.GatherProfileId);
            primitives.Add(new VisualPrimitive(
                GetResourceKind(resource.ResourceType),
                resource.Id,
                (int)resource.ResourceType,
                GameData.NeutralOwnerPlayerIndex,
                resource.Position,
                resource.Position,
                size,
                resource.RemainingAmount,
                resource.RemainingAmount,
                false));
        }

        private static Fixed GetBuildingFootprintSize(BuildingTypeId buildingTypeId)
        {
            return Fixed.FromInt(GameData.GetBuildingPlacementRadiusTiles(buildingTypeId) * 2);
        }

        private static Fixed GetResourceVisualSize(GatherProfileId gatherProfileId)
        {
            GatherProfile profile = GameData.GetGatherProfile(gatherProfileId);
            return Fixed.FromInt(profile.VisualRadiusTiles * 2);
        }

        private static VisualPrimitiveKind GetResourceKind(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Food:
                    return VisualPrimitiveKind.FoodResourceCircle;
                case ResourceType.Wood:
                    return VisualPrimitiveKind.WoodResourceCircle;
                case ResourceType.Gold:
                    return VisualPrimitiveKind.GoldResourceCircle;
                default:
                    return VisualPrimitiveKind.FoodResourceCircle;
            }
        }

        private static void AddHealthBar(int entityId, int ownerPlayerIndex, FixedVector2 position, int hitPoints, int maxHitPoints, List<VisualPrimitive> primitives)
        {
            primitives.Add(new VisualPrimitive(
                VisualPrimitiveKind.HealthBar,
                entityId,
                0,
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
                    (int)unit.UnitTypeId,
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
