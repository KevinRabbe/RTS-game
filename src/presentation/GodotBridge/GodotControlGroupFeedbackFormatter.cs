namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotControlGroupFeedbackFormatter
    {
        public static string BuildAssignedText(int groupIndex, int selectedUnitCount)
        {
            return "Group " + groupIndex + " assigned: " + selectedUnitCount + " units";
        }

        public static string BuildAddedText(int groupIndex, int totalUnitCount)
        {
            return "Group " + groupIndex + " added: " + totalUnitCount + " units";
        }

        public static string BuildRecalledText(int groupIndex, int recalledUnitCount)
        {
            if (recalledUnitCount <= 0)
            {
                return "Group " + groupIndex + " empty";
            }

            return "Group " + groupIndex + " recalled: " + recalledUnitCount + " units";
        }
    }
}
