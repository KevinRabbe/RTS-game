namespace RtsGame.Presentation.GodotBridge
{
    public enum GodotCommandResultKind
    {
        Accepted = 1,
        Rejected = 2,
        NoVisibleCountChange = 3
    }

    public static class GodotCommandResultClassifier
    {
        public static GodotCommandResultKind Classify(int beforeExecuted, int beforeRejected, int afterExecuted, int afterRejected)
        {
            if (afterRejected > beforeRejected)
            {
                return GodotCommandResultKind.Rejected;
            }

            if (afterExecuted > beforeExecuted)
            {
                return GodotCommandResultKind.Accepted;
            }

            return GodotCommandResultKind.NoVisibleCountChange;
        }
    }
}
