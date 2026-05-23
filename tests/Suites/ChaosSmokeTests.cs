using RtsGame.Net.Lockstep;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Sim.Systems;
using RtsGame.Stress;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void ChaosV1StressSmoke()
        {
            StressScenarioResult result = new StressScenarioRunner().RunChaosV1(1200, 77);
            AssertEqual(true, result.Passed, "chaos v1 stress should pass invariants");
            AssertEqual(1200, result.FinalTick, "chaos v1 stress should reach requested tick");
        }

        private static void ChaosV2StressSmoke()
        {
            StressScenarioResult result = new StressScenarioRunner().RunChaosV2(1200, 78);
            string invariantDetails = result.InvariantFailures.Count == 0 ? "none" : string.Join(" | ", result.InvariantFailures);
            AssertEqual(true, result.Passed, "chaos v2 stress should pass invariants details=" + invariantDetails);
            AssertEqual(1200, result.FinalTick, "chaos v2 stress should reach requested tick");
            AssertEqual(1, result.ScenarioVersion, "chaos v2 version should be frozen at v1");
        }

        private static void ChaosV3StressSmoke()
        {
            StressScenarioResult result = new StressScenarioRunner().RunChaosV3(1200, 79);
            AssertEqual(true, result.Passed, "chaos v3 stress should pass invariants");
            AssertEqual(1200, result.FinalTick, "chaos v3 stress should reach requested tick");
            AssertEqual(1, result.ScenarioVersion, "chaos v3 version should be frozen at v1");
        }

        private static void ChaosV4StressSmoke()
        {
            StressScenarioResult result = new StressScenarioRunner().RunChaosV4(1200, 80);
            AssertEqual(true, result.Passed, "chaos v4 stress should pass invariants");
            AssertEqual(1200, result.FinalTick, "chaos v4 stress should reach requested tick");
            AssertEqual(1, result.ScenarioVersion, "chaos v4 version should be frozen at v1");
        }

        private static void ChaosV5StressSmoke()
        {
            StressScenarioResult result = new StressScenarioRunner().RunChaosV5(1200, 81);
            AssertEqual(true, result.Passed, "chaos v5 stress should pass invariants");
            AssertEqual(1200, result.FinalTick, "chaos v5 stress should reach requested tick");
            AssertEqual(1, result.ScenarioVersion, "chaos v5 version should be frozen at v1");
        }

    }
}
