using System.Collections.Generic;

namespace RtsGame.Stress
{
    public sealed class StressScenarioResult
    {
        public string ScenarioName { get; }
        public int ScenarioVersion { get; }
        public int FinalTick { get; }
        public ulong FinalChecksum { get; }
        public int CommandCount { get; }
        public int DesyncCount { get; }
        public int InvariantFailureCount
        {
            get { return InvariantFailures.Count; }
        }

        public List<string> InvariantFailures { get; } = new List<string>();

        public bool Passed
        {
            get { return DesyncCount == 0 && InvariantFailures.Count == 0; }
        }

        public StressScenarioResult(string scenarioName, int scenarioVersion, int finalTick, ulong finalChecksum, int commandCount, int desyncCount)
        {
            ScenarioName = scenarioName;
            ScenarioVersion = scenarioVersion;
            FinalTick = finalTick;
            FinalChecksum = finalChecksum;
            CommandCount = commandCount;
            DesyncCount = desyncCount;
        }
    }
}
