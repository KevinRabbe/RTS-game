using RtsGame.Net.Lockstep;
using RtsGame.Presentation.Snapshots;
using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;
using RtsGame.Stress;

namespace RtsGame.Tests
{
    public static class Program
    {
        public static int Main()
        {
            var tests = new List<TestCase>
            {
                new TestCase("empty tick determinism", EmptyTickDeterminism),
                new TestCase("command ordering", CommandOrdering),
                new TestCase("replay determinism", ReplayDeterminism),
                new TestCase("lockstep empty stream", LockstepEmptyStream),
                new TestCase("lockstep arrival reordering", LockstepArrivalReordering),
                new TestCase("missing input stalls", MissingInputStalls),
                new TestCase("canonical serialization", CanonicalSerialization),
                new TestCase("fixed point determinism", FixedPointDeterminism),
                new TestCase("cleanup updates lookup", CleanupUpdatesLookup),
                new TestCase("nomad start creates initial units", NomadStartCreatesInitialUnits),
                new TestCase("nomad map creates center resources", NomadMapCreatesCenterResources),
                new TestCase("placement rejects overlapping building", PlacementRejectsOverlappingBuilding),
                new TestCase("placement rejects resource overlap", PlacementRejectsResourceOverlap),
                new TestCase("placement rejects outside map", PlacementRejectsOutsideMap),
                new TestCase("placement rejection replay determinism", PlacementRejectionReplayDeterminism),
                new TestCase("placement rejection lockstep", PlacementRejectionLockstep),
                new TestCase("first town center is free", FirstTownCenterIsFree),
                new TestCase("second town center pays wood", SecondTownCenterPaysWood),
                new TestCase("second town center rejects missing wood", SecondTownCenterRejectsMissingWood),
                new TestCase("town center expansion cost stays meaningful", TownCenterExpansionCostStaysMeaningful),
                new TestCase("first town center becomes capital", FirstTownCenterBecomesCapital),
                new TestCase("second town center stays normal", SecondTownCenterStaysNormal),
                new TestCase("completed normal town center stays weaker than capital", CompletedNormalTownCenterStaysWeakerThanCapital),
                new TestCase("capital loss removes bonus", CapitalLossRemovesBonus),
                new TestCase("town center after capital loss stays normal", TownCenterAfterCapitalLossStaysNormal),
                new TestCase("normal town center does not inherit capital bonus", NormalTownCenterDoesNotInheritCapitalBonus),
                new TestCase("capital placement replay determinism", CapitalPlacementReplayDeterminism),
                new TestCase("capital placement lockstep", CapitalPlacementLockstep),
                new TestCase("gather waits for completed town center", GatherWaitsForCompletedTownCenter),
                new TestCase("villagers gather and deposit food", VillagersGatherAndDepositFood),
                new TestCase("gather rejects carried different resource", GatherRejectsCarriedDifferentResource),
                new TestCase("depleted resource clears gather assignment", DepletedResourceClearsGatherAssignment),
                new TestCase("economy replay determinism", EconomyReplayDeterminism),
                new TestCase("economy lockstep", EconomyLockstep),
                new TestCase("train villager pays cost and completes", TrainVillagerPaysCostAndCompletes),
                new TestCase("train villager rejects missing resources", TrainVillagerRejectsMissingResources),
                new TestCase("train villager respects population cap", TrainVillagerRespectsPopulationCap),
                new TestCase("training replay determinism", TrainingReplayDeterminism),
                new TestCase("training lockstep", TrainingLockstep),
                new TestCase("move unit advances deterministically", MoveUnitAdvancesDeterministically),
                new TestCase("move unit snaps to target", MoveUnitSnapsToTarget),
                new TestCase("move command clears work assignments", MoveCommandClearsWorkAssignments),
                new TestCase("move rejects wall-blocked target", MoveRejectsWallBlockedTarget),
                new TestCase("movement pathfinds around wall", MovementPathfindsAroundWall),
                new TestCase("pathfinder returns same first step", PathfinderReturnsSameFirstStep),
                new TestCase("pathfinder wall blocks path", PathfinderWallBlocksPath),
                new TestCase("destroyed wall opens path next tick", DestroyedWallOpensPathNextTick),
                new TestCase("no path returns failure deterministically", NoPathReturnsFailureDeterministically),
                new TestCase("unit blocked by stationary unit", UnitBlockedByStationaryUnit),
                new TestCase("two units attempting same tile fail", TwoUnitsAttemptingSameTileFail),
                new TestCase("three units attempting same tile fail", ThreeUnitsAttemptingSameTileFail),
                new TestCase("two unit tile swap fails", TwoUnitTileSwapFails),
                new TestCase("unit death same tick still blocks movement", UnitDeathSameTickStillBlocksMovement),
                new TestCase("wall destruction same tick still blocks movement", WallDestructionSameTickStillBlocksMovement),
                new TestCase("wall blocking replay determinism", WallBlockingReplayDeterminism),
                new TestCase("wall blocking lockstep", WallBlockingLockstep),
                new TestCase("movement replay determinism", MovementReplayDeterminism),
                new TestCase("movement lockstep", MovementLockstep),
                new TestCase("visibility reveals initial scout area", VisibilityRevealsInitialScoutArea),
                new TestCase("visibility updates after movement", VisibilityUpdatesAfterMovement),
                new TestCase("explored visibility persists", ExploredVisibilityPersists),
                new TestCase("visibility replay determinism", VisibilityReplayDeterminism),
                new TestCase("visibility lockstep", VisibilityLockstep),
                new TestCase("presentation snapshot includes visible local state", PresentationSnapshotIncludesVisibleLocalState),
                new TestCase("presentation snapshot hides invisible enemies", PresentationSnapshotHidesInvisibleEnemies),
                new TestCase("presentation snapshot does not mutate checksum", PresentationSnapshotDoesNotMutateChecksum),
                new TestCase("simulation does not reference presentation", SimulationDoesNotReferencePresentation),
                new TestCase("visual frame creates ugly prototype primitives", VisualFrameCreatesUglyPrototypePrimitives),
                new TestCase("visual frame marks capital larger than normal building", VisualFrameMarksCapitalLargerThanNormalBuilding),
                new TestCase("visual frame includes trade route line", VisualFrameIncludesTradeRouteLine),
                new TestCase("visual frame does not mutate checksum", VisualFrameDoesNotMutateChecksum),
                new TestCase("train infantry completes", TrainInfantryCompletes),
                new TestCase("train cavalry completes", TrainCavalryCompletes),
                new TestCase("cavalry moves faster than infantry", CavalryMovesFasterThanInfantry),
                new TestCase("cavalry damages enemy unit", CavalryDamagesEnemyUnit),
                new TestCase("cavalry replay determinism", CavalryReplayDeterminism),
                new TestCase("cavalry lockstep", CavalryLockstep),
                new TestCase("attack damages enemy unit", AttackDamagesEnemyUnit),
                new TestCase("attack respects cooldown", AttackRespectsCooldown),
                new TestCase("attack rejects friendly target", AttackRejectsFriendlyTarget),
                new TestCase("move command clears attack target", MoveCommandClearsAttackTarget),
                new TestCase("move command preserves attack cooldown", MoveCommandPreservesAttackCooldown),
                new TestCase("disengage requires explicit reattack", DisengageRequiresExplicitReattack),
                new TestCase("dead unit cleanup after combat", DeadUnitCleanupAfterCombat),
                new TestCase("combat replay determinism", CombatReplayDeterminism),
                new TestCase("combat lockstep", CombatLockstep),
                new TestCase("attack damages building", AttackDamagesBuilding),
                new TestCase("capital destruction removes bonus", CapitalDestructionRemovesBonus),
                new TestCase("capital destruction does not defeat player with another town center", CapitalDestructionDoesNotDefeatPlayerWithAnotherTownCenter),
                new TestCase("capital destruction replay determinism", CapitalDestructionReplayDeterminism),
                new TestCase("capital destruction lockstep", CapitalDestructionLockstep),
                new TestCase("resign neutralizes assets and assigns placement", ResignNeutralizesAssetsAndAssignsPlacement),
                new TestCase("resigned assets despawn after timer", ResignedAssetsDespawnAfterTimer),
                new TestCase("resigned player non-noop commands reject", ResignedPlayerNonNoOpCommandsReject),
                new TestCase("resignation replay determinism", ResignationReplayDeterminism),
                new TestCase("resignation lockstep", ResignationLockstep),
                new TestCase("player eliminated with no town center and no villagers", PlayerEliminatedWithNoTownCenterAndNoVillagers),
                new TestCase("player survives with villager after town center loss", PlayerSurvivesWithVillagerAfterTownCenterLoss),
                new TestCase("elimination replay determinism", EliminationReplayDeterminism),
                new TestCase("elimination lockstep", EliminationLockstep),
                new TestCase("match ends when one player remains", MatchEndsWhenOnePlayerRemains),
                new TestCase("match end replay determinism", MatchEndReplayDeterminism),
                new TestCase("match end lockstep", MatchEndLockstep),
                new TestCase("wall rejects missing wood", WallRejectsMissingWood),
                new TestCase("train siege cannon completes", TrainSiegeCannonCompletes),
                new TestCase("siege setup delays first shot", SiegeSetupDelaysFirstShot),
                new TestCase("siege reload delays second shot", SiegeReloadDelaysSecondShot),
                new TestCase("moving siege cancels deployment", MovingSiegeCancelsDeployment),
                new TestCase("siege rejects unit target", SiegeRejectsUnitTarget),
                new TestCase("siege destroys capital", SiegeDestroysCapital),
                new TestCase("siege replay determinism", SiegeReplayDeterminism),
                new TestCase("siege lockstep", SiegeLockstep),
                new TestCase("train mangonel completes", TrainMangonelCompletes),
                new TestCase("mangonel area hits multiple enemies", MangonelAreaHitsMultipleEnemies),
                new TestCase("mangonel area ignores friendly units", MangonelAreaIgnoresFriendlyUnits),
                new TestCase("mangonel simultaneous deaths cleanup", MangonelSimultaneousDeathsCleanup),
                new TestCase("mangonel area does not damage capital", MangonelAreaDoesNotDamageCapital),
                new TestCase("mangonel replay determinism", MangonelReplayDeterminism),
                new TestCase("mangonel lockstep", MangonelLockstep),
                new TestCase("place wall creates vulnerable construction", PlaceWallCreatesVulnerableConstruction),
                new TestCase("assigned villagers complete wall", AssignedVillagersCompleteWall),
                new TestCase("under construction wall can be destroyed", UnderConstructionWallCanBeDestroyed),
                new TestCase("wall replay determinism", WallReplayDeterminism),
                new TestCase("wall lockstep", WallLockstep),
                new TestCase("trade post rejects before completed town center", TradePostRejectsBeforeCompletedTownCenter),
                new TestCase("trade post rejects missing resources", TradePostRejectsMissingResources),
                new TestCase("place trade post creates construction", PlaceTradePostCreatesConstruction),
                new TestCase("assigned villagers complete trade post", AssignedVillagersCompleteTradePost),
                new TestCase("trade post replay determinism", TradePostReplayDeterminism),
                new TestCase("trade post lockstep", TradePostLockstep),
                new TestCase("train trade cart completes", TrainTradeCartCompletes),
                new TestCase("trade route pays gold on arrival", TradeRoutePaysGoldOnArrival),
                new TestCase("longer trade route pays more", LongerTradeRoutePaysMore),
                new TestCase("destroyed trade endpoint clears route", DestroyedTradeEndpointClearsRoute),
                new TestCase("trade cart can be killed", TradeCartCanBeKilled),
                new TestCase("trade replay determinism", TradeReplayDeterminism),
                new TestCase("trade lockstep", TradeLockstep),
                new TestCase("chaos v1 stress smoke", ChaosV1StressSmoke),
                new TestCase("chaos v2 stress smoke", ChaosV2StressSmoke),
                new TestCase("chaos v3 stress smoke", ChaosV3StressSmoke),
                new TestCase("chaos v4 stress smoke", ChaosV4StressSmoke)
            };

            int failed = 0;
            foreach (TestCase test in tests)
            {
                try
                {
                    test.Run();
                    Console.WriteLine("PASS " + test.Name);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("FAIL " + test.Name);
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("tests=" + tests.Count + " failed=" + failed);
            return failed == 0 ? 0 : 1;
        }

        private static void EmptyTickDeterminism()
        {
            ulong first = RunNoOpSimulation(1000, 2, 123);
            ulong second = RunNoOpSimulation(1000, 2, 123);
            AssertEqual(first, second, "same empty command stream must produce same checksum");
        }

        private static void CommandOrdering()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            ulong canonical = RunDebugCommands(rules, new[]
            {
                DebugCommand(0, 0, 0, 1),
                DebugCommand(0, 1, 0, 10),
                DebugCommand(0, 0, 1, 100)
            });

            ulong shuffled = RunDebugCommands(rules, new[]
            {
                DebugCommand(0, 0, 1, 100),
                DebugCommand(0, 1, 0, 10),
                DebugCommand(0, 0, 0, 1)
            });

            AssertEqual(canonical, shuffled, "shuffled insertion must still execute deterministically");
        }

        private static void ReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var recorder = new ReplayRecorder(rules, 77, 2);
            for (int tick = 0; tick < 100; tick++)
            {
                recorder.RecordCommand(new CommandEnvelope(new CommandHeader(tick, 0, 0, CommandType.NoOp), new NoOpCommand()));
                recorder.RecordCommand(new CommandEnvelope(new CommandHeader(tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            }

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 100);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 100);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "replay checksum must be stable");
        }

        private static void LockstepEmptyStream()
        {
            LockstepSession session = RunLockstep(500, 2, 3, false);
            AssertEqual(500, session.CurrentTick, "lockstep should reach requested tick");
            AssertEqual(0, session.DesyncReports.Count, "lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "peer checksums should match");
        }

        private static void LockstepArrivalReordering()
        {
            LockstepSession session = RunLockstep(250, 2, 3, true);
            AssertEqual(250, session.CurrentTick, "lockstep should reach requested tick with reordered delivery");
            AssertEqual(0, session.DesyncReports.Count, "reordered command arrival should not desync");
        }

        private static void MissingInputStalls()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 9);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            bool advanced = session.TryAdvanceOneTick();
            AssertFalse(advanced, "missing player input must stall");
            AssertEqual(0, session.CurrentTick, "current tick must not advance when input is missing");
        }

        private static void CanonicalSerialization()
        {
            var writerA = new CanonicalWriter();
            writerA.WriteInt32(-1);
            writerA.WriteUInt64(42);
            writerA.WriteBool(true);

            var writerB = new CanonicalWriter();
            writerB.WriteInt32(-1);
            writerB.WriteUInt64(42);
            writerB.WriteBool(true);

            byte[] a = writerA.ToArray();
            byte[] b = writerB.ToArray();
            AssertEqual(a.Length, b.Length, "canonical byte lengths must match");
            for (int i = 0; i < a.Length; i++)
            {
                AssertEqual(a[i], b[i], "canonical bytes must match");
            }
        }

        private static void FixedPointDeterminism()
        {
            Fixed a = Fixed.FromRatio(1, 3);
            Fixed b = Fixed.FromRatio(2, 3);
            Fixed c = a + b;
            AssertEqual(Fixed.FromInt(1).Raw - 1, c.Raw, "fixed ratios should truncate deterministically");
        }

        private static void CleanupUpdatesLookup()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = new GameState(1, 1);
            state.EntityState.Units.Add(new Unit { Id = 1, OwnerPlayerIndex = 0, HitPoints = 0, IsDead = true });
            state.EntityState.Units.Add(new Unit { Id = 2, OwnerPlayerIndex = 0, HitPoints = 1, IsDead = false });
            state.EntityState.EntityLookup[1] = new EntityRef(EntityKind.Unit, 0);
            state.EntityState.EntityLookup[2] = new EntityRef(EntityKind.Unit, 1);

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            AssertEqual(1, state.EntityState.Units.Count, "dead unit should be removed");
            AssertFalse(state.EntityState.EntityLookup.ContainsKey(1), "removed unit lookup should be gone");
            AssertEqual(0, state.EntityState.EntityLookup[2].Index, "moved unit lookup should update");
        }

        private static void NomadStartCreatesInitialUnits()
        {
            GameState state = GameInitializer.CreateNomadStart(5, 3);
            AssertEqual(15, state.EntityState.Units.Count, "nomad start should create five units per player");
            AssertEqual(0, state.EntityState.Buildings.Count, "nomad start should not create town centers");

            for (int player = 0; player < 3; player++)
            {
                AssertEqual(5, state.PlayerStates.Players[player].PopulationUsed, "each player should start with five population used");
                AssertEqual(0, state.PlayerStates.Players[player].PopulationCap, "capital bonus should not exist before TC placement");
                AssertFalse(state.PlayerStates.Players[player].CapitalStatus.HasCapitalBeenPlaced, "capital should not exist before TC placement");
            }
        }

        private static void NomadMapCreatesCenterResources()
        {
            GameState state = GameInitializer.CreateNomadStart(6, 3);
            AssertEqual(12, state.EconomyState.ResourceNodes.Count, "nomad map should create player resources plus center resources");

            ResourceNode centerGold = state.EconomyState.ResourceNodes[9];
            AssertEqual(ResourceType.Gold, centerGold.ResourceType, "first center resource should be gold");
            AssertEqual(FixedVector2.FromInts(GameData.MapWidthTiles / 2, GameData.MapHeightTiles / 2), centerGold.Position, "center gold should be placed at map center");
            AssertEqual(GameData.CenterGoldAmount, centerGold.RemainingAmount, "center gold should be high value");
        }

        private static void PlacementRejectsOverlappingBuilding()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            runner.AdvanceOneTick(state, rules, buffer);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(10, 10))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "overlapping wall should be rejected");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "overlapping building placement should count as rejected");
        }

        private static void PlacementRejectsResourceOverlap()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(8, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(6, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "town center should not place over a resource node");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "resource overlap placement should count as rejected");
        }

        private static void PlacementRejectsOutsideMap()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(9, 1);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(-1, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "building should not place outside map");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "outside map placement should count as rejected");
        }

        private static void PlacementRejectionReplayDeterminism()
        {
            var replay = new ReplayFile(1, GameRules.CreatePhaseZeroDefaults(1), 10, 1, ReplayInitialState.Nomad);
            replay.Commands.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(6, 0))));
            ReplayResult first = new ReplayRunner().Run(replay, 1);
            ReplayResult second = new ReplayRunner().Run(replay, 1);

            AssertEqual(first.FinalChecksum, second.FinalChecksum, "rejected placement replay checksums should match");
        }

        private static void PlacementRejectionLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 11, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(6, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "rejected placement tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "rejected placement lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "rejected placement peer checksums should match");
            AssertEqual(1, session.Peers[0].LocalState.DebugCounters.RejectedCommandCount, "rejected placement should be counted in lockstep");
        }

        private static void FirstTownCenterIsFree()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(12, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "first town center should place without resources");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "first town center should not spend wood");
        }

        private static void SecondTownCenterPaysWood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(13, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(2, state.EntityState.Buildings.Count, "second town center should place when affordable");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "second town center should spend wood");
        }

        private static void SecondTownCenterRejectsMissingWood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(14, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "second town center should reject without wood");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "missing town center wood should count as rejected");
        }

        private static void TownCenterExpansionCostStaysMeaningful()
        {
            AssertEqual(true, GameData.TownCenterWoodCost > GameData.TradePostWoodCost, "normal town center should cost more wood than a trade post");
            AssertEqual(true, GameData.TownCenterWoodCost > GameData.WallWoodCost * 20, "normal town center should be meaningfully more expensive than walling");
        }

        private static void FirstTownCenterBecomesCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(1, 2);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));

            var runner = new TickRunner();
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "one TC should be placed");
            AssertEqual(true, state.EntityState.Buildings[0].IsCapital, "first TC should be capital");
            AssertEqual(true, state.EntityState.Buildings[0].IsUnderConstruction, "placed TC should start under construction");
            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus, state.EntityState.Buildings[0].HitPoints, "capital should get hit point bonus");
            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.HasCapitalBeenPlaced, "capital status should be set");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should wait for completion");
            AssertEqual(0, state.PlayerStates.Players[0].PopulationCap, "capital should not grant population while under construction");

            int buildingId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(buildingId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Buildings[0].IsUnderConstruction, "assigned villagers should complete construction");
            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should be active after completion");
            AssertEqual(GameData.CapitalPopulationBonus, state.PlayerStates.Players[0].PopulationCap, "capital should grant population bonus");
        }

        private static void SecondTownCenterStaysNormal()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));

            var runner = new TickRunner();
            runner.AdvanceOneTick(state, rules, buffer);
            int firstBuildingId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(firstBuildingId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            runner.AdvanceOneTick(state, rules, buffer);

            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;
            buffer.Add(new CommandEnvelope(new CommandHeader(3, 0, 2, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(2, state.EntityState.Buildings.Count, "two TCs should exist");
            AssertEqual(true, state.EntityState.Buildings[0].IsCapital, "first TC should remain capital");
            AssertEqual(false, state.EntityState.Buildings[1].IsCapital, "second TC should be normal");
            AssertEqual(GameData.TownCenterHitPoints, state.EntityState.Buildings[1].HitPoints, "normal TC should not get capital hit points");
            AssertEqual(GameData.CapitalPopulationBonus, state.PlayerStates.Players[0].PopulationCap, "capital bonus should apply only once");
        }

        private static void CompletedNormalTownCenterStaysWeakerThanCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(15, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);
            int normalTownCenterId = state.EntityState.Buildings[1].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 4, CommandType.AssignBuild), new AssignBuildCommand(normalTownCenterId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, state.Tick, 0, 5);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Buildings[1].IsUnderConstruction, "normal town center should complete");
            AssertEqual(GameData.TownCenterHitPoints, state.EntityState.Buildings[1].HitPoints, "completed normal town center should use normal hit points");
            AssertEqual(true, state.EntityState.Buildings[0].HitPoints > state.EntityState.Buildings[1].HitPoints, "capital should remain stronger than normal town center");
        }

        private static void CapitalLossRemovesBonus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));

            runner.AdvanceOneTick(state, rules, buffer);
            int buildingId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(buildingId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            runner.AdvanceOneTick(state, rules, buffer);
            state.EntityState.Buildings[0].IsDead = true;
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "dead capital should be removed by cleanup");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.IsCapitalAlive, "capital should no longer be alive");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should be inactive after loss");
            AssertEqual(0, state.PlayerStates.Players[0].PopulationCap, "capital population bonus should be removed");
        }

        private static void TownCenterAfterCapitalLossStaysNormal()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(16, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.EntityState.Buildings[0].IsDead = true;
            runner.AdvanceOneTick(state, rules, buffer);
            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "replacement town center should place after capital loss");
            AssertEqual(false, state.EntityState.Buildings[0].IsCapital, "capital cannot be rebuilt");
            AssertEqual(GameData.TownCenterHitPoints, state.EntityState.Buildings[0].HitPoints, "post-loss town center should use normal hit points");
            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.HasCapitalBeenPlaced, "capital placement history should remain permanent");
            AssertEqual(false, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "capital bonus should not reactivate");
        }

        private static void NormalTownCenterDoesNotInheritCapitalBonus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateBuildingCombatState();
            state.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;
            EntityFactory.CreateTownCenter(state, 1, FixedVector2.FromInts(3, 0));
            state.EntityState.Buildings[1].IsUnderConstruction = false;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "normal town center should remain after capital loss");
            AssertEqual(false, state.EntityState.Buildings[0].IsCapital, "remaining town center should stay normal");
            AssertEqual(0, state.PlayerStates.Players[1].PopulationCap, "capital population bonus should not transfer to normal town center");
            AssertEqual(false, state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "capital bonus should remain inactive");
        }

        private static void CapitalPlacementReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var recorder = new ReplayRecorder(rules, 99, 2, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(50, 10))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.AssignBuild), new AssignBuildCommand(12, new[] { 6, 7, 8, 9 })));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 3);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 3);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "capital placement replay should be deterministic");
        }

        private static void CapitalPlacementLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 123, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(50, 10))));

            bool advanced = session.TryAdvanceOneTick();
            AssertEqual(true, advanced, "capital placement tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2, 3, 4 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.AssignBuild), new AssignBuildCommand(12, new[] { 6, 7, 8, 9 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "capital build assignment tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 1, 2, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "capital completion tick should advance");
            AssertEqual(0, session.DesyncReports.Count, "capital placement should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "peer checksums should match after placement");
            AssertEqual(true, session.Peers[0].LocalState.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "player 0 capital should be active");
            AssertEqual(true, session.Peers[0].LocalState.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "player 1 capital should be active");
        }

        private static void GatherWaitsForCompletedTownCenter()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            runner.AdvanceOneTick(state, rules, buffer);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(10, state.EntityState.Units[0].CarriedAmount, "villager should gather to carry capacity");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Food, "food should not deposit before TC completion");
        }

        private static void VillagersGatherAndDepositFood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, state.Tick, 0, 3);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(20, state.PlayerStates.Players[0].Resources.Food, "two villagers should deposit one full carry each");
            AssertEqual(480, state.EconomyState.ResourceNodes[0].RemainingAmount, "food node should lose gathered amount");
            AssertEqual(0, state.EntityState.Units[0].CarriedAmount, "villager should empty carried food after deposit");
            AssertEqual(ResourceType.None, state.EntityState.Units[0].CarriedResourceType, "villager carried type should reset after deposit");
        }

        private static void GatherRejectsCarriedDifferentResource()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2, 1);
            state.EntityState.Units[0].CarriedResourceType = ResourceType.Food;
            state.EntityState.Units[0].CarriedAmount = 5;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(3, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "villager should not accept incompatible gather order");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "incompatible gather order should count as rejected");
        }

        private static void DepletedResourceClearsGatherAssignment()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(3, 1);
            state.EconomyState.ResourceNodes[0].RemainingAmount = 5;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EconomyState.ResourceNodes[0].RemainingAmount, "resource node should deplete");
            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "depleted node should clear gather assignment");
        }

        private static void EconomyReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var recorder = new ReplayRecorder(rules, 44, 1, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(4, 0, 4, CommandType.NoOp), new NoOpCommand()));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 5);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 5);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "economy replay should be deterministic");
        }

        private static void EconomyLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var session = new LockstepSession(rules, 22, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            AssertEqual(true, session.TryAdvanceOneTick(), "place TC tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2, 3, 4 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "assign build tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "complete TC tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "gather assignment tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(4, 0, 4, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "deposit tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "economy lockstep should not desync");
            AssertEqual(20, session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food, "food should deposit in lockstep state");
        }

        private static void TrainVillagerPaysCostAndCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = 50;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnitCount = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.PlayerStates.Players[0].Resources.Food, "training should pay food cost immediately");
            AssertEqual(6, state.PlayerStates.Players[0].PopulationUsed, "training should reserve population immediately");
            AssertEqual(1, state.EntityState.Buildings[0].TrainingQueue.Count, "villager should be queued");

            AddNoOp(buffer, state.Tick, 0, 4);
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, state.Tick, 0, 5);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(initialUnitCount + 1, state.EntityState.Units.Count, "villager should spawn when training completes");
            AssertEqual(0, state.EntityState.Buildings[0].TrainingQueue.Count, "training queue should be empty after completion");
            AssertEqual(UnitTypeId.Villager, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be villager");
        }

        private static void TrainVillagerRejectsMissingResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            int buildingId = state.EntityState.Buildings[0].Id;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings[0].TrainingQueue.Count, "training should reject without food");
            AssertEqual(5, state.PlayerStates.Players[0].PopulationUsed, "rejected training should not reserve population");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "rejected command should be counted");
        }

        private static void TrainVillagerRespectsPopulationCap()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(7, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = 500;
            state.PlayerStates.Players[0].PopulationCap = state.PlayerStates.Players[0].PopulationUsed;
            int buildingId = state.EntityState.Buildings[0].Id;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings[0].TrainingQueue.Count, "training should reject when population is capped");
            AssertEqual(500, state.PlayerStates.Players[0].Resources.Food, "rejected population cap training should not spend food");
        }

        private static void TrainingReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var recorder = new ReplayRecorder(rules, 88, 1, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(4, 0, 4, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(5, 0, 5, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(6, 0, 6, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(7, 0, 7, CommandType.TrainUnit), new TrainUnitCommand(6, UnitTypeId.Villager)));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(8, 0, 8, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(9, 0, 9, CommandType.NoOp), new NoOpCommand()));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 10);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 10);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "training replay should be deterministic");
        }

        private static void TrainingLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var session = new LockstepSession(rules, 66, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            AssertEqual(true, session.TryAdvanceOneTick(), "place TC tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2, 3, 4 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "assign build tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "complete TC tick should advance");
            session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food = 50;
            session.Broadcast(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(6, UnitTypeId.Villager)));
            AssertEqual(true, session.TryAdvanceOneTick(), "train command tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(4, 0, 4, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "training progress tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(5, 0, 5, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "training completion tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "training lockstep should not desync");
            AssertEqual(6, session.Peers[0].LocalState.EntityState.Units.Count, "trained villager should exist");
        }

        private static void MoveUnitAdvancesDeterministically()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));

            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromRatio(1, 2).Raw, state.EntityState.Units[0].Position.X.Raw, "villager should move half a tile on first tick");
            AssertEqual(0L, state.EntityState.Units[0].Position.Y.Raw, "villager should stay on same Y axis");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "unit should continue moving toward target");
        }

        private static void MoveUnitSnapsToTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = CreateOccupancyState(1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));

            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 1, 0, 1);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromInt(1).Raw, state.EntityState.Units[0].Position.X.Raw, "unit should snap to target");
            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "unit should clear move target after arrival");
        }

        private static void MoveCommandClearsWorkAssignments()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AssertEqual(1, state.EntityState.Units[0].CurrentResourceNodeId, "unit should have gather assignment");

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(3, 0))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "move should clear gather assignment");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "move target should be set");
        }

        private static void MoveRejectsWallBlockedTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(15, 1);
            EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(2, 0));
            state.EntityState.Buildings[0].IsUnderConstruction = false;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "move target inside wall should reject");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "blocked move target should count as rejected");
        }

        private static void MovementPathfindsAroundWall()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = CreateOccupancyState(16);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(2, 0));
            state.EntityState.Buildings[0].IsUnderConstruction = false;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));
            for (int tick = 0; tick < 8; tick++)
            {
                if (tick > 0)
                {
                    AddNoOp(buffer, tick, 0, (uint)tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(true, state.EntityState.Units[0].Position.X.Raw > Fixed.FromInt(1).Raw, "unit should progress past the wall using a side route");
            AssertEqual(false, SpatialRules.IsBlockedByWall(state, state.EntityState.Units[0].Position), "unit should not occupy a wall-blocked position");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "unit should continue pathing toward target");
        }

        private static void PathfinderReturnsSameFirstStep()
        {
            GameState first = CreatePathfindingWallState();
            GameState second = CreatePathfindingWallState();

            bool firstFound = DeterministicPathfinder.TryFindNextTile(first, 0, 0, 4, 0, out int firstX, out int firstY);
            bool secondFound = DeterministicPathfinder.TryFindNextTile(second, 0, 0, 4, 0, out int secondX, out int secondY);

            AssertEqual(true, firstFound, "pathfinder should find route around single wall");
            AssertEqual(firstX, secondX, "same path request should return same X step");
            AssertEqual(firstY, secondY, "same path request should return same Y step");
            AssertEqual(1, firstX, "first step should follow frozen east-first neighbor order");
            AssertEqual(0, firstY, "first step should stay on row before rerouting");
        }

        private static void PathfinderWallBlocksPath()
        {
            GameState state = CreateOccupancyState(17);
            AddCompletedWall(state, 0, FixedVector2.FromInts(2, 0));

            bool found = DeterministicPathfinder.TryFindNextTile(state, 0, 0, 2, 0, out int nextX, out int nextY);

            AssertEqual(false, found, "sealed wall barrier should block path");
            AssertEqual(0, nextX, "failed path should keep default X");
            AssertEqual(0, nextY, "failed path should keep default Y");
        }

        private static void DestroyedWallOpensPathNextTick()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(18);
            int wallId = AddCompletedWall(state, 0, FixedVector2.FromInts(1, 0));
            bool blocked = DeterministicPathfinder.TryFindNextTile(state, 0, 0, 1, 0, out _, out _);
            state.EntityState.Buildings[state.EntityState.EntityLookup[wallId].Index].IsDead = true;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            bool opened = DeterministicPathfinder.TryFindNextTile(state, 0, 0, 1, 0, out int nextX, out int nextY);

            AssertEqual(false, blocked, "wall tile should initially block direct path");
            AssertEqual(true, opened, "destroyed wall should open path after cleanup");
            AssertEqual(1, nextX, "opened path should step through former wall tile");
            AssertEqual(0, nextY, "opened path should stay on row");
        }

        private static void NoPathReturnsFailureDeterministically()
        {
            GameState first = CreateOccupancyState(19);
            GameState second = CreateOccupancyState(19);
            AddVerticalBarrier(first, 1, 0, GameData.MapHeightTiles);
            AddVerticalBarrier(second, 1, 0, GameData.MapHeightTiles);

            bool firstFound = DeterministicPathfinder.TryFindNextTile(first, 0, 0, 2, 0, out int firstX, out int firstY);
            bool secondFound = DeterministicPathfinder.TryFindNextTile(second, 0, 0, 2, 0, out int secondX, out int secondY);

            AssertEqual(false, firstFound, "no-path request should fail");
            AssertEqual(firstFound, secondFound, "no-path result should be deterministic");
            AssertEqual(firstX, secondX, "no-path X should be deterministic");
            AssertEqual(firstY, secondY, "no-path Y should be deterministic");
        }

        private static void UnitBlockedByStationaryUnit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromInt(0).Raw, state.EntityState.Units[0].Position.X.Raw, "stationary unit should hold occupied tile");
            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "blocked unit should clear move target");
        }

        private static void TwoUnitsAttemptingSameTileFail()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(2, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 2 }, FixedVector2.FromInts(1, 1))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromInt(0).Raw, state.EntityState.Units[0].Position.X.Raw, "first contender should not enter shared target tile");
            AssertEqual(Fixed.FromInt(2).Raw, state.EntityState.Units[1].Position.X.Raw, "second contender should not enter shared target tile");
        }

        private static void ThreeUnitsAttemptingSameTileFail()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(2, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 2));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 2, 3 }, FixedVector2.FromInts(1, 1))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromInt(0).Raw, state.EntityState.Units[0].Position.X.Raw, "first contender should fail shared target");
            AssertEqual(Fixed.FromInt(2).Raw, state.EntityState.Units[1].Position.X.Raw, "second contender should fail shared target");
            AssertEqual(Fixed.FromInt(2).Raw, state.EntityState.Units[2].Position.Y.Raw, "third contender should fail shared target");
        }

        private static void TwoUnitTileSwapFails()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(4);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            state.EntityState.Units[0].LastMovedTick = -1;
            state.EntityState.Units[1].LastMovedTick = -1;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 2 }, FixedVector2.FromInts(0, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromInt(0).Raw, state.EntityState.Units[0].Position.X.Raw, "first unit should not swap tiles");
            AssertEqual(Fixed.FromInt(1).Raw, state.EntityState.Units[1].Position.X.Raw, "second unit should not swap tiles");
        }

        private static void UnitDeathSameTickStillBlocksMovement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(5, 2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(1, 1));
            state.EntityState.Units[1].HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.Attack), new AttackCommand(new[] { 3 }, 2)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromInt(0).Raw, state.EntityState.Units[0].Position.X.Raw, "unit dying later in same tick should still block movement");
        }

        private static void WallDestructionSameTickStillBlocksMovement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(6, 2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            int wallId = EntityFactory.CreateWall(state, 1, FixedVector2.FromInts(2, 0));
            Building wall = state.EntityState.Buildings[state.EntityState.EntityLookup[wallId].Index];
            wall.IsUnderConstruction = false;
            wall.HitPoints = GameData.SiegeCannonBuildingDamage;
            int siegeId = EntityFactory.CreateUnit(state, 0, UnitTypeId.SiegeCannon, FixedVector2.FromInts(0, 2));
            Unit siege = state.EntityState.Units[state.EntityState.EntityLookup[siegeId].Index];
            siege.IsSiegeDeployed = true;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.Attack), new AttackCommand(new[] { 3 }, wallId)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromInt(1).Raw, state.EntityState.Units[0].Position.X.Raw, "wall destroyed later in same tick should still block movement step");
        }

        private static void WallBlockingReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState firstState = CreateWallBlockingState(17);
            GameState secondState = CreateWallBlockingState(17);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand())
            };

            ulong first = RunCommandsFromState(firstState, rules, commands, 3);
            ulong second = RunCommandsFromState(secondState, rules, commands, 3);
            AssertEqual(first, second, "wall blocking replay should be deterministic");
        }

        private static void WallBlockingLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 18, true);
            AddCompletedWall(session.Peers[0].LocalState, 0, FixedVector2.FromInts(2, 0));
            AddCompletedWall(session.Peers[1].LocalState, 0, FixedVector2.FromInts(2, 0));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "wall blocking move tick should advance");
            for (int tick = 1; tick <= 2; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "wall blocking continuation tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "wall blocking lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "wall blocking peer checksums should match");
        }

        private static void MovementReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var recorder = new ReplayRecorder(rules, 91, 1, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 5 }, FixedVector2.FromInts(5, 0))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.NoOp), new NoOpCommand()));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 4);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 4);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "movement replay should be deterministic");
        }

        private static void MovementLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 42, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(5, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 6 }, FixedVector2.FromInts(45, 0))));
            AssertEqual(true, session.TryAdvanceOneTick(), "move tick should advance");

            for (int tick = 1; tick < 5; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "movement continuation tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "movement lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "movement peer checksums should match");
        }

        private static void VisibilityRevealsInitialScoutArea()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, IsVisible(state, 0, 0, 2), "scout tile should be visible");
            AssertEqual(true, IsVisible(state, 0, 8, 2), "scout radius should reveal eight tiles horizontally");
            AssertEqual(false, IsVisible(state, 0, 20, 20), "far tile should not be visible");
        }

        private static void VisibilityUpdatesAfterMovement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(20, 2))));

            for (int tick = 0; tick < 22; tick++)
            {
                if (tick > 0)
                {
                    AddNoOp(buffer, tick, 0, (uint)tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(true, IsVisible(state, 0, 28, 2), "moved scout should reveal around new location");
            AssertEqual(false, IsVisible(state, 0, 8, 2), "old scout-only tile should no longer be currently visible");
        }

        private static void ExploredVisibilityPersists()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            runner.AdvanceOneTick(state, rules, buffer);
            AssertEqual(true, IsExplored(state, 0, 8, 2), "initial scout-only tile should be explored");

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(20, 2))));
            for (int tick = 1; tick <= 24; tick++)
            {
                if (tick > 1)
                {
                    AddNoOp(buffer, tick, 0, (uint)tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(false, IsVisible(state, 0, 8, 2), "old scout-only tile should leave current visibility");
            AssertEqual(true, IsExplored(state, 0, 8, 2), "old scout-only tile should remain explored");
        }

        private static void VisibilityReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var recorder = new ReplayRecorder(rules, 101, 1, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(10, 2))));
            for (int tick = 1; tick < 8; tick++)
            {
                recorder.RecordCommand(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
            }

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 8);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 8);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "visibility replay should be deterministic");
        }

        private static void VisibilityLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 55, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 5 }, FixedVector2.FromInts(10, 2))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 10 }, FixedVector2.FromInts(50, 2))));
            AssertEqual(true, session.TryAdvanceOneTick(), "visibility movement tick should advance");

            for (int tick = 1; tick < 8; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "visibility continuation tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "visibility lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "visibility peer checksums should match");
        }

        private static void PresentationSnapshotIncludesVisibleLocalState()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(63, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            state.PlayerStates.Players[0].Resources.Food = 100;
            state.PlayerStates.Players[0].Resources.Wood = 200;
            state.PlayerStates.Players[0].Resources.Gold = 300;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            AssertEqual(state.Tick, snapshot.Tick, "snapshot should copy tick");
            AssertEqual(0, snapshot.LocalPlayerIndex, "snapshot should copy local player index");
            AssertEqual(true, snapshot.Units.Count > 0, "snapshot should include visible local units");
            AssertEqual(true, snapshot.Buildings.Count > 0, "snapshot should include visible local buildings");
            AssertEqual(100, snapshot.LocalPlayer.Food, "snapshot should copy local food");
            AssertEqual(200, snapshot.LocalPlayer.Wood, "snapshot should copy local wood");
            AssertEqual(300, snapshot.LocalPlayer.Gold, "snapshot should copy local gold");
            AssertEqual(GameData.CapitalPopulationBonus, snapshot.LocalPlayer.PopulationCap, "snapshot should copy local population cap");
            AssertEqual(true, snapshot.LocalPlayer.CapitalBonusActive, "snapshot should copy capital status");
        }

        private static void PresentationSnapshotHidesInvisibleEnemies()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(64, 2);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                AssertFalse(snapshot.Units[i].OwnerPlayerIndex == 1, "snapshot should not include invisible enemy units");
            }
        }

        private static void PresentationSnapshotDoesNotMutateChecksum()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(65, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());
            ulong before = StateChecksum.Compute(state, rules);

            GameSnapshotBuilder.Build(state, 0);
            ulong after = StateChecksum.Compute(state, rules);

            AssertEqual(before, after, "building a presentation snapshot must not mutate simulation state");
        }

        private static void SimulationDoesNotReferencePresentation()
        {
            string simProject = System.IO.File.ReadAllText(System.IO.Path.Combine("src", "sim", "RtsGame.Sim.csproj"));

            AssertFalse(simProject.Contains("presentation") || simProject.Contains("Presentation"), "simulation project must not reference presentation layer");
        }

        private static void VisualFrameCreatesUglyPrototypePrimitives()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(66, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));

            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.FogOverlay), "visual frame should include fog overlay primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.UnitSquare), "visual frame should include unit square primitives");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.BuildingRectangle), "visual frame should include building rectangle primitives");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.HealthBar), "visual frame should include health bar primitives");
        }

        private static void VisualFrameMarksCapitalLargerThanNormalBuilding()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(67, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            int normalId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(4, 0));
            Building normal = state.EntityState.Buildings[state.EntityState.EntityLookup[normalId].Index];
            normal.IsUnderConstruction = false;
            normal.BuildProgressTicks = GameData.TownCenterBuildTicks;
            normal.HitPoints = GameData.TownCenterHitPoints;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));
            VisualPrimitive capital = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, state.PlayerStates.Players[0].CapitalStatus.CapitalBuildingId);
            VisualPrimitive normalTownCenter = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, normalId);

            AssertEqual(true, capital.IsCapital, "capital primitive should be marked as capital");
            AssertEqual(true, capital.Size.Raw > normalTownCenter.Size.Raw, "capital should render larger than normal town center");
        }

        private static void VisualFrameIncludesTradeRouteLine()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTradeState(6);
            Unit cart = state.EntityState.Units[state.EntityState.Units.Count - 1];
            cart.TradeRouteAId = state.EntityState.Buildings[0].Id;
            cart.TradeRouteBId = state.EntityState.Buildings[1].Id;
            cart.TradeDestinationId = state.EntityState.Buildings[1].Id;
            cart.TradeIncomePerTrip = 1;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));

            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.TradeRouteLine), "visual frame should include visible trade route line");
        }

        private static void VisualFrameDoesNotMutateChecksum()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(68, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());
            ulong before = StateChecksum.Compute(state, rules);

            VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));
            ulong after = StateChecksum.Compute(state, rules);

            AssertEqual(before, after, "building visual frame must not mutate simulation state");
        }

        private static void TrainInfantryCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = GameData.InfantryFoodCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Infantry)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.InfantryTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "infantry should complete training");
            AssertEqual(UnitTypeId.Infantry, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be infantry");
        }

        private static void TrainCavalryCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Food = GameData.CavalryFoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.CavalryGoldCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Cavalry)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.CavalryTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "cavalry should complete training");
            AssertEqual(UnitTypeId.Cavalry, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be cavalry");
            AssertEqual(7, state.PlayerStates.Players[0].PopulationUsed, "cavalry should reserve two population");
        }

        private static void CavalryMovesFasterThanInfantry()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(61);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Cavalry, FixedVector2.FromInts(0, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(5, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 2 }, FixedVector2.FromInts(5, 1))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(Fixed.FromRatio(2, 5).Raw, state.EntityState.Units[0].Position.X.Raw, "infantry should move at infantry speed");
            AssertEqual(Fixed.FromRatio(4, 5).Raw, state.EntityState.Units[1].Position.X.Raw, "cavalry should move at cavalry speed");
        }

        private static void CavalryDamagesEnemyUnit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateCavalryCombatState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.InfantryHitPoints - GameData.CavalryAttackDamage, state.EntityState.Units[11].HitPoints, "cavalry should damage enemy infantry");
            AssertEqual(GameData.CavalryAttackCooldownTicks, state.EntityState.Units[10].AttackCooldownTicksRemaining, "cavalry cooldown should be set");
        }

        private static void CavalryReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12))
            };

            ulong first = RunCommandsFromState(CreateCavalryCombatState(), rules, commands, 1);
            ulong second = RunCommandsFromState(CreateCavalryCombatState(), rules, commands, 1);
            AssertEqual(first, second, "cavalry combat should replay deterministically");
        }

        private static void CavalryLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 62, true);
            SetupCavalryCombatState(session.Peers[0].LocalState);
            SetupCavalryCombatState(session.Peers[1].LocalState);

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "cavalry combat tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "cavalry lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "cavalry peer checksums should match");
        }

        private static void AttackDamagesEnemyUnit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = CreateAdjacentCombatState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit target = state.EntityState.Units[11];
            AssertEqual(GameData.InfantryHitPoints - GameData.InfantryAttackDamage, target.HitPoints, "attack should damage enemy infantry");
            AssertEqual(GameData.InfantryAttackCooldownTicks, state.EntityState.Units[10].AttackCooldownTicksRemaining, "attacker cooldown should be set");
        }

        private static void AttackRespectsCooldown()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = CreateAdjacentCombatState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            var runner = new TickRunner();
            runner.AdvanceOneTick(state, rules, buffer);
            int hitPointsAfterFirstAttack = state.EntityState.Units[11].HitPoints;

            AddNoOp(buffer, 1, 0, 1);
            AddNoOp(buffer, 1, 1, 1);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(hitPointsAfterFirstAttack, state.EntityState.Units[11].HitPoints, "cooldown should prevent immediate second damage");
        }

        private static void AttackRejectsFriendlyTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 6 }, 7)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "friendly attack command should reject");
            AssertEqual(GameData.InfantryHitPoints, state.EntityState.Units[6].HitPoints, "friendly target should not be damaged");
        }

        private static void MoveCommandClearsAttackTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            state.EntityState.Units[10].AttackTargetId = 12;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 11 }, FixedVector2.FromInts(0, 2))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Units[10].AttackTargetId, "move should clear attack intent");
            AssertEqual(true, state.EntityState.Units[10].HasMoveTarget, "move target should remain active");
        }

        private static void MoveCommandPreservesAttackCooldown()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            state.EntityState.Units[10].AttackTargetId = 12;
            state.EntityState.Units[10].AttackCooldownTicksRemaining = 3;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 11 }, FixedVector2.FromInts(0, 2))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(2, state.EntityState.Units[10].AttackCooldownTicksRemaining, "disengage should preserve recovery cooldown and allow normal tick countdown");
        }

        private static void DisengageRequiresExplicitReattack()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            int hitPointsAfterAttack = state.EntityState.Units[11].HitPoints;

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 11 }, FixedVector2.FromInts(0, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 2; tick <= GameData.InfantryAttackCooldownTicks + 2; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)tick);
                AddNoOp(buffer, tick, 1, (uint)tick);
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(hitPointsAfterAttack, state.EntityState.Units[11].HitPoints, "disengaged unit should not resume attacking without explicit attack command");
            AssertEqual(0, state.EntityState.Units[10].AttackTargetId, "disengaged unit should stay without attack target");
        }

        private static void DeadUnitCleanupAfterCombat()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = CreateAdjacentCombatState();
            state.EntityState.Units[11].HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(12), "dead target should be removed from lookup");
        }

        private static void CombatReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var replay = new ReplayFile(1, rules, 303, 2, ReplayInitialState.Nomad);
            replay.Commands.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            GameState firstState = CreateAdjacentCombatState();
            GameState secondState = CreateAdjacentCombatState();
            ulong first = RunCommandsFromState(firstState, rules, replay.Commands, 1);
            ulong second = RunCommandsFromState(secondState, rules, replay.Commands, 1);
            AssertEqual(first, second, "combat command stream should be deterministic from same state");
        }

        private static void CombatLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 404, true);
            EntityFactory.CreateUnit(session.Peers[0].LocalState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(session.Peers[0].LocalState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(session.Peers[1].LocalState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(session.Peers[1].LocalState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "combat tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "combat lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "combat peer checksums should match");
        }

        private static void AttackDamagesBuilding()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateBuildingCombatState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus - GameData.InfantryAttackDamage, state.EntityState.Buildings[0].HitPoints, "attack should damage enemy building");
        }

        private static void CapitalDestructionRemovesBonus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateBuildingCombatState();
            state.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.PlayerStates.Players[1].CapitalStatus.IsCapitalAlive, "destroyed capital should no longer be alive");
            AssertEqual(false, state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "destroyed capital should remove bonus");
            AssertEqual(0, state.PlayerStates.Players[1].PopulationCap, "capital population bonus should be removed");
            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(12), "destroyed capital should be removed during cleanup");
        }

        private static void CapitalDestructionDoesNotDefeatPlayerWithAnotherTownCenter()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateBuildingCombatState();
            state.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;
            EntityFactory.CreateTownCenter(state, 1, FixedVector2.FromInts(2, 0));
            state.EntityState.Buildings[1].IsUnderConstruction = false;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.PlayerStates.Players[1].IsDefeated, "capital loss alone should not defeat player");
            AssertEqual(1, state.EntityState.Buildings.Count, "normal town center should remain after capital cleanup");
            AssertEqual(false, state.EntityState.Buildings[0].IsCapital, "remaining town center should not become replacement capital");
        }

        private static void CapitalDestructionReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12))
            };
            GameState firstState = CreateBuildingCombatState();
            firstState.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;
            GameState secondState = CreateBuildingCombatState();
            secondState.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;

            ulong first = RunCommandsFromState(firstState, rules, commands, 1);
            ulong second = RunCommandsFromState(secondState, rules, commands, 1);
            AssertEqual(first, second, "capital destruction should replay deterministically");
        }

        private static void CapitalDestructionLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 505, true);
            SetupBuildingCombatState(session.Peers[0].LocalState);
            SetupBuildingCombatState(session.Peers[1].LocalState);
            session.Peers[0].LocalState.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;
            session.Peers[1].LocalState.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "capital destruction tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "capital destruction lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "capital destruction peer checksums should match");
            AssertEqual(false, session.Peers[0].LocalState.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "capital bonus should be inactive in lockstep state");
        }

        private static void ResignNeutralizesAssetsAndAssignsPlacement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(12, 2);
            int buildingId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(1, 0));
            state.EntityState.Buildings[state.EntityState.EntityLookup[buildingId].Index].IsUnderConstruction = false;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.PlayerStates.Players[0].IsResigned, "player should be resigned");
            AssertEqual(true, state.PlayerStates.Players[0].IsDefeated, "resigned player should be defeated for placement");
            AssertEqual(2, state.PlayerStates.Players[0].Placement, "first defeated player in 2-player match should get second place");
            AssertEqual(0, state.RankingState.NextPlacement, "next placement should be exhausted after winner assignment");
            AssertEqual(true, state.MatchResultState.IsFinished, "match should finish after one player resigns in a two-player match");
            AssertEqual(1, state.PlayerStates.Players[1].Placement, "remaining player should receive first place");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, state.EntityState.Units[0].OwnerPlayerIndex, "resigned unit should become neutral");
            AssertEqual(GameData.ResignedAssetDespawnTicks, state.EntityState.Units[0].DespawnTicksRemaining, "resigned unit should get despawn timer");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, state.EntityState.Buildings[0].OwnerPlayerIndex, "resigned building should become neutral");
            AssertEqual(GameData.ResignedAssetDespawnTicks, state.EntityState.Buildings[0].DespawnTicksRemaining, "resigned building should get despawn timer");
        }

        private static void ResignedAssetsDespawnAfterTimer()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(13, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int i = 0; i < GameData.ResignedAssetDespawnTicks; i++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(0, state.EntityState.Units.Count, "resigned units should despawn after timer");
        }

        private static void ResignedPlayerNonNoOpCommandsReject()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(14, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(10, 0))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "resigned non-noop command should be rejected");
            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "neutral resigned unit should not receive move target");
        }

        private static void ResignationReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var recorder = new ReplayRecorder(rules, 15, 2, ReplayInitialState.Nomad);
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 10);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 10);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "resignation replay should be deterministic");
        }

        private static void ResignationLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 16, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Resign), new ResignCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "resignation tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "resignation lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "resignation peer checksums should match");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, session.Peers[0].LocalState.EntityState.Units[0].OwnerPlayerIndex, "resigned player unit should be neutral in lockstep");
        }

        private static void PlayerEliminatedWithNoTownCenterAndNoVillagers()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(21, 2);
            KillPlayerVillagers(state, 0);
            var buffer = new CommandBuffer();

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.PlayerStates.Players[0].IsDefeated, "player with no TC and no villagers should be defeated");
            AssertEqual(2, state.PlayerStates.Players[0].Placement, "first eliminated player should receive last place");
        }

        private static void PlayerSurvivesWithVillagerAfterTownCenterLoss()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(22, 2);
            EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(1, 0));
            state.EntityState.Buildings[0].IsUnderConstruction = false;
            state.EntityState.Buildings[0].IsDead = true;
            var buffer = new CommandBuffer();

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.PlayerStates.Players[0].IsDefeated, "player should survive TC loss if villagers remain");
        }

        private static void EliminationReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState first = GameInitializer.CreateNomadStart(23, 2);
            GameState second = GameInitializer.CreateNomadStart(23, 2);
            KillPlayerVillagers(first, 0);
            KillPlayerVillagers(second, 0);
            ulong firstChecksum = RunCommandsFromState(first, rules, new CommandEnvelope[0], 1);
            ulong secondChecksum = RunCommandsFromState(second, rules, new CommandEnvelope[0], 1);
            AssertEqual(firstChecksum, secondChecksum, "elimination should be deterministic");
        }

        private static void EliminationLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 24, true);
            KillPlayerVillagers(session.Peers[0].LocalState, 0);
            KillPlayerVillagers(session.Peers[1].LocalState, 0);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            AssertEqual(true, session.TryAdvanceOneTick(), "elimination tick should advance");
            AssertEqual(0, session.DesyncReports.Count, "elimination lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "elimination peer checksums should match");
            AssertEqual(true, session.Peers[0].LocalState.PlayerStates.Players[0].IsDefeated, "player should be defeated in lockstep");
        }

        private static void MatchEndsWhenOnePlayerRemains()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(25, 2);
            KillPlayerVillagers(state, 0);

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            AssertEqual(true, state.MatchResultState.IsFinished, "match should finish when one player remains");
            AssertEqual(1, state.MatchResultState.WinnerPlayerIndex, "remaining player should be winner");
            AssertEqual(1, state.PlayerStates.Players[1].Placement, "winner should receive first place");
            AssertEqual(2, state.PlayerStates.Players[0].Placement, "eliminated player should receive second place");
        }

        private static void MatchEndReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState first = GameInitializer.CreateNomadStart(26, 2);
            GameState second = GameInitializer.CreateNomadStart(26, 2);
            KillPlayerVillagers(first, 0);
            KillPlayerVillagers(second, 0);

            ulong firstChecksum = RunCommandsFromState(first, rules, new CommandEnvelope[0], 1);
            ulong secondChecksum = RunCommandsFromState(second, rules, new CommandEnvelope[0], 1);
            AssertEqual(firstChecksum, secondChecksum, "match end should be deterministic");
        }

        private static void MatchEndLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 27, true);
            KillPlayerVillagers(session.Peers[0].LocalState, 0);
            KillPlayerVillagers(session.Peers[1].LocalState, 0);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            AssertEqual(true, session.TryAdvanceOneTick(), "match end tick should advance");
            AssertEqual(0, session.DesyncReports.Count, "match end lockstep should not desync");
            AssertEqual(true, session.Peers[0].LocalState.MatchResultState.IsFinished, "match should finish in lockstep");
            AssertEqual(1, session.Peers[0].LocalState.PlayerStates.Players[1].Placement, "winner placement should be assigned in lockstep");
        }

        private static void TrainSiegeCannonCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(31, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Wood = GameData.SiegeCannonWoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.SiegeCannonGoldCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.SiegeCannon)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.SiegeCannonTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "siege cannon should complete training");
            AssertEqual(UnitTypeId.SiegeCannon, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be siege cannon");
            AssertEqual(8, state.PlayerStates.Players[0].PopulationUsed, "siege cannon should reserve three population");
        }

        private static void SiegeSetupDelaysFirstShot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            runner.AdvanceOneTick(state, rules, buffer);
            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus, state.EntityState.Buildings[0].HitPoints, "siege should not fire on first setup tick");
            AssertEqual(false, state.EntityState.Units[10].IsSiegeDeployed, "siege should not deploy immediately");

            AddNoOp(buffer, 1, 0, 1);
            AddNoOp(buffer, 1, 1, 1);
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            AddNoOp(buffer, 2, 1, 2);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.EntityState.Units[10].IsSiegeDeployed, "siege should deploy after setup ticks");
            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus - GameData.SiegeCannonBuildingDamage, state.EntityState.Buildings[0].HitPoints, "siege should fire after setup completes");
        }

        private static void SiegeReloadDelaysSecondShot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            AdvanceSiegeUntilFirstShot(rules, state, buffer, runner);
            int hitPointsAfterFirstShot = state.EntityState.Buildings[0].HitPoints;

            AddNoOp(buffer, state.Tick, 0, 3);
            AddNoOp(buffer, state.Tick, 1, 3);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(hitPointsAfterFirstShot, state.EntityState.Buildings[0].HitPoints, "reload should prevent immediate second shot");
            AssertEqual(GameData.SiegeCannonReloadTicks - 1, state.EntityState.Units[10].SiegeReloadTicksRemaining, "reload should count down after first shot");
        }

        private static void MovingSiegeCancelsDeployment()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            AdvanceSiegeUntilFirstShot(rules, state, buffer, runner);
            AssertEqual(true, state.EntityState.Units[10].IsSiegeDeployed, "siege should be deployed before move");

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 11 }, FixedVector2.FromInts(1, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 3, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Units[10].IsSiegeDeployed, "moving siege should cancel deployment");
            AssertEqual(0, state.EntityState.Units[10].SiegeSetupTicksRemaining, "moving siege should clear setup");
            AssertEqual(0, state.EntityState.Units[10].SiegeReloadTicksRemaining, "moving siege should clear reload");
        }

        private static void SiegeRejectsUnitTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            EntityFactory.CreateUnit(state, 0, UnitTypeId.SiegeCannon, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 13 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "siege should reject unit target in first slice");
        }

        private static void SiegeDestroysCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateSiegeCapitalState();
            state.EntityState.Buildings[0].HitPoints = GameData.SiegeCannonBuildingDamage;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            AdvanceSiegeUntilFirstShot(rules, state, buffer, runner);

            AssertEqual(false, state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "siege-destroyed capital should remove bonus");
            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(12), "siege-destroyed capital should be cleaned up");
        }

        private static void SiegeReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(2, 1, 2, CommandType.NoOp), new NoOpCommand())
            };

            ulong first = RunCommandsFromState(CreateSiegeCapitalState(), rules, commands, 3);
            ulong second = RunCommandsFromState(CreateSiegeCapitalState(), rules, commands, 3);
            AssertEqual(first, second, "siege replay should be deterministic");
        }

        private static void SiegeLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 32, true);
            SetupSiegeCapitalState(session.Peers[0].LocalState);
            SetupSiegeCapitalState(session.Peers[1].LocalState);

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "siege attack command tick should advance");
            for (int tick = 1; tick <= 2; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "siege setup tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "siege lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "siege peer checksums should match");
            AssertEqual(true, session.Peers[0].LocalState.EntityState.Units[10].IsSiegeDeployed, "siege should deploy in lockstep");
        }

        private static void TrainMangonelCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(33, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            state.PlayerStates.Players[0].Resources.Wood = GameData.MangonelWoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.MangonelGoldCost;
            int buildingId = state.EntityState.Buildings[0].Id;
            int initialUnits = state.EntityState.Units.Count;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.TrainUnit), new TrainUnitCommand(buildingId, UnitTypeId.Mangonel)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.MangonelTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnits + 1, state.EntityState.Units.Count, "mangonel should complete training");
            AssertEqual(UnitTypeId.Mangonel, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be mangonel");
            AssertEqual(8, state.PlayerStates.Players[0].PopulationUsed, "mangonel should reserve three population");
        }

        private static void MangonelAreaHitsMultipleEnemies()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.InfantryHitPoints - GameData.MangonelAreaDamage, state.EntityState.Units[11].HitPoints, "target enemy should take area damage");
            AssertEqual(GameData.InfantryHitPoints - GameData.MangonelAreaDamage, state.EntityState.Units[12].HitPoints, "nearby enemy should take area damage");
            AssertEqual(GameData.InfantryHitPoints, state.EntityState.Units[13].HitPoints, "enemy outside radius should not take area damage");
            AssertEqual(GameData.MangonelAreaCooldownTicks, state.EntityState.Units[10].AttackCooldownTicksRemaining, "mangonel cooldown should be set after firing");
        }

        private static void MangonelAreaIgnoresFriendlyUnits()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.InfantryHitPoints, state.EntityState.Units[14].HitPoints, "friendly unit inside radius should not take area damage");
        }

        private static void MangonelSimultaneousDeathsCleanup()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            state.EntityState.Units[11].HitPoints = GameData.MangonelAreaDamage;
            state.EntityState.Units[12].HitPoints = GameData.MangonelAreaDamage;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(12), "area-killed target should be cleaned up");
            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(13), "area-killed nearby unit should be cleaned up");
            AssertEqual(6, state.PlayerStates.Players[1].PopulationUsed, "population should remove both area-killed infantry");
        }

        private static void MangonelAreaDoesNotDamageCapital()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateMangonelAreaState();
            int capitalId = EntityFactory.CreateTownCenter(state, 1, FixedVector2.FromInts(5, 0));
            Building capital = state.EntityState.Buildings[state.EntityState.EntityLookup[capitalId].Index];
            capital.IsUnderConstruction = false;
            capital.HitPoints = GameData.GetBuildingCompletedHitPoints(BuildingTypeId.TownCenter, true);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus, capital.HitPoints, "mangonel area should not damage buildings in first slice");
        }

        private static void MangonelReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12))
            };

            ulong first = RunCommandsFromState(CreateMangonelAreaState(), rules, commands, 1);
            ulong second = RunCommandsFromState(CreateMangonelAreaState(), rules, commands, 1);
            AssertEqual(first, second, "mangonel area damage should replay deterministically");
        }

        private static void MangonelLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 34, true);
            SetupMangonelAreaState(session.Peers[0].LocalState);
            SetupMangonelAreaState(session.Peers[1].LocalState);

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "mangonel attack tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "mangonel lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "mangonel peer checksums should match");
            AssertEqual(GameData.InfantryHitPoints - GameData.MangonelAreaDamage, session.Peers[0].LocalState.EntityState.Units[12].HitPoints, "nearby enemy should take area damage in lockstep");
        }

        private static void WallRejectsMissingWood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(39, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "wall should reject without wood");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "missing wall wood should count as rejected");
        }

        private static void PlaceWallCreatesVulnerableConstruction()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(40, 1);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "wall should be created");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "wall should spend wood");
            AssertEqual(BuildingTypeId.Wall, state.EntityState.Buildings[0].BuildingTypeId, "created building should be wall");
            AssertEqual(true, state.EntityState.Buildings[0].IsUnderConstruction, "wall should start under construction");
            AssertEqual(GameData.WallUnderConstructionHitPoints, state.EntityState.Buildings[0].HitPoints, "under-construction wall should be vulnerable");
            AssertEqual(false, state.EntityState.Buildings[0].IsCapital, "wall should never be capital");
        }

        private static void AssignedVillagersCompleteWall()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(41, 1);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            runner.AdvanceOneTick(state, rules, buffer);
            int wallId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(wallId, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Buildings[0].IsUnderConstruction, "two villagers should complete wall in two ticks");
            AssertEqual(GameData.WallHitPoints, state.EntityState.Buildings[0].HitPoints, "completed wall should have full HP");
            AssertEqual(0, state.EntityState.Units[0].CurrentBuildTargetId, "builder assignment should clear after completion");
        }

        private static void UnderConstructionWallCanBeDestroyed()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(42, 2);
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(3, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            int wallId = state.EntityState.Buildings[0].Id;
            state.EntityState.Buildings[0].HitPoints = GameData.InfantryAttackDamage;

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.NoOp), new NoOpCommand()));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.Attack), new AttackCommand(new[] { 11 }, wallId)));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(wallId), "destroyed under-construction wall should be removed");
        }

        private static void WallReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2 })),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand())
            };
            GameState firstState = GameInitializer.CreateNomadStart(43, 1);
            GameState secondState = GameInitializer.CreateNomadStart(43, 1);
            firstState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            secondState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;

            ulong first = RunCommandsFromState(firstState, rules, commands, 3);
            ulong second = RunCommandsFromState(secondState, rules, commands, 3);
            AssertEqual(first, second, "wall replay should be deterministic");
        }

        private static void WallLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 44, true);
            session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            session.Peers[1].LocalState.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(3, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "wall placement tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "wall build assignment tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "wall lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "wall peer checksums should match");
        }

        private static void TradePostRejectsBeforeCompletedTownCenter()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(53, 1);
            FundTradePost(state, 0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Buildings.Count, "trade post should require completed town center");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "early trade post placement should be rejected");
        }

        private static void TradePostRejectsMissingResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(54, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.EntityState.Buildings.Count, "trade post should reject without resources");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "missing trade post resources should count as rejected");
        }

        private static void PlaceTradePostCreatesConstruction()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(53, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(state, 0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Building tradePost = state.EntityState.Buildings[1];
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Wood, "trade post should spend wood");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Gold, "trade post should spend gold");
            AssertEqual(BuildingTypeId.TradePost, tradePost.BuildingTypeId, "trade post placement should create trade post");
            AssertEqual(true, tradePost.IsUnderConstruction, "placed trade post should start under construction");
            AssertEqual(GameData.TradePostUnderConstructionHitPoints, tradePost.HitPoints, "placed trade post should be vulnerable while building");
        }

        private static void AssignedVillagersCompleteTradePost()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(54, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(state, 0);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(7, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 3, 0, 3);
            runner.AdvanceOneTick(state, rules, buffer);

            Building tradePost = state.EntityState.Buildings[1];
            AssertEqual(false, tradePost.IsUnderConstruction, "assigned villagers should complete trade post");
            AssertEqual(GameData.TradePostHitPoints, tradePost.HitPoints, "completed trade post should receive full hit points");
        }

        private static void TradePostReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))),
                new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(7, new[] { 1, 2 })),
                new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()),
                new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.NoOp), new NoOpCommand())
            };
            GameState firstState = GameInitializer.CreateNomadStart(55, 1);
            GameState secondState = GameInitializer.CreateNomadStart(55, 1);
            AddCompletedTownCenter(firstState, 0, FixedVector2.FromInts(0, 0));
            AddCompletedTownCenter(secondState, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(firstState, 0);
            FundTradePost(secondState, 0);

            ulong first = RunCommandsFromState(firstState, rules, commands, 4);
            ulong second = RunCommandsFromState(secondState, rules, commands, 4);
            AssertEqual(first, second, "trade post replay checksums should match");
        }

        private static void TradePostLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 56, true);
            AddCompletedTownCenter(session.Peers[0].LocalState, 0, FixedVector2.FromInts(0, 0));
            AddCompletedTownCenter(session.Peers[1].LocalState, 0, FixedVector2.FromInts(0, 0));
            FundTradePost(session.Peers[0].LocalState, 0);
            FundTradePost(session.Peers[1].LocalState, 0);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTradePost), new PlaceTradePostCommand(FixedVector2.FromInts(20, 20))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post placement tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(12, new[] { 1, 2 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post build assignment tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 1, 2, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post build tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(3, 0, 3, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(3, 1, 3, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post completion tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "trade post lockstep should not desync");
            AssertEqual(false, session.Peers[0].LocalState.EntityState.Buildings[1].IsUnderConstruction, "trade post should complete in lockstep state");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "trade post peer checksums should match");
        }

        private static void TrainTradeCartCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(50, 1);
            int postId = EntityFactory.CreateTradePost(state, 0, FixedVector2.FromInts(0, 0));
            state.PlayerStates.Players[0].PopulationCap = 10;
            state.PlayerStates.Players[0].Resources.Wood = GameData.TradeCartWoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.TradeCartGoldCost;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.TrainUnit), new TrainUnitCommand(postId, UnitTypeId.TradeCart)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.TradeCartTrainTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(1 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(UnitTypeId.TradeCart, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trade post should train trade cart");
        }

        private static void TradeRoutePaysGoldOnArrival()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTradeState(10);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(6, 7, 8)));

            for (int tick = 0; tick < 18; tick++)
            {
                if (tick > 0)
                {
                    AddNoOp(buffer, tick, 0, (uint)tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(20, state.PlayerStates.Players[0].Resources.Gold, "trade cart should pay gold based on route length");
            AssertEqual(7, state.EntityState.Units[5].TradeDestinationId, "cart should head back to first post after payment");
        }

        private static void LongerTradeRoutePaysMore()
        {
            GameState shortRoute = CreateTradeState(10);
            GameState longRoute = CreateTradeState(20);
            AssertEqual(true, shortRoute.EntityState.Units[5].Id == 6, "trade cart id should be deterministic");
            var shortCommand = new CreateTradeRouteCommand(6, 7, 8);
            var longCommand = new CreateTradeRouteCommand(6, 7, 8);
            shortCommand.Execute(shortRoute, GameRules.CreatePhaseZeroDefaults(1), new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute));
            longCommand.Execute(longRoute, GameRules.CreatePhaseZeroDefaults(1), new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute));
            AssertEqual(true, longRoute.EntityState.Units[5].TradeIncomePerTrip > shortRoute.EntityState.Units[5].TradeIncomePerTrip, "longer route should pay more");
        }

        private static void DestroyedTradeEndpointClearsRoute()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTradeState(10);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(6, 7, 8)));
            runner.AdvanceOneTick(state, rules, buffer);
            state.EntityState.Buildings[1].IsDead = true;
            AddNoOp(buffer, 1, 0, 1);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Units[5].TradeRouteAId, "destroyed endpoint should clear trade route");
        }

        private static void TradeCartCanBeKilled()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(51, 2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.TradeCart, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            state.EntityState.Units[10].HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.Attack), new AttackCommand(new[] { 12 }, 11)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(11), "trade cart should be vulnerable to attack");
        }

        private static void TradeReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var commands = new List<CommandEnvelope>
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(6, 7, 8))
            };

            for (int tick = 1; tick < 18; tick++)
            {
                commands.Add(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
            }

            GameState firstState = CreateTradeState(10);
            GameState secondState = CreateTradeState(10);
            ulong first = RunCommandsFromState(firstState, rules, commands, 18);
            ulong second = RunCommandsFromState(secondState, rules, commands, 18);
            AssertEqual(first, second, "trade route trip should replay deterministically");
            AssertEqual(20, firstState.PlayerStates.Players[0].Resources.Gold, "replayed trade route should pay gold on arrival");
        }

        private static void TradeLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 52, true);
            SetupTradeState(session.Peers[0].LocalState, 10);
            SetupTradeState(session.Peers[1].LocalState, 10);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.CreateTradeRoute), new CreateTradeRouteCommand(11, 12, 13)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade route tick should advance");
            for (int tick = 1; tick < 18; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "trade movement tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "trade lockstep should not desync");
            AssertEqual(20, session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Gold, "trade income should be paid in lockstep state");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "trade peer checksums should match");
        }

        private static void ChaosV1StressSmoke()
        {
            StressScenarioResult result = new StressScenarioRunner().RunChaosV1(1200, 77);
            AssertEqual(true, result.Passed, "chaos v1 stress should pass invariants");
            AssertEqual(1200, result.FinalTick, "chaos v1 stress should reach requested tick");
        }

        private static void ChaosV2StressSmoke()
        {
            StressScenarioResult result = new StressScenarioRunner().RunChaosV2(1200, 78);
            AssertEqual(true, result.Passed, "chaos v2 stress should pass invariants");
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

        private static ulong RunNoOpSimulation(int ticks, int players, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(players);
            var state = new GameState(seed, players);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();

            for (int tick = 0; tick < ticks; tick++)
            {
                for (int player = 0; player < players; player++)
                {
                    buffer.Add(new CommandEnvelope(new CommandHeader(tick, player, 0, CommandType.NoOp), new NoOpCommand()));
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            return state.LastChecksum;
        }

        private static ulong RunDebugCommands(GameRules rules, IEnumerable<CommandEnvelope> commands)
        {
            var state = new GameState(1, rules.MaxPlayers);
            var buffer = new CommandBuffer();
            foreach (CommandEnvelope command in commands)
            {
                buffer.Add(command);
            }

            new TickRunner().AdvanceOneTick(state, rules, buffer);
            return state.LastChecksum;
        }

        private static CommandEnvelope DebugCommand(int tick, int player, uint sequence, int amount)
        {
            return new CommandEnvelope(
                new CommandHeader(tick, player, sequence, CommandType.DebugIncrementCounter),
                new DebugIncrementCounterCommand(amount));
        }

        private static void CompleteCapitalForPlayerZero(GameRules rules, GameState state, CommandBuffer buffer, TickRunner runner)
        {
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            runner.AdvanceOneTick(state, rules, buffer);
            int buildingId = state.EntityState.Buildings[0].Id;
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(buildingId, new[] { 1, 2, 3, 4 })));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            runner.AdvanceOneTick(state, rules, buffer);
        }

        private static void AddNoOp(CommandBuffer buffer, int tick, int player, uint sequence)
        {
            buffer.Add(new CommandEnvelope(new CommandHeader(tick, player, sequence, CommandType.NoOp), new NoOpCommand()));
        }

        private static int AddCompletedTownCenter(GameState state, int ownerPlayerIndex, FixedVector2 position)
        {
            int townCenterId = EntityFactory.CreateTownCenter(state, ownerPlayerIndex, position);
            Building townCenter = state.EntityState.Buildings[state.EntityState.EntityLookup[townCenterId].Index];
            townCenter.IsUnderConstruction = false;
            townCenter.BuildProgressTicks = GameData.TownCenterBuildTicks;
            townCenter.HitPoints = GameData.GetBuildingCompletedHitPoints(BuildingTypeId.TownCenter, townCenter.IsCapital);
            if (townCenter.IsCapital)
            {
                PlayerState player = state.PlayerStates.Players[ownerPlayerIndex];
                player.CapitalStatus.IsCapitalAlive = true;
                player.CapitalStatus.CapitalBonusActive = true;
                player.PopulationCap += GameData.CapitalPopulationBonus;
            }

            return townCenterId;
        }

        private static void FundTradePost(GameState state, int playerIndex)
        {
            state.PlayerStates.Players[playerIndex].Resources.Wood = GameData.TradePostWoodCost;
            state.PlayerStates.Players[playerIndex].Resources.Gold = GameData.TradePostGoldCost;
        }

        private static GameState CreateWallBlockingState(ulong seed)
        {
            GameState state = GameInitializer.CreateNomadStart(seed, 1);
            AddCompletedWall(state, 0, FixedVector2.FromInts(2, 0));
            return state;
        }

        private static GameState CreatePathfindingWallState()
        {
            GameState state = CreateOccupancyState(17);
            AddCompletedWall(state, 0, FixedVector2.FromInts(2, 0));
            return state;
        }

        private static GameState CreateOccupancyState(ulong seed)
        {
            return CreateOccupancyState(seed, 1);
        }

        private static GameState CreateOccupancyState(ulong seed, int playerCount)
        {
            return new GameState(seed, playerCount);
        }

        private static int AddCompletedWall(GameState state, int ownerPlayerIndex, FixedVector2 position)
        {
            int wallId = EntityFactory.CreateWall(state, ownerPlayerIndex, position);
            Building wall = state.EntityState.Buildings[state.EntityState.EntityLookup[wallId].Index];
            wall.IsUnderConstruction = false;
            wall.BuildProgressTicks = GameData.WallBuildTicks;
            wall.HitPoints = GameData.WallHitPoints;
            return wallId;
        }

        private static void AddVerticalBarrier(GameState state, int x, int startY, int length)
        {
            for (int i = 0; i < length; i++)
            {
                AddCompletedWall(state, 0, FixedVector2.FromInts(x, startY + i));
            }
        }

        private static GameState CreateAdjacentCombatState()
        {
            GameState state = GameInitializer.CreateNomadStart(9, 2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            return state;
        }

        private static GameState CreateCavalryCombatState()
        {
            GameState state = GameInitializer.CreateNomadStart(62, 2);
            SetupCavalryCombatState(state);
            return state;
        }

        private static void SetupCavalryCombatState(GameState state)
        {
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Cavalry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
        }

        private static GameState CreateBuildingCombatState()
        {
            GameState state = GameInitializer.CreateNomadStart(10, 2);
            SetupBuildingCombatState(state);
            return state;
        }

        private static GameState CreateSiegeCapitalState()
        {
            GameState state = GameInitializer.CreateNomadStart(30, 2);
            SetupSiegeCapitalState(state);
            return state;
        }

        private static GameState CreateMangonelAreaState()
        {
            GameState state = GameInitializer.CreateNomadStart(34, 2);
            SetupMangonelAreaState(state);
            return state;
        }

        private static void SetupMangonelAreaState(GameState state)
        {
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Mangonel, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(4, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(5, 0));
            EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(7, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(5, 0));
        }

        private static void SetupSiegeCapitalState(GameState state)
        {
            EntityFactory.CreateUnit(state, 0, UnitTypeId.SiegeCannon, FixedVector2.FromInts(0, 0));
            int capitalId = EntityFactory.CreateTownCenter(state, 1, FixedVector2.FromInts(8, 0));
            Building capital = state.EntityState.Buildings[state.EntityState.EntityLookup[capitalId].Index];
            capital.IsUnderConstruction = false;
            state.PlayerStates.Players[1].CapitalStatus.IsCapitalAlive = true;
            state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive = true;
            state.PlayerStates.Players[1].PopulationCap = GameData.CapitalPopulationBonus;
        }

        private static void AdvanceSiegeUntilFirstShot(GameRules rules, GameState state, CommandBuffer buffer, TickRunner runner)
        {
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 1, 0, 1);
            AddNoOp(buffer, 1, 1, 1);
            runner.AdvanceOneTick(state, rules, buffer);
            AddNoOp(buffer, 2, 0, 2);
            AddNoOp(buffer, 2, 1, 2);
            runner.AdvanceOneTick(state, rules, buffer);
        }

        private static GameState CreateTradeState(int distanceTiles)
        {
            GameState state = GameInitializer.CreateNomadStart(49, 1);
            SetupTradeState(state, distanceTiles);
            return state;
        }

        private static void SetupTradeState(GameState state, int distanceTiles)
        {
            EntityFactory.CreateUnit(state, 0, UnitTypeId.TradeCart, FixedVector2.FromInts(0, 20));
            EntityFactory.CreateTradePost(state, 0, FixedVector2.FromInts(0, 20));
            EntityFactory.CreateTradePost(state, 0, FixedVector2.FromInts(distanceTiles, 20));
        }

        private static void SetupBuildingCombatState(GameState state)
        {
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            int capitalId = EntityFactory.CreateTownCenter(state, 1, FixedVector2.FromInts(1, 0));
            Building capital = state.EntityState.Buildings[state.EntityState.EntityLookup[capitalId].Index];
            capital.IsUnderConstruction = false;
            state.PlayerStates.Players[1].CapitalStatus.IsCapitalAlive = true;
            state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive = true;
            state.PlayerStates.Players[1].PopulationCap = GameData.CapitalPopulationBonus;
        }

        private static ulong RunCommandsFromState(GameState state, GameRules rules, IEnumerable<CommandEnvelope> commands, int ticks)
        {
            var buffer = new CommandBuffer();
            foreach (CommandEnvelope command in commands)
            {
                buffer.Add(command);
            }

            var runner = new TickRunner();
            for (int i = 0; i < ticks; i++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
            }

            return state.LastChecksum;
        }

        private static void KillPlayerVillagers(GameState state, int playerIndex)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.OwnerPlayerIndex == playerIndex && unit.UnitTypeId == UnitTypeId.Villager)
                {
                    unit.HitPoints = 0;
                }
            }
        }

        private static bool IsVisible(GameState state, int player, int x, int y)
        {
            return state.VisibilityState.Players[player].VisibleTiles[state.VisibilityState.GetIndex(x, y)];
        }

        private static bool IsExplored(GameState state, int player, int x, int y)
        {
            return state.VisibilityState.Players[player].ExploredTiles[state.VisibilityState.GetIndex(x, y)];
        }

        private static bool HasPrimitive(VisualFrame frame, VisualPrimitiveKind kind)
        {
            for (int i = 0; i < frame.Primitives.Count; i++)
            {
                if (frame.Primitives[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static VisualPrimitive FindPrimitive(VisualFrame frame, VisualPrimitiveKind kind, int entityId)
        {
            for (int i = 0; i < frame.Primitives.Count; i++)
            {
                if (frame.Primitives[i].Kind == kind && frame.Primitives[i].EntityId == entityId)
                {
                    return frame.Primitives[i];
                }
            }

            throw new InvalidOperationException("primitive not found kind=" + kind + " entity=" + entityId);
        }

        private static LockstepSession RunLockstep(int ticks, int players, ulong seed, bool reverseDelivery)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(players);
            var session = new LockstepSession(rules, seed);
            uint sequence = 0;

            for (int tick = 0; tick < ticks; tick++)
            {
                var commands = new List<CommandEnvelope>();
                for (int player = 0; player < players; player++)
                {
                    var header = new CommandHeader(tick, player, sequence++, CommandType.NoOp);
                    commands.Add(new CommandEnvelope(header, new NoOpCommand()));
                }

                if (reverseDelivery)
                {
                    commands.Reverse();
                }

                foreach (CommandEnvelope command in commands)
                {
                    session.Broadcast(command);
                }

                session.TryAdvanceOneTick();
            }

            return session;
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " expected=" + expected + " actual=" + actual);
            }
        }

        private static void AssertFalse(bool value, string message)
        {
            if (value)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct TestCase
        {
            public string Name { get; }
            public Action Run { get; }

            public TestCase(string name, Action run)
            {
                Name = name;
                Run = run;
            }
        }
    }
}
