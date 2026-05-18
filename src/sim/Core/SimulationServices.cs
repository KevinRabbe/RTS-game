using System.Collections.Generic;
using RtsGame.Sim.Data;

namespace RtsGame.Sim.Core
{
    /// <summary>
    /// Deterministic reason code used by the traffic authority when releasing reservations.
    /// This enum is part of the stable simulation compatibility boundary.
    /// </summary>
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
        /// <summary>
        /// Geometry truth only: returns static blocker state from simulation footprints, bounds, and terrain.
        /// Must not inspect presentation data.
        /// </summary>
        bool IsStaticBlocked(GameState state, int tileX, int tileY, int ignoredBuildingId = 0);
        /// <summary>
        /// Dynamic occupancy query for live units.
        /// </summary>
        bool IsOccupied(GameState state, int tileX, int tileY, int ignoredUnitId = 0);
        /// <summary>
        /// Reservation occupancy query for final-purpose slots.
        /// </summary>
        bool IsReserved(GameState state, int tileX, int tileY, int ignoredUnitId = 0);
        /// <summary>
        /// Generates interaction ring candidates around a simulation footprint.
        /// </summary>
        void EnumerateInteractionRing(GameState state, List<SpatialRules.TileCoord> footprintTiles, int ignoredBuildingId, List<SpatialRules.TileCoord> output);
    }

    /// <summary>
    /// Optional extension point for future path quality upgrades (v2+).
    /// Keep null/no-op for baseline behavior.
    /// </summary>
    public interface IPathCandidateShortlistStrategy
    {
        void BuildCandidateShortlist(
            GameState state,
            int unitId,
            int fromX,
            int fromY,
            int toX,
            int toY,
            int contextVersion,
            List<SpatialRules.TileCoord> output);
    }

    public interface IPathQueryService
    {
        /// <summary>
        /// Stable compatibility property. Null means baseline deterministic pathfinder behavior.
        /// Implementations must remain deterministic.
        /// </summary>
        IPathCandidateShortlistStrategy? CandidateShortlistStrategy { get; set; }
        bool TryNextStep(GameState state, int unitId, int fromX, int fromY, int toX, int toY, int contextVersion, out int nextX, out int nextY);
        bool TryPathCost(GameState state, int fromX, int fromY, int toX, int toY, int contextVersion, out int cost);
    }

    /// <summary>
    /// Optional extension point for future traffic lane/side preference scoring (v2+).
    /// Keep null/no-op for baseline behavior.
    /// </summary>
    public interface ITrafficLanePreferenceScorer
    {
        int Score(
            GameState state,
            Unit unit,
            InteractionReservationKind kind,
            int targetId,
            SpatialRules.TileCoord candidate,
            int contextVersion);
    }

    public interface ITrafficReservationService
    {
        /// <summary>
        /// Stable compatibility property. Null means baseline deterministic tie-break ordering.
        /// </summary>
        ITrafficLanePreferenceScorer? LanePreferenceScorer { get; set; }
        void Reserve(GameState state, Unit unit, InteractionReservationKind kind, int targetId, SpatialRules.TileCoord tile);
        void Release(GameState state, Unit unit, ReservationReleaseReason reason);
        bool Revalidate(GameState state, Unit unit, InteractionReservationKind kind, int targetId, int tileX, int tileY);
        bool TryReserve(GameState state, Unit unit, InteractionReservationKind kind, int targetId, IReadOnlyList<SpatialRules.TileCoord> candidates, int contextVersion, out SpatialRules.TileCoord selected);
    }

    /// <summary>
    /// Optional extension point for future steering/hysteresis policy (v4+).
    /// Keep null/no-op for baseline behavior.
    /// </summary>
    public interface IMovementSteeringPolicy
    {
        bool TryAdjustStep(
            GameState state,
            Unit unit,
            int currentTileX,
            int currentTileY,
            int targetTileX,
            int targetTileY,
            ref int nextTileX,
            ref int nextTileY);
    }

    public interface IMovementProgressPolicy
    {
        /// <summary>
        /// Stable compatibility property. Null means baseline movement progress logic.
        /// </summary>
        IMovementSteeringPolicy? SteeringPolicy { get; set; }
        bool ShouldRecordProgressTick(Unit unit, bool entersNewTile, bool reachesTarget);
        bool IsNoProgressTimedOut(GameState state, Unit unit);
    }
}
