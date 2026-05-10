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
                new GodotHotkeyHelpEntry("F1", "Start DryArabiaTest01 local 1v1"),
                new GodotHotkeyHelpEntry("F6", "Start local 6-player FFA"),
                new GodotHotkeyHelpEntry("F9", "Toggle primitive/sprite render mode"),
                new GodotHotkeyHelpEntry("F10", "Toggle debug overlay"),
                new GodotHotkeyHelpEntry("H/F11", "Toggle hotkey help"),
                new GodotHotkeyHelpEntry("Space", "Pause / unpause"),
                new GodotHotkeyHelpEntry("Arrows", "Pan camera"),
                new GodotHotkeyHelpEntry("Mouse edge", "Pan camera"),
                new GodotHotkeyHelpEntry("Middle mouse drag", "Pan camera"),
                new GodotHotkeyHelpEntry("C", "Enter TC placement mode (click to place, RMB/Esc cancel)"),
                new GodotHotkeyHelpEntry("W", "Place Wall at mouse"),
                new GodotHotkeyHelpEntry("T", "Place Trade Post at mouse"),
                new GodotHotkeyHelpEntry("R", "Create Trade Route with selected Trade Cart"),
                new GodotHotkeyHelpEntry("V", "Train Villager from selected building"),
                new GodotHotkeyHelpEntry("I", "Train Infantry from selected building"),
                new GodotHotkeyHelpEntry("K", "Train Trade Cart from selected Trade Post"),
                new GodotHotkeyHelpEntry("Y", researchIsWired ? "Research current available tech" : "Research reserved/not active"),
                new GodotHotkeyHelpEntry("Left Click", "Select unit / confirm placement in placement mode"),
                new GodotHotkeyHelpEntry("Right Click", "Context action: move, attack, gather, assign build; or cancel placement")
            };
        }
    }
}
