namespace RtsGame.Sim.Commands
{
    public enum CommandType : ushort
    {
        NoOp = 0,
        DebugIncrementCounter = 1,
        PlaceTownCenter = 2,
        AssignBuild = 3,
        GatherResource = 4,
        TrainUnit = 5,
        MoveUnits = 6,
        Attack = 7,
        Resign = 8,
        PlaceWall = 9,
        CreateTradeRoute = 10,
        PlaceTradePost = 11,
        ResearchTech = 12
    }
}
