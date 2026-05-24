namespace RtsGame.Presentation.GodotBridge
{
    public readonly struct GodotHotkeyHelpEntry
    {
        public GodotHotkeyHelpEntry(string input, string action)
        {
            Input = input;
            Action = action;
        }

        public string Input { get; }
        public string Action { get; }
    }

    public static class GodotHotkeyHelpBuilder
    {
        public static GodotHotkeyHelpEntry[] Build(bool researchIsWired)
        {
            return new[]
            {
                new GodotHotkeyHelpEntry("Session:", ""),
                new GodotHotkeyHelpEntry("F1", "Start DryArabiaTest01 economy test"),
                new GodotHotkeyHelpEntry("F2", "Start CombatTest01 combat test"),
                new GodotHotkeyHelpEntry("F6", "Start local 6-player FFA"),
                new GodotHotkeyHelpEntry("", ""),

                new GodotHotkeyHelpEntry("Camera:", ""),
                new GodotHotkeyHelpEntry("Mouse Edge", "Pan camera"),
                new GodotHotkeyHelpEntry("Middle Mouse Drag", "Pan camera"),
                new GodotHotkeyHelpEntry("Arrow Keys", "Pan camera"),
                new GodotHotkeyHelpEntry("", ""),

                new GodotHotkeyHelpEntry("View:", ""),
                new GodotHotkeyHelpEntry("F9", "Toggle sprites/primitives"),
                new GodotHotkeyHelpEntry("F10", "Toggle debug overlay"),
                new GodotHotkeyHelpEntry("F12", "Toggle screenshot mode"),
                new GodotHotkeyHelpEntry("H/F11", "Toggle hotkey help"),
                new GodotHotkeyHelpEntry("Space", "Pause / unpause"),
                new GodotHotkeyHelpEntry("", ""),

                new GodotHotkeyHelpEntry("Control Groups:", ""),
                new GodotHotkeyHelpEntry("Ctrl+1..9", "Assign selected units to control group"),
                new GodotHotkeyHelpEntry("1..9", "Recall control group selection"),
                new GodotHotkeyHelpEntry("", ""),

                new GodotHotkeyHelpEntry("Building:", ""),
                new GodotHotkeyHelpEntry("C", "Enter Town Center placement mode"),
                new GodotHotkeyHelpEntry("A", "Enter Attack-Move targeting mode"),
                new GodotHotkeyHelpEntry("W", "Place Wall at mouse"),
                new GodotHotkeyHelpEntry("T", "Place Trade Post at mouse"),
                new GodotHotkeyHelpEntry("Right Click", "Confirm contextual action / cancel placement depending mode"),
                new GodotHotkeyHelpEntry("Escape", "Cancel active mode"),
                new GodotHotkeyHelpEntry("", ""),

                new GodotHotkeyHelpEntry("Production:", ""),
                new GodotHotkeyHelpEntry("V", "Train Villager"),
                new GodotHotkeyHelpEntry("I", "Train Infantry"),
                new GodotHotkeyHelpEntry("K", "Train Trade Cart"),
                new GodotHotkeyHelpEntry("R", "Create Trade Route with selected Trade Cart"),
                new GodotHotkeyHelpEntry("Y", researchIsWired ? "Research Infantry Attack I" : "Research reserved/not active"),
                new GodotHotkeyHelpEntry("", ""),

                new GodotHotkeyHelpEntry("Mouse:", ""),
                new GodotHotkeyHelpEntry("Left Click", "Select / confirm placement"),
                new GodotHotkeyHelpEntry("Right Click", "Move, gather, attack, assign build, or cancel placement")
            };
        }
    }
}
