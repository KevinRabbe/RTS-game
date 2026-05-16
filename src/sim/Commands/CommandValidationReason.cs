namespace RtsGame.Sim.Commands
{
    public enum CommandValidationReason : ushort
    {
        Accepted = 0,
        TemporaryCongestionAcceptedIntent = 1,
        InvalidHeader = 2,
        TargetMissing = 3,
        WrongOwner = 4,
        TargetComplete = 5,
        InvalidTargetType = 6,
        UnitCannotPerformAction = 7,
        NoStaticPath = 8,
        TargetBlockedByStaticGeometry = 9,
        MissingResources = 10,
        PopulationBlocked = 11,
        DuplicateUnitSelection = 12,
        PlayerStateBlocked = 13,
        Unknown = 14
    }
}

