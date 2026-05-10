namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotTechLabelResolver
    {
        public static string ResolveTechLabel(int techId)
        {
            switch (techId)
            {
                case 1:
                    return "InfAtk1";
                default:
                    return "#" + techId;
            }
        }

        public static string ResolveModifierLabel(int modifierId)
        {
            switch (modifierId)
            {
                case 1:
                    return "InfAtkBonus";
                default:
                    return "#" + modifierId;
            }
        }
    }
}
