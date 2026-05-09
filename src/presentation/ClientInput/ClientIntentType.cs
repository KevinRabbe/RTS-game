namespace RtsGame.Presentation.ClientInput
{
    public enum ClientIntentType
    {
        NoOp = 0,
        PlaceTownCenter = 1,
        AssignBuild = 2,
        GatherResource = 3,
        TrainUnit = 4,
        MoveUnits = 5,
        Attack = 6,
        Resign = 7,
        PlaceWall = 8,
        CreateTradeRoute = 9,
        PlaceTradePost = 10
    }
}
