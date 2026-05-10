using System;
using RtsGame.Sim.Commands;

namespace RtsGame.Presentation.ClientInput
{
    public static class ClientCommandMapper
    {
        public static CommandEnvelope ToCommandEnvelope(ClientCommandIntent intent, int tick, int playerIndex, uint sequence)
        {
            switch (intent.IntentType)
            {
                case ClientIntentType.NoOp:
                    return Envelope(tick, playerIndex, sequence, CommandType.NoOp, new NoOpCommand());
                case ClientIntentType.PlaceTownCenter:
                    return Envelope(tick, playerIndex, sequence, CommandType.PlaceTownCenter, new PlaceTownCenterCommand(intent.Position));
                case ClientIntentType.AssignBuild:
                    return Envelope(tick, playerIndex, sequence, CommandType.AssignBuild, new AssignBuildCommand(intent.BuildingId, intent.UnitIds));
                case ClientIntentType.GatherResource:
                    return Envelope(tick, playerIndex, sequence, CommandType.GatherResource, new GatherResourceCommand(intent.ResourceNodeId, intent.UnitIds));
                case ClientIntentType.TrainUnit:
                    return Envelope(tick, playerIndex, sequence, CommandType.TrainUnit, new TrainUnitCommand(intent.BuildingId, intent.UnitTypeId));
                case ClientIntentType.ResearchTech:
                    return Envelope(tick, playerIndex, sequence, CommandType.ResearchTech, new ResearchTechCommand(intent.BuildingId, intent.TechId));
                case ClientIntentType.MoveUnits:
                    return Envelope(tick, playerIndex, sequence, CommandType.MoveUnits, new MoveUnitsCommand(intent.UnitIds, intent.Position));
                case ClientIntentType.Attack:
                    return Envelope(tick, playerIndex, sequence, CommandType.Attack, new AttackCommand(intent.UnitIds, intent.TargetEntityId));
                case ClientIntentType.Resign:
                    return Envelope(tick, playerIndex, sequence, CommandType.Resign, new ResignCommand());
                case ClientIntentType.PlaceWall:
                    return Envelope(tick, playerIndex, sequence, CommandType.PlaceWall, new PlaceWallCommand(intent.Position));
                case ClientIntentType.CreateTradeRoute:
                    return Envelope(tick, playerIndex, sequence, CommandType.CreateTradeRoute, new CreateTradeRouteCommand(intent.TradeCartId, intent.TradeRouteAId, intent.TradeRouteBId));
                case ClientIntentType.PlaceTradePost:
                    return Envelope(tick, playerIndex, sequence, CommandType.PlaceTradePost, new PlaceTradePostCommand(intent.Position));
                default:
                    throw new ArgumentOutOfRangeException(nameof(intent), "Unknown client command intent.");
            }
        }

        private static CommandEnvelope Envelope(int tick, int playerIndex, uint sequence, CommandType commandType, ICommand command)
        {
            return new CommandEnvelope(new CommandHeader(tick, playerIndex, sequence, commandType), command);
        }
    }
}
