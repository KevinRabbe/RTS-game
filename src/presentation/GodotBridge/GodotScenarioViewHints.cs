namespace RtsGame.Presentation.GodotBridge
{
    public static class GodotScenarioViewHints
    {
        public static bool TryGetInitialCameraTile(string mapName, out int tileX, out int tileY)
        {
            if (mapName == CombatTest01MapName)
            {
                tileX = 64;
                tileY = 48;
                return true;
            }

            tileX = 0;
            tileY = 0;
            return false;
        }

        private const string CombatTest01MapName = "CombatTest01";
    }
}
