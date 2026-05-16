using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public readonly struct CommandValidationReport
    {
        public bool Accepted { get; }
        public CommandValidationReason Reason { get; }
        public int TargetEntityId { get; }
        public int TargetTileX { get; }
        public int TargetTileY { get; }
        public int UnitCount { get; }
        public int FirstUnitId { get; }

        public CommandValidationReport(
            bool accepted,
            CommandValidationReason reason,
            int targetEntityId,
            int targetTileX,
            int targetTileY,
            int unitCount,
            int firstUnitId)
        {
            Accepted = accepted;
            Reason = reason;
            TargetEntityId = targetEntityId;
            TargetTileX = targetTileX;
            TargetTileY = targetTileY;
            UnitCount = unitCount;
            FirstUnitId = firstUnitId;
        }
    }

    public static class CommandValidationInspector
    {
        public static CommandValidationReport Evaluate(GameState state, GameRules rules, CommandEnvelope command)
        {
            if (command.Payload is AssignBuildCommand assignBuild)
            {
                return CreateUnitCommandReport(
                    assignBuild.GetValidationReason(state, rules, command.Header),
                    assignBuild.TargetBuildingId,
                    0,
                    0,
                    assignBuild.UnitIds);
            }

            if (command.Payload is GatherResourceCommand gather)
            {
                return CreateUnitCommandReport(
                    gather.GetValidationReason(state, rules, command.Header),
                    gather.ResourceNodeId,
                    0,
                    0,
                    gather.UnitIds);
            }

            if (command.Payload is MoveUnitsCommand move)
            {
                return CreateUnitCommandReport(
                    move.GetValidationReason(state, rules, command.Header),
                    0,
                    SpatialRules.GetTileX(move.Target),
                    SpatialRules.GetTileY(move.Target),
                    move.UnitIds);
            }

            if (command.Payload is TrainUnitCommand train)
            {
                CommandValidationReason reason = train.GetValidationReason(state, rules, command.Header);
                return new CommandValidationReport(
                    IsAccepted(reason),
                    reason,
                    train.BuildingId,
                    0,
                    0,
                    1,
                    0);
            }

            if (command.Payload is AttackCommand attack)
            {
                return CreateUnitCommandReport(
                    attack.GetValidationReason(state, rules, command.Header),
                    attack.TargetEntityId,
                    0,
                    0,
                    attack.AttackerUnitIds);
            }

            bool accepted = command.Payload.IsValid(state, rules, command.Header);
            return new CommandValidationReport(
                accepted,
                accepted ? CommandValidationReason.Accepted : CommandValidationReason.Unknown,
                0,
                0,
                0,
                0,
                0);
        }

        public static bool IsAccepted(CommandValidationReason reason)
        {
            return reason == CommandValidationReason.Accepted
                || reason == CommandValidationReason.TemporaryCongestionAcceptedIntent;
        }

        private static CommandValidationReport CreateUnitCommandReport(
            CommandValidationReason reason,
            int targetEntityId,
            int targetTileX,
            int targetTileY,
            System.Collections.Generic.IReadOnlyList<int> unitIds)
        {
            return new CommandValidationReport(
                IsAccepted(reason),
                reason,
                targetEntityId,
                targetTileX,
                targetTileY,
                unitIds.Count,
                unitIds.Count > 0 ? unitIds[0] : 0);
        }
    }
}
