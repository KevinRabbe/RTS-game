using System.Collections.Generic;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    public enum ReservationReleaseReason : ushort
    {
        None = 0,
        CommandReplaced = 1,
        TargetInvalid = 2,
        UnitDeath = 3,
        UnitResign = 4,
        Timeout = 5,
        Arrival = 6
    }

    public interface ISpatialIndexService
    {
        bool IsStaticBlocked(GameState state, int tileX, int tileY, int ignoredBuildingId = 0);
        bool IsOccupied(GameState state, int tileX, int tileY, int ignoredUnitId = 0);
        bool IsReserved(GameState state, int tileX, int tileY, int ignoredUnitId = 0);
        void EnumerateInteractionRing(GameState state, List<SpatialRules.TileCoord> footprintTiles, int ignoredBuildingId, List<SpatialRules.TileCoord> output);
    }

    public interface IPathQueryService
    {
        bool TryNextStep(GameState state, int unitId, int fromX, int fromY, int toX, int toY, int contextVersion, out int nextX, out int nextY);
        bool TryPathCost(GameState state, int fromX, int fromY, int toX, int toY, int contextVersion, out int cost);
    }

    public interface ITrafficReservationService
    {
        void Reserve(GameState state, Unit unit, InteractionReservationKind kind, int targetId, SpatialRules.TileCoord tile);
        void Release(GameState state, Unit unit, ReservationReleaseReason reason);
        bool Revalidate(GameState state, Unit unit, InteractionReservationKind kind, int targetId, int tileX, int tileY);
        bool TryReserve(GameState state, Unit unit, InteractionReservationKind kind, int targetId, IReadOnlyList<SpatialRules.TileCoord> candidates, int contextVersion, out SpatialRules.TileCoord selected);
    }

    public interface IMovementProgressPolicy
    {
        bool ShouldRecordProgressTick(Unit unit, bool entersNewTile, bool reachesTarget);
        bool IsNoProgressTimedOut(GameState state, Unit unit);
    }
}
