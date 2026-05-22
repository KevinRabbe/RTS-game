using RtsGame.Net.Lockstep;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Sim.Systems;

namespace RtsGame.Tests
{
    public static partial class Program
    {
        private static void ThirtyWorkersAcrossResourcesKeepProgressOrIntent()
        {
            RunWorkerPressureScenario(3042, 30, 240, "thirty workers across resources", false);
        }

        private static void ThirtyWorkersAcrossResourcesKeepProgressOrIntentV2()
        {
            RunWorkerPressureScenario(3042, 30, 240, "thirty workers across resources", true);
        }

        private static void FiftyWorkersAcrossResourcesKeepProgressOrIntent()
        {
            RunWorkerPressureScenario(3043, 50, 280, "fifty workers across resources", false);
        }

        private static void FiftyWorkersAcrossResourcesKeepProgressOrIntentV2()
        {
            RunWorkerPressureScenario(3043, 50, 280, "fifty workers across resources", true);
        }

        private static void OneHundredTwentyWorkersAcrossResourcesKeepProgressOrIntent()
        {
            RunWorkerPressureScenario(3044, 120, 360, "one hundred twenty workers across resources", false);
        }

        private static void OneHundredTwentyWorkersAcrossResourcesKeepProgressOrIntentV2()
        {
            RunWorkerPressureScenario(3044, 120, 360, "one hundred twenty workers across resources", true);
        }

        private static void SixPlayerMixedPopulationTrafficRemainsDeterministic()
        {
            RunSixPlayerMixedPopulationTrafficRemainsDeterministic(false);
        }

        private static void SixPlayerMixedPopulationTrafficRemainsDeterministicV2()
        {
            RunSixPlayerMixedPopulationTrafficRemainsDeterministic(true);
        }

        private static void PathQueryBudgetStaysBoundedUnderPressure()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3046);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            int areaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 15));
            int nodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 15), 2500);
            int[] workers = CreateGridOfVillagers(state, 120, 18, 30, 12);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(nodeId, workers)));
            var runner = new TickRunner();
            int maxPathCallsPerTick = 0;
            int maxRetargetsPerTick = 0;
            for (int tick = 0; tick < 220; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int pathCalls = state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls;
                if (pathCalls > maxPathCallsPerTick)
                {
                    maxPathCallsPerTick = pathCalls;
                }

                if (state.DebugCounters.ReservationRetargetCount > maxRetargetsPerTick)
                {
                    maxRetargetsPerTick = state.DebugCounters.ReservationRetargetCount;
                }
            }

            AssertEqual(true, maxPathCallsPerTick <= GameData.PathQueryBudgetPerTick, "path query budget should stay bounded under 120-worker pressure max=" + maxPathCallsPerTick);
            AssertEqual(true, maxRetargetsPerTick <= GameData.ReservationRetargetBudgetPerTick, "reservation retarget churn should stay bounded under 120-worker pressure max=" + maxRetargetsPerTick);
        }

        private static void RepeatedCommandReplacementStaysBounded()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3047);
            int[] units = CreateLineOfUnits(state, 20, 0, 0, 10, 0, 1);
            var buffer = new CommandBuffer();
            for (int tick = 0; tick < 80; tick++)
            {
                int tx = (tick % 2 == 0) ? 40 : 10;
                int ty = (tick % 2 == 0) ? 40 : 10;
                buffer.Add(new CommandEnvelope(new CommandHeader(tick, 0, unchecked((uint)tick), CommandType.MoveUnits), new MoveUnitsCommand(units, FixedVector2.FromInts(tx, ty))));
            }

            var runner = new TickRunner();
            int maxPathCallsPerTick = 0;
            for (int tick = 0; tick < 120; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int pathCalls = state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls;
                if (pathCalls > maxPathCallsPerTick)
                {
                    maxPathCallsPerTick = pathCalls;
                }

                AssertNoLiveUnitStacking(state, "repeated replacement should not stack");
            }

            AssertEqual(true, maxPathCallsPerTick <= GameData.PathQueryBudgetPerTick, "repeated replacement path query budget should stay bounded max=" + maxPathCallsPerTick);
        }

        private static void TwoToFiveVillagersGatherDepositCrossingRoutesStayStable()
        {
            RunTwoToFiveVillagersGatherDepositCrossingRoutesStayStable(false);
        }

        private static void TwoToFiveVillagersGatherDepositCrossingRoutesStayStableV2()
        {
            RunTwoToFiveVillagersGatherDepositCrossingRoutesStayStable(true);
        }

        private static void LeftGoldBlockerVillagerRecoversWithoutEndlessMoveToResource()
        {
            RunLeftGoldBlockerVillagerRecoversWithoutEndlessMoveToResource(false);
        }

        private static void LeftGoldBlockerVillagerRecoversWithoutEndlessMoveToResourceV2()
        {
            RunLeftGoldBlockerVillagerRecoversWithoutEndlessMoveToResource(true);
        }

        private static void RepeatedMoveReplacementNearTcHotspotStaysStable()
        {
            RunRepeatedMoveReplacementNearTcHotspotStaysStable(false);
        }

        private static void RepeatedMoveReplacementNearTcHotspotStaysStableV2()
        {
            RunRepeatedMoveReplacementNearTcHotspotStaysStable(true);
        }

        private static void SixPlayerSevenTwentyVillagerEquivalentPressureStaysBounded()
        {
            RunSixPlayerSevenTwentyVillagerEquivalentPressureStaysBounded(false);
        }

        private static void SixPlayerSevenTwentyVillagerEquivalentPressureStaysBoundedV2()
        {
            RunSixPlayerSevenTwentyVillagerEquivalentPressureStaysBounded(true);
        }

        private static void SixPlayerTwelveHundredActiveUnitPressureStaysBounded()
        {
            RunSixPlayerTwelveHundredActiveUnitPressureStaysBounded(false);
        }

        private static void SixPlayerTwelveHundredActiveUnitPressureStaysBoundedV2()
        {
            RunSixPlayerTwelveHundredActiveUnitPressureStaysBounded(true);
        }

        private static void PressureWindowBudgetsStayBounded()
        {
            RunPressureWindowBudgetsStayBounded(false);
        }

        private static void PressureWindowBudgetsStayBoundedV2()
        {
            RunPressureWindowBudgetsStayBounded(true);
        }

    }
}
