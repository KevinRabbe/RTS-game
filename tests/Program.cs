using RtsGame.Net.Lockstep;
using RtsGame.Presentation.ClientInput;
using RtsGame.Presentation.GodotBridge;
using RtsGame.Presentation.LocalPlay;
using RtsGame.Presentation.Snapshots;
using RtsGame.Presentation.Visuals;
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
        public static int Main(string[] args)
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
                new TestCase("test runner parses filter argument", TestRunnerParsesFilterArgument),
                new TestCase("test runner matches filter case insensitive", TestRunnerMatchesFilterCaseInsensitive),
                new TestCase("test runner runs all without filter", TestRunnerRunsAllWithoutFilter),
                new TestCase("test runner detects list argument", TestRunnerDetectsListArgument),
                new TestCase("test runner detects fail fast argument", TestRunnerDetectsFailFastArgument),
                new TestCase("test runner detects help argument", TestRunnerDetectsHelpArgument),
                new TestCase("nomad start creates initial units", NomadStartCreatesInitialUnits),
                new TestCase("nomad map creates center resources", NomadMapCreatesCenterResources),
                new TestCase("resource profiles define stockpile kind and node type", ResourceProfilesDefineStockpileKindAndNodeType),
                new TestCase("nomad resources create areas and profiled nodes", NomadResourcesCreateAreasAndProfiledNodes),
                new TestCase("resource visual overhang does not change sim footprint", ResourceVisualOverhangDoesNotChangeSimFootprint),
                new TestCase("resource interaction ring uses sim footprint", ResourceInteractionRingUsesSimFootprint),
                new TestCase("large resource interaction ring uses larger sim footprint", LargeResourceInteractionRingUsesLargerSimFootprint),
                new TestCase("building interaction ring uses sim footprint", BuildingInteractionRingUsesSimFootprint),
                new TestCase("town center ring accepts workers on every side", TownCenterRingAcceptsWorkersOnEverySide),
                new TestCase("dry arabia test map initializes deterministically", DryArabiaTestMapInitializesDeterministically),
                new TestCase("dry arabia resources create typed areas", DryArabiaResourcesCreateTypedAreas),
                new TestCase("dry arabia test map has valid tc placement zones", DryArabiaTestMapHasValidTcPlacementZones),
                new TestCase("dry arabia test map has nearby resources", DryArabiaTestMapHasNearbyResources),
                new TestCase("dry arabia test map resources avoid tc zones", DryArabiaTestMapResourcesAvoidTcZones),
                new TestCase("dry arabia tc zones have open traffic buffer", DryArabiaTcZonesHaveOpenTrafficBuffer),
                new TestCase("dry arabia starting resource areas contain nodes", DryArabiaStartingResourceAreasContainNodes),
                new TestCase("dry arabia starting resource areas have valid interaction slots", DryArabiaStartingResourceAreasHaveValidInteractionSlots),
                new TestCase("dry arabia tc foundation has reachable interaction ring", DryArabiaTcFoundationHasReachableInteractionRing),
                new TestCase("dry arabia starting villagers can all receive build assignment", DryArabiaStartingVillagersCanAllReceiveBuildAssignment),
                new TestCase("dry arabia decorative rock tiles are not sim blockers", DryArabiaDecorativeRockTilesAreNotSimBlockers),
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
                new TestCase("gather accepts mixed selection with incompatible carry", GatherAcceptsMixedSelectionWithIncompatibleCarry),
                new TestCase("gather reject reason for depleted resource is target complete", GatherRejectReasonForDepletedResourceIsTargetComplete),
                new TestCase("depleted resource clears gather assignment", DepletedResourceClearsGatherAssignment),
                new TestCase("tree depletion reduces amount and unblocks footprint", TreeDepletionReducesAmountAndUnblocksFootprint),
                new TestCase("depleted resource rejects gather command", DepletedResourceRejectsGatherCommand),
                new TestCase("forest continuation chooses another tree", ForestContinuationChoosesAnotherTree),
                new TestCase("berry patch continuation chooses another bush", BerryPatchContinuationChoosesAnotherBush),
                new TestCase("gold deposit continuation chooses another vein", GoldDepositContinuationChoosesAnotherVein),
                new TestCase("resource area exhaustion idles worker cleanly", ResourceAreaExhaustionIdlesWorkerCleanly),
                new TestCase("gather command keeps selected food target id", GatherCommandKeepsSelectedFoodTargetId),
                new TestCase("gather command keeps selected wood target id", GatherCommandKeepsSelectedWoodTargetId),
                new TestCase("gather command keeps selected gold target id", GatherCommandKeepsSelectedGoldTargetId),
                new TestCase("gather command sets movement toward resource", GatherCommandSetsMovementTowardResource),
                new TestCase("gather move target uses resource interaction ring", GatherMoveTargetUsesResourceInteractionRing),
                new TestCase("multiple workers reserve distinct resource slots", MultipleWorkersReserveDistinctResourceSlots),
                new TestCase("multiple workers on same resource do not stack", MultipleWorkersOnSameResourceDoNotStack),
                new TestCase("stale resource slot reservation chooses alternate", StaleResourceSlotReservationChoosesAlternate),
                new TestCase("stale resource slot reservation falls back when only slot remains", StaleResourceSlotReservationFallsBackWhenOnlySlotRemains),
                new TestCase("stale resource slot retarget can switch to sibling node", StaleResourceSlotRetargetCanSwitchToSiblingNode),
                new TestCase("blocked waiting gather can fallback to sibling node without reservation", BlockedWaitingGatherCanFallbackToSiblingNodeWithoutReservation),
                new TestCase("interaction reservation scoring prefers less congested tile", InteractionReservationScoringPrefersLessCongestedTile),
                new TestCase("multi worker resource traffic makes progress", MultiWorkerResourceTrafficMakesProgress),
                new TestCase("worker in resource range gathers without move rewrite", WorkerInResourceRangeGathersWithoutMoveRewrite),
                new TestCase("villager does not gather outside resource range", VillagerDoesNotGatherOutsideResourceRange),
                new TestCase("villager gathers in resource interaction range", VillagerGathersInResourceInteractionRange),
                new TestCase("villager gathers from diagonal resource interaction tile", VillagerGathersFromDiagonalResourceInteractionTile),
                new TestCase("villager returns to dropoff when full", VillagerReturnsToDropoffWhenFull),
                new TestCase("dropoff move target uses town center interaction ring", DropoffMoveTargetUsesTownCenterInteractionRing),
                new TestCase("multiple full food carriers reserve distinct dropoff slots", MultipleFullFoodCarriersReserveDistinctDropoffSlots),
                new TestCase("multiple full wood carriers reserve distinct dropoff slots", MultipleFullWoodCarriersReserveDistinctDropoffSlots),
                new TestCase("multiple full gold carriers reserve distinct dropoff slots", MultipleFullGoldCarriersReserveDistinctDropoffSlots),
                new TestCase("multiple full carriers dropping at same tc do not stack", MultipleFullCarriersDroppingAtSameTcDoNotStack),
                new TestCase("multiple full carriers dropping at same tc deposit cleanly", MultipleFullCarriersDroppingAtSameTcDepositCleanly),
                new TestCase("stale dropoff slot timeout clears and reassigns", StaleDropoffSlotTimeoutClearsAndReassigns),
                new TestCase("worker in dropoff range deposits without move rewrite", WorkerInDropoffRangeDepositsWithoutMoveRewrite),
                new TestCase("villager deposits from diagonal town center interaction tile", VillagerDepositsFromDiagonalTownCenterInteractionTile),
                new TestCase("villager resumes resource loop after deposit", VillagerResumesResourceLoopAfterDeposit),
                new TestCase("villager returns to same food target after deposit", VillagerReturnsToSameFoodTargetAfterDeposit),
                new TestCase("villager returns to same wood target after deposit", VillagerReturnsToSameWoodTargetAfterDeposit),
                new TestCase("villager returns to same gold target after deposit", VillagerReturnsToSameGoldTargetAfterDeposit),
                new TestCase("villager keeps explicitly assigned gold node across deposit loop", VillagerKeepsExplicitlyAssignedGoldNodeAcrossDepositLoop),
                new TestCase("explicit gather retarget switches assigned node", ExplicitGatherRetargetSwitchesAssignedNode),
                new TestCase("gather keeps assigned resource when nearer same type exists", GatherKeepsAssignedResourceWhenNearerSameTypeExists),
                new TestCase("gather move target remains stable while approaching", GatherMoveTargetRemainsStableWhileApproaching),
                new TestCase("worker gather loop diagnostics stay stable", WorkerGatherLoopDiagnosticsStayStable),
                new TestCase("full gold carrier blocked dropoff target retargets and deposits", FullGoldCarrierBlockedDropoffTargetRetargetsAndDeposits),
                new TestCase("full food carrier blocked dropoff target retargets and deposits", FullFoodCarrierBlockedDropoffTargetRetargetsAndDeposits),
                new TestCase("full wood carrier blocked dropoff target retargets and deposits", FullWoodCarrierBlockedDropoffTargetRetargetsAndDeposits),
                new TestCase("construction pacing does not complete instantly", ConstructionPacingDoesNotCompleteInstantly),
                new TestCase("construction progress advances gradually", ConstructionProgressAdvancesGradually),
                new TestCase("two builders in range build faster than one", TwoBuildersInRangeBuildFasterThanOne),
                new TestCase("build move target uses foundation interaction ring", BuildMoveTargetUsesFoundationInteractionRing),
                new TestCase("multiple builders reserve distinct build slots", MultipleBuildersReserveDistinctBuildSlots),
                new TestCase("assign build accepts temporary congestion intent", AssignBuildAcceptsTemporaryCongestionIntent),
                new TestCase("assign build accepts mixed selection and ignores non builders", AssignBuildAcceptsMixedSelectionAndIgnoresNonBuilders),
                new TestCase("assign build reject reason for completed target", AssignBuildRejectReasonForCompletedTarget),
                new TestCase("builder in build range builds without micro movement", BuilderInBuildRangeBuildsWithoutMicroMovement),
                new TestCase("builder blocked approach retargets deterministically", BuilderBlockedApproachRetargetsDeterministically),
                new TestCase("movement arrival snaps without raw oscillation", MovementArrivalSnapsWithoutRawOscillation),
                new TestCase("worker sub tile jitter does not reset no progress timeout", WorkerSubTileJitterDoesNotResetNoProgressTimeout),
                new TestCase("economy replay determinism", EconomyReplayDeterminism),
                new TestCase("economy lockstep", EconomyLockstep),
                new TestCase("train villager pays cost and completes", TrainVillagerPaysCostAndCompletes),
                new TestCase("trained villager spawns outside town center footprint", TrainedVillagerSpawnsOutsideTownCenterFootprint),
                new TestCase("trained villager avoids occupied spawn slot", TrainedVillagerAvoidsOccupiedSpawnSlot),
                new TestCase("blocked spawn waits until slot opens", BlockedSpawnWaitsUntilSlotOpens),
                new TestCase("multiple trained villagers use different spawn slots", MultipleTrainedVillagersUseDifferentSpawnSlots),
                new TestCase("trained villager avoids reserved spawn slot", TrainedVillagerAvoidsReservedSpawnSlot),
                new TestCase("spawn slot selection is deterministic", SpawnSlotSelectionIsDeterministic),
                new TestCase("train villager rejects missing resources", TrainVillagerRejectsMissingResources),
                new TestCase("train villager respects population cap", TrainVillagerRespectsPopulationCap),
                new TestCase("training replay determinism", TrainingReplayDeterminism),
                new TestCase("training lockstep", TrainingLockstep),
                new TestCase("move unit advances deterministically", MoveUnitAdvancesDeterministically),
                new TestCase("move unit snaps to target", MoveUnitSnapsToTarget),
                new TestCase("move command clears work assignments", MoveCommandClearsWorkAssignments),
                new TestCase("move arrival clears destination reservation", MoveArrivalClearsDestinationReservation),
                new TestCase("move replacement clears destination reservation", MoveReplacementClearsDestinationReservation),
                new TestCase("unit death clears destination reservation", UnitDeathClearsDestinationReservation),
                new TestCase("resign clears destination reservation", ResignClearsDestinationReservation),
                new TestCase("move rejects wall-blocked target", MoveRejectsWallBlockedTarget),
                new TestCase("move reject reason for wall blocked target", MoveRejectReasonForWallBlockedTarget),
                new TestCase("move rejects resource-blocked target", MoveRejectsResourceBlockedTarget),
                new TestCase("move rejects unreachable open target", MoveRejectsUnreachableOpenTarget),
                new TestCase("move accepts multi-select when at least one unit can path", MoveAcceptsMultiSelectWhenAtLeastOneUnitCanPath),
                new TestCase("movement pathfinds around wall", MovementPathfindsAroundWall),
                new TestCase("pathfinder returns same first step", PathfinderReturnsSameFirstStep),
                new TestCase("pathfinder wall blocks path", PathfinderWallBlocksPath),
                new TestCase("pathfinder blocks building and resource tiles", PathfinderBlocksBuildingAndResourceTiles),
                new TestCase("destroyed wall opens path next tick", DestroyedWallOpensPathNextTick),
                new TestCase("no path returns failure deterministically", NoPathReturnsFailureDeterministically),
                new TestCase("unit ordered to occupied destination receives nearby slot", UnitOrderedToOccupiedDestinationReceivesNearbySlot),
                new TestCase("occupied next step uses deterministic alternate", OccupiedNextStepUsesDeterministicAlternate),
                new TestCase("occupied next step can use diagonal alternate", OccupiedNextStepCanUseDiagonalAlternate),
                new TestCase("alternate step avoids occupied tiles", AlternateStepAvoidsOccupiedTiles),
                new TestCase("alternate step avoids static blockers", AlternateStepAvoidsStaticBlockers),
                new TestCase("temporary live unit blockage preserves move target", TemporaryLiveUnitBlockagePreservesMoveTarget),
                new TestCase("tc front blocker allows pass around progress", TcFrontBlockerAllowsPassAroundProgress),
                new TestCase("resource dropoff blocker preserves worker intent", ResourceDropoffBlockerPreservesWorkerIntent),
                new TestCase("moving unit can enter vacated tile without stacking", MovingUnitCanEnterVacatedTileWithoutStacking),
                new TestCase("group move assigns distinct destination slots", GroupMoveAssignsDistinctDestinationSlots),
                new TestCase("ten unit group move does not stack", TenUnitGroupMoveDoesNotStack),
                new TestCase("twenty unit group move settles or waits without stacking", TwentyUnitGroupMoveSettlesOrWaitsWithoutStacking),
                new TestCase("group move avoids reserved final destination slots", GroupMoveAvoidsReservedFinalDestinationSlots),
                new TestCase("group move does not cause endless jitter", GroupMoveDoesNotCauseEndlessJitter),
                new TestCase("dry arabia two villager ground move near tc makes progress", DryArabiaTwoVillagerGroundMoveNearTcMakesProgress),
                new TestCase("dry arabia four villager ground move around tc makes progress", DryArabiaFourVillagerGroundMoveAroundTcMakesProgress),
                new TestCase("dry arabia five villager group move near resources stays bounded", DryArabiaFiveVillagerGroupMoveNearResourcesStaysBounded),
                new TestCase("dry arabia repeated group move replacement clears stale destinations", DryArabiaRepeatedGroupMoveReplacementClearsStaleDestinations),
                new TestCase("dry arabia group move followed by gather clears move destinations", DryArabiaGroupMoveFollowedByGatherClearsMoveDestinations),
                new TestCase("dry arabia gather followed by group move clears resource reservations", DryArabiaGatherFollowedByGroupMoveClearsResourceReservations),
                new TestCase("group gather does not collapse onto one interaction slot", GroupGatherDoesNotCollapseOntoOneInteractionSlot),
                new TestCase("group gather workers make progress or wait cleanly", GroupGatherWorkersMakeProgressOrWaitCleanly),
                new TestCase("group gather resolves clicked node to resource area distribution", GroupGatherResolvesClickedNodeToResourceAreaDistribution),
                new TestCase("group gather avoids single node collapse when sibling nodes exist", GroupGatherAvoidsSingleNodeCollapseWhenSiblingNodesExist),
                new TestCase("dry arabia berry group gather makes bounded food progress", DryArabiaBerryGroupGatherMakesBoundedFoodProgress),
                new TestCase("dry arabia four worker same resource avoids single worker starvation", DryArabiaFourWorkerSameResourceAvoidsSingleWorkerStarvation),
                new TestCase("berry visual radius matches simulation footprint radius", BerryVisualRadiusMatchesSimulationFootprintRadius),
                new TestCase("thirty workers across resources keep progress or intent", ThirtyWorkersAcrossResourcesKeepProgressOrIntent),
                new TestCase("thirty workers across resources keep progress or intent v2", ThirtyWorkersAcrossResourcesKeepProgressOrIntentV2),
                new TestCase("fifty workers across resources keep progress or intent", FiftyWorkersAcrossResourcesKeepProgressOrIntent),
                new TestCase("fifty workers across resources keep progress or intent v2", FiftyWorkersAcrossResourcesKeepProgressOrIntentV2),
                new TestCase("one hundred twenty workers across resources keep progress or intent", OneHundredTwentyWorkersAcrossResourcesKeepProgressOrIntent),
                new TestCase("one hundred twenty workers across resources keep progress or intent v2", OneHundredTwentyWorkersAcrossResourcesKeepProgressOrIntentV2),
                new TestCase("six player mixed population traffic remains deterministic", SixPlayerMixedPopulationTrafficRemainsDeterministic),
                new TestCase("six player mixed population traffic remains deterministic v2", SixPlayerMixedPopulationTrafficRemainsDeterministicV2),
                new TestCase("path query budget stays bounded under pressure", PathQueryBudgetStaysBoundedUnderPressure),
                new TestCase("repeated command replacement stays bounded", RepeatedCommandReplacementStaysBounded),
                new TestCase("two to five villagers gather deposit crossing routes stay stable", TwoToFiveVillagersGatherDepositCrossingRoutesStayStable),
                new TestCase("two to five villagers gather deposit crossing routes stay stable v2", TwoToFiveVillagersGatherDepositCrossingRoutesStayStableV2),
                new TestCase("left gold blocker villager recovers without endless move to resource", LeftGoldBlockerVillagerRecoversWithoutEndlessMoveToResource),
                new TestCase("left gold blocker villager recovers without endless move to resource v2", LeftGoldBlockerVillagerRecoversWithoutEndlessMoveToResourceV2),
                new TestCase("repeated move replacement near tc hotspot stays stable", RepeatedMoveReplacementNearTcHotspotStaysStable),
                new TestCase("repeated move replacement near tc hotspot stays stable v2", RepeatedMoveReplacementNearTcHotspotStaysStableV2),
                new TestCase("six player seven twenty villager equivalent pressure stays bounded", SixPlayerSevenTwentyVillagerEquivalentPressureStaysBounded),
                new TestCase("six player seven twenty villager equivalent pressure stays bounded v2", SixPlayerSevenTwentyVillagerEquivalentPressureStaysBoundedV2),
                new TestCase("six player twelve hundred active unit pressure stays bounded", SixPlayerTwelveHundredActiveUnitPressureStaysBounded),
                new TestCase("six player twelve hundred active unit pressure stays bounded v2", SixPlayerTwelveHundredActiveUnitPressureStaysBoundedV2),
                new TestCase("pressure window budgets stay bounded", PressureWindowBudgetsStayBounded),
                new TestCase("pressure window budgets stay bounded v2", PressureWindowBudgetsStayBoundedV2),
                new TestCase("two units attempting same tile receive slots", TwoUnitsAttemptingSameTileReceiveSlots),
                new TestCase("three units attempting same tile receive slots", ThreeUnitsAttemptingSameTileReceiveSlots),
                new TestCase("two unit tile swap fails", TwoUnitTileSwapFails),
                new TestCase("worker task tile swap keeps movement intent", WorkerTaskTileSwapKeepsMovementIntent),
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
                new TestCase("presentation snapshot includes visible resources", PresentationSnapshotIncludesVisibleResources),
                new TestCase("presentation snapshot hides depleted resources", PresentationSnapshotHidesDepletedResources),
                new TestCase("presentation snapshot includes building status", PresentationSnapshotIncludesBuildingStatus),
                new TestCase("presentation snapshot includes unit status", PresentationSnapshotIncludesUnitStatus),
                new TestCase("presentation snapshot includes tech status", PresentationSnapshotIncludesTechStatus),
                new TestCase("presentation snapshot does not mutate checksum", PresentationSnapshotDoesNotMutateChecksum),
                new TestCase("simulation does not reference presentation", SimulationDoesNotReferencePresentation),
                new TestCase("simulation source does not reference presentation", SimulationSourceDoesNotReferencePresentation),
                new TestCase("simulation source routes pathfinding through service layer", SimulationSourceRoutesPathfindingThroughServiceLayer),
                new TestCase("simulation source routes reservation writes through traffic service", SimulationSourceRoutesReservationWritesThroughTrafficService),
                new TestCase("deterministic reservation conflicts avoid unordered iteration", DeterministicReservationConflictsAvoidUnorderedIteration),
                new TestCase("movement solver v2 villager flag defaults off", MovementSolverV2VillagerFlagDefaultsOff),
                new TestCase("movement engine v2 flag defaults on", MovementEngineV2FlagDefaultsOn),
                new TestCase("gather engine v2 flag defaults on", GatherEngineV2FlagDefaultsOn),
                new TestCase("movement checksum includes v2 unit state", MovementChecksumIncludesV2UnitState),
                new TestCase("rules checksum includes movement and gather v2 flags", RulesChecksumIncludesMovementAndGatherV2Flags),
                new TestCase("movement solver v2 villager mode remains deterministic", MovementSolverV2VillagerModeRemainsDeterministic),
                new TestCase("movement engine v2 mode remains deterministic", MovementEngineV2ModeRemainsDeterministic),
                new TestCase("movement solver v2 updates corridor memory fields", MovementSolverV2UpdatesCorridorMemoryFields),
                new TestCase("gather engine v2 mode remains deterministic", GatherEngineV2ModeRemainsDeterministic),
                new TestCase("godot bridge does not reference godot api", GodotBridgeDoesNotReferenceGodotApi),
                new TestCase("godot client script does not reference simulation core", GodotClientScriptDoesNotReferenceSimulationCore),
                new TestCase("godot client script does not switch on raw primitive kind", GodotClientScriptDoesNotSwitchOnRawPrimitiveKind),
                new TestCase("visual frame creates ugly prototype primitives", VisualFrameCreatesUglyPrototypePrimitives),
                new TestCase("visual frame uses building footprint size", VisualFrameUsesBuildingFootprintSize),
                new TestCase("visual frame includes type ids", VisualFrameIncludesTypeIds),
                new TestCase("visual frame includes resource primitives", VisualFrameIncludesResourcePrimitives),
                new TestCase("visual frame uses resource profile visual size", VisualFrameUsesResourceProfileVisualSize),
                new TestCase("visual frame includes trade route line", VisualFrameIncludesTradeRouteLine),
                new TestCase("visual frame does not mutate checksum", VisualFrameDoesNotMutateChecksum),
                new TestCase("client intent maps movement command", ClientIntentMapsMovementCommand),
                new TestCase("client intent sorts selected unit ids", ClientIntentSortsSelectedUnitIds),
                new TestCase("client intent maps research command", ClientIntentMapsResearchCommand),
                new TestCase("client intent maps local 1v1 command flow", ClientIntentMapsLocal1v1CommandFlow),
                new TestCase("client command mapping does not mutate checksum", ClientCommandMappingDoesNotMutateChecksum),
                new TestCase("local play session advances with automatic noops", LocalPlaySessionAdvancesWithAutomaticNoOps),
                new TestCase("local play session queues without mutating before tick", LocalPlaySessionQueuesWithoutMutatingBeforeTick),
                new TestCase("local play session completes 1v1 capitals", LocalPlaySessionCompletes1v1Capitals),
                new TestCase("local play session exposes visual frame", LocalPlaySessionExposesVisualFrame),
                new TestCase("local play session rejects invalid intent through sim", LocalPlaySessionRejectsInvalidIntentThroughSim),
                new TestCase("local play session creates 6 player ffa", LocalPlaySessionCreates6PlayerFfa),
                new TestCase("local play session creates dry arabia test map", LocalPlaySessionCreatesDryArabiaTestMap),
                new TestCase("local play session creates combat test map", LocalPlaySessionCreatesCombatTestMap),
                new TestCase("combat test scenario supports attack intent through facade", CombatTestScenarioSupportsAttackIntentThroughFacade),
                new TestCase("combat test scenario supports enemy building attack through facade", CombatTestScenarioSupportsEnemyBuildingAttackThroughFacade),
                new TestCase("godot facade returns drawable frame dto", GodotFacadeReturnsDrawableFrameDto),
                new TestCase("godot facade drives local capital flow", GodotFacadeDrivesLocalCapitalFlow),
                new TestCase("godot facade creates local 6 player ffa", GodotFacadeCreatesLocal6PlayerFfa),
                new TestCase("godot facade rejects invalid commands through sim", GodotFacadeRejectsInvalidCommandsThroughSim),
                new TestCase("godot facade exposes last command rejection metadata", GodotFacadeExposesLastCommandRejectionMetadata),
                new TestCase("godot facade exposes fixed raw coordinates", GodotFacadeExposesFixedRawCoordinates),
                new TestCase("godot facade exposes primitive type ids", GodotFacadeExposesPrimitiveTypeIds),
                new TestCase("godot facade exposes building status dto", GodotFacadeExposesBuildingStatusDto),
                new TestCase("godot facade exposes unit status dto", GodotFacadeExposesUnitStatusDto),
                new TestCase("godot facade exposes resource primitive dto", GodotFacadeExposesResourcePrimitiveDto),
                new TestCase("godot facade routes gather command", GodotFacadeRoutesGatherCommand),
                new TestCase("godot interaction router picks nearest overlapping resource", GodotInteractionRouterPicksNearestOverlappingResource),
                new TestCase("dry arabia resource dto exposes stable id type tile", DryArabiaResourceDtoExposesStableIdTypeTile),
                new TestCase("godot facade routes training command", GodotFacadeRoutesTrainingCommand),
                new TestCase("godot facade routes attack command", GodotFacadeRoutesAttackCommand),
                new TestCase("godot facade routes wall command", GodotFacadeRoutesWallCommand),
                new TestCase("godot facade routes trade post command", GodotFacadeRoutesTradePostCommand),
                new TestCase("godot facade routes trade cart training command", GodotFacadeRoutesTradeCartTrainingCommand),
                new TestCase("godot facade routes research command", GodotFacadeRoutesResearchCommand),
                new TestCase("godot interaction router prioritizes attack", GodotInteractionRouterPrioritizesAttack),
                new TestCase("godot interaction router routes build assignment", GodotInteractionRouterRoutesBuildAssignment),
                new TestCase("godot interaction router routes build assignment with expanded bounds", GodotInteractionRouterRoutesBuildAssignmentWithExpandedBounds),
                new TestCase("godot interaction router ignores completed build target", GodotInteractionRouterIgnoresCompletedBuildTarget),
                new TestCase("godot interaction router routes resources", GodotInteractionRouterRoutesResources),
                new TestCase("godot interaction router routes resource over friendly completed building", GodotInteractionRouterRoutesResourceOverFriendlyCompletedBuilding),
                new TestCase("godot interaction router routes move fallback", GodotInteractionRouterRoutesMoveFallback),
                new TestCase("godot interaction router ignores friendly target", GodotInteractionRouterIgnoresFriendlyTarget),
                new TestCase("godot selection router prioritizes local unit", GodotSelectionRouterPrioritizesLocalUnit),
                new TestCase("godot selection router selects local building", GodotSelectionRouterSelectsLocalBuilding),
                new TestCase("godot selection router ignores enemy primitive", GodotSelectionRouterIgnoresEnemyPrimitive),
                new TestCase("godot selection router rectangle selects owned units in id order", GodotSelectionRouterRectangleSelectsOwnedUnitsInIdOrder),
                new TestCase("godot selection router rectangle normalizes corners", GodotSelectionRouterRectangleNormalizesCorners),
                new TestCase("godot selection router returns none", GodotSelectionRouterReturnsNone),
                new TestCase("godot selection edit normal click replaces selection", GodotSelectionEditNormalClickReplacesSelection),
                new TestCase("godot selection edit shift click adds owned unit", GodotSelectionEditShiftClickAddsOwnedUnit),
                new TestCase("godot selection edit shift click removes selected unit", GodotSelectionEditShiftClickRemovesSelectedUnit),
                new TestCase("godot selection edit shift click none keeps selection", GodotSelectionEditShiftClickNoneKeepsSelection),
                new TestCase("control group recall works after shift selection edit", ControlGroupRecallWorksAfterShiftSelectionEdit),
                new TestCase("godot hud text includes economy and selection", GodotHudTextIncludesEconomyAndSelection),
                new TestCase("godot hud text build lines returns two lines", GodotHudTextBuildLinesReturnsTwoLines),
                new TestCase("godot hud text includes unit gather status", GodotHudTextIncludesUnitGatherStatus),
                new TestCase("godot hud text includes selected unit type and phase", GodotHudTextIncludesSelectedUnitTypeAndPhase),
                new TestCase("godot hud text includes combat attack target and cooldown", GodotHudTextIncludesCombatAttackTargetAndCooldown),
                new TestCase("godot hud text includes attack move destination", GodotHudTextIncludesAttackMoveDestination),
                new TestCase("godot hud text includes building training status", GodotHudTextIncludesBuildingTrainingStatus),
                new TestCase("godot hud text includes building type label", GodotHudTextIncludesBuildingTypeLabel),
                new TestCase("godot hud text includes research action ready", GodotHudTextIncludesResearchActionReady),
                new TestCase("godot hud text includes research action queued", GodotHudTextIncludesResearchActionQueued),
                new TestCase("godot hud text includes research action done", GodotHudTextIncludesResearchActionDone),
                new TestCase("godot hud text includes research action blocked build", GodotHudTextIncludesResearchActionBlockedBuild),
                new TestCase("godot hud text includes research action cost", GodotHudTextIncludesResearchActionCost),
                new TestCase("godot hud text includes train action status for town center", GodotHudTextIncludesTrainActionStatusForTownCenter),
                new TestCase("godot hud text includes train action status for trade post", GodotHudTextIncludesTrainActionStatusForTradePost),
                new TestCase("godot hud text includes research status", GodotHudTextIncludesResearchStatus),
                new TestCase("godot hud text includes queued research count", GodotHudTextIncludesQueuedResearchCount),
                new TestCase("godot hud text includes modifier status", GodotHudTextIncludesModifierStatus),
                new TestCase("godot hud text includes modifier count", GodotHudTextIncludesModifierCount),
                new TestCase("godot hud text includes completed tech label", GodotHudTextIncludesCompletedTechLabel),
                new TestCase("godot hud text includes completed tech count", GodotHudTextIncludesCompletedTechCount),
                new TestCase("godot hud text includes rejected command count", GodotHudTextIncludesRejectedCommandCount),
                new TestCase("godot hud text includes last command status", GodotHudTextIncludesLastCommandStatus),
                new TestCase("godot hud text includes last command reason label", GodotHudTextIncludesLastCommandReasonLabel),
                new TestCase("godot hud text includes readable non combat attack rejection hint", GodotHudTextIncludesReadableNonCombatAttackRejectionHint),
                new TestCase("godot hud text includes readable non combat attack move rejection hint", GodotHudTextIncludesReadableNonCombatAttackMoveRejectionHint),
                new TestCase("godot hud text handles missing status", GodotHudTextHandlesMissingStatus),
                new TestCase("godot selected status hides idle no progress ticks", GodotSelectedStatusHidesIdleNoProgressTicks),
                new TestCase("godot selected status shows blocked waiting no progress ticks", GodotSelectedStatusShowsBlockedWaitingNoProgressTicks),
                new TestCase("godot selected status includes attack move debug fields", GodotSelectedStatusIncludesAttackMoveDebugFields),
                new TestCase("godot primitive hit test includes boundary", GodotPrimitiveHitTestIncludesBoundary),
                new TestCase("godot building hit test includes footprint boundary", GodotBuildingHitTestIncludesFootprintBoundary),
                new TestCase("godot primitive hit test supports rectangular bounds", GodotPrimitiveHitTestSupportsRectangularBounds),
                new TestCase("godot primitive hit test rejects outside", GodotPrimitiveHitTestRejectsOutside),
                new TestCase("godot primitive interaction hit test expands building bounds", GodotPrimitiveInteractionHitTestExpandsBuildingBounds),
                new TestCase("godot visual style resolves local unit types", GodotVisualStyleResolvesLocalUnitTypes),
                new TestCase("godot visual style resolves enemy unit", GodotVisualStyleResolvesEnemyUnit),
                new TestCase("godot visual style resolves building types", GodotVisualStyleResolvesBuildingTypes),
                new TestCase("godot visual style resolves resources", GodotVisualStyleResolvesResources),
                new TestCase("godot tech label resolver resolves known tech", GodotTechLabelResolverResolvesKnownTech),
                new TestCase("godot tech label resolver falls back for unknown tech", GodotTechLabelResolverFallsBackForUnknownTech),
                new TestCase("godot tech label resolver resolves known modifier", GodotTechLabelResolverResolvesKnownModifier),
                new TestCase("godot tech label resolver falls back for unknown modifier", GodotTechLabelResolverFallsBackForUnknownModifier),
                new TestCase("godot research action evaluator returns ready", GodotResearchActionEvaluatorReturnsReady),
                new TestCase("godot research action evaluator returns queued", GodotResearchActionEvaluatorReturnsQueued),
                new TestCase("godot research action evaluator returns done", GodotResearchActionEvaluatorReturnsDone),
                new TestCase("godot research action evaluator returns missing resources", GodotResearchActionEvaluatorReturnsMissingResources),
                new TestCase("godot research action evaluator returns none", GodotResearchActionEvaluatorReturnsNone),
                new TestCase("godot research action evaluator returns not applicable", GodotResearchActionEvaluatorReturnsNotApplicable),
                new TestCase("godot research action evaluator returns blocked construction", GodotResearchActionEvaluatorReturnsBlockedConstruction),
                new TestCase("godot train action evaluator returns ready", GodotTrainActionEvaluatorReturnsReady),
                new TestCase("godot train action evaluator returns missing resources", GodotTrainActionEvaluatorReturnsMissingResources),
                new TestCase("godot train action evaluator returns population capped", GodotTrainActionEvaluatorReturnsPopulationCapped),
                new TestCase("godot train action evaluator returns not applicable", GodotTrainActionEvaluatorReturnsNotApplicable),
                new TestCase("godot train action evaluator returns blocked construction", GodotTrainActionEvaluatorReturnsBlockedConstruction),
                new TestCase("godot primitive draw kind resolves known primitives", GodotPrimitiveDrawKindResolvesKnownPrimitives),
                new TestCase("godot primitive draw kind returns none for unknown", GodotPrimitiveDrawKindReturnsNoneForUnknown),
                new TestCase("godot debug event log keeps bounded messages", GodotDebugEventLogKeepsBoundedMessages),
                new TestCase("godot command result classifier classifies counter deltas", GodotCommandResultClassifierClassifiesCounterDeltas),
                new TestCase("godot hotkey help contains known bindings", GodotHotkeyHelpContainsKnownBindings),
                new TestCase("control group state assign recall stores sorted ids", ControlGroupStateAssignRecallStoresSortedIds),
                new TestCase("control group resolver filters missing and non local units", ControlGroupResolverFiltersMissingAndNonLocalUnits),
                new TestCase("godot scenario view hints provides combat camera start", GodotScenarioViewHintsProvidesCombatCameraStart),
                new TestCase("godot scenario view hints ignores unknown map", GodotScenarioViewHintsIgnoresUnknownMap),
                new TestCase("tc placement preview valid at player 0 tc zone", TcPlacementPreviewValidAtPlayer0TcZone),
                new TestCase("tc placement preview valid at player 1 tc zone", TcPlacementPreviewValidAtPlayer1TcZone),
                new TestCase("tc placement preview invalid overlapping resource", TcPlacementPreviewInvalidOverlappingResource),
                new TestCase("tc placement preview invalid overlapping building", TcPlacementPreviewInvalidOverlappingBuilding),
                new TestCase("tc placement preview invalid outside map", TcPlacementPreviewInvalidOutsideMap),
                new TestCase("tc placement preview invalid missing resources", TcPlacementPreviewInvalidMissingResources),
                new TestCase("tc placement preview invalid blocked player state", TcPlacementPreviewInvalidBlockedPlayerState),
                new TestCase("tc placement preview command parity around player 0 zone", TcPlacementPreviewCommandParityAroundPlayer0Zone),
                new TestCase("tc placement preview does not mutate checksum", TcPlacementPreviewDoesNotMutateChecksum),
                new TestCase("tc placement preview unknown result does not throw", TcPlacementPreviewUnknownResultDoesNotThrow),
                new TestCase("godot hotkey help contains edge pan entry", GodotHotkeyHelpContainsEdgePanEntry),
                new TestCase("godot building debug status includes training queue count", GodotBuildingDebugStatusIncludesTrainingQueueCount),
                new TestCase("godot building debug status includes training progress", GodotBuildingDebugStatusIncludesTrainingProgress),
                new TestCase("godot building debug status marks incomplete training unavailable", GodotBuildingDebugStatusMarksIncompleteTrainingUnavailable),
                new TestCase("godot train intent text includes unit and building", GodotTrainIntentTextIncludesUnitAndBuilding),
                new TestCase("simulation source does not reference debug overlay helpers", SimulationSourceDoesNotReferenceDebugOverlayHelpers),
                new TestCase("godot sprite sheet layout resolves expected frame rect", GodotSpriteSheetLayoutResolvesExpectedFrameRect),
                new TestCase("godot sprite sheet layout uses deterministic default frame", GodotSpriteSheetLayoutUsesDeterministicDefaultFrame),
                new TestCase("godot sprite sheet layout returns false for unknown unit type", GodotSpriteSheetLayoutReturnsFalseForUnknownUnitType),
                new TestCase("godot sprite sheet layout includes placeholder slots", GodotSpriteSheetLayoutIncludesPlaceholderSlots),
                new TestCase("godot sprite sheet layout filenames are correct", GodotSpriteSheetLayoutFilenamesAreCorrect),
                new TestCase("godot coordinate mapper converts raw to pixels", GodotCoordinateMapperConvertsRawToPixels),
                new TestCase("godot coordinate mapper converts screen to raw", GodotCoordinateMapperConvertsScreenToRaw),
                new TestCase("godot coordinate mapper floors screen tile", GodotCoordinateMapperFloorsScreenTile),
                new TestCase("godot trade route router finds local trade post", GodotTradeRouteRouterFindsLocalTradePost),
                new TestCase("godot trade route router ignores invalid trade post targets", GodotTradeRouteRouterIgnoresInvalidTradePostTargets),
                new TestCase("godot trade route router finds selected trade cart", GodotTradeRouteRouterFindsSelectedTradeCart),
                new TestCase("godot trade route router ignores non cart selection", GodotTradeRouteRouterIgnoresNonCartSelection),
                new TestCase("train infantry completes", TrainInfantryCompletes),
                new TestCase("train cavalry completes", TrainCavalryCompletes),
                new TestCase("cavalry moves faster than infantry", CavalryMovesFasterThanInfantry),
                new TestCase("cavalry damages enemy unit", CavalryDamagesEnemyUnit),
                new TestCase("cavalry replay determinism", CavalryReplayDeterminism),
                new TestCase("cavalry lockstep", CavalryLockstep),
                new TestCase("research infantry attack completes", ResearchInfantryAttackCompletes),
                new TestCase("research rejects completed tech", ResearchRejectsCompletedTech),
                new TestCase("research infantry attack modifies damage", ResearchInfantryAttackModifiesDamage),
                new TestCase("research checksum covers tech state", ResearchChecksumCoversTechState),
                new TestCase("research replay determinism", ResearchReplayDeterminism),
                new TestCase("research lockstep", ResearchLockstep),
                new TestCase("attack damages enemy unit", AttackDamagesEnemyUnit),
                new TestCase("attack respects cooldown", AttackRespectsCooldown),
                new TestCase("attack rejects friendly target", AttackRejectsFriendlyTarget),
                new TestCase("attack out of range preserves intent", AttackOutOfRangePreservesIntent),
                new TestCase("dead target clears attack intent on next tick", DeadTargetClearsAttackIntentOnNextTick),
                new TestCase("attack rejects non combat unit", AttackRejectsNonCombatUnit),
                new TestCase("attack out of range queues attack slot movement", AttackOutOfRangeQueuesAttackSlotMovement),
                new TestCase("attack group uses distinct attack slots", AttackGroupUsesDistinctAttackSlots),
                new TestCase("attack overflow attackers wait without stacking", AttackOverflowAttackersWaitWithoutStacking),
                new TestCase("attack move accepts combat unit", AttackMoveAcceptsCombatUnit),
                new TestCase("attack move rejects non combat unit", AttackMoveRejectsNonCombatUnit),
                new TestCase("attack move clears explicit attack intent", AttackMoveClearsExplicitAttackIntent),
                new TestCase("move command clears attack move intent", MoveCommandClearsAttackMoveIntent),
                new TestCase("explicit attack clears attack move intent", ExplicitAttackClearsAttackMoveIntent),
                new TestCase("attack move checksum covers intent state", AttackMoveChecksumCoversIntentState),
                new TestCase("attack move replay determinism", AttackMoveReplayDeterminism),
                new TestCase("attack move lockstep", AttackMoveLockstep),
                new TestCase("attack move acquires nearby enemy", AttackMoveAcquiresNearbyEnemy),
                new TestCase("attack move without nearby enemy keeps moving", AttackMoveWithoutNearbyEnemyKeepsMoving),
                new TestCase("attack move acquisition respects cadence", AttackMoveAcquisitionRespectsCadence),
                new TestCase("attack move resumes movement after target dies", AttackMoveResumesMovementAfterTargetDies),
                new TestCase("attack move completes at destination without target", AttackMoveCompletesAtDestinationWithoutTarget),
                new TestCase("attack move resume still respects acquire cadence", AttackMoveResumeStillRespectsAcquireCadence),
                new TestCase("attack move acquisition replay determinism", AttackMoveAcquisitionReplayDeterminism),
                new TestCase("attack move acquisition lockstep", AttackMoveAcquisitionLockstep),
                new TestCase("attack move pressure ten units through enemy group stays stable", AttackMovePressureTenUnitsThroughEnemyGroupStaysStable),
                new TestCase("attack move pressure fifty vs fifty stays stable", AttackMovePressureFiftyVsFiftyStaysStable),
                new TestCase("attack move pressure one hundred fifty vs one hundred fifty stays stable", AttackMovePressureOneHundredFiftyVsOneHundredFiftyStaysStable),
                new TestCase("attack move pressure hotspot three attackers vs defender stays stable", AttackMovePressureHotspotThreeAttackersVsDefenderStaysStable),
                new TestCase("combat pressure ten attackers vs one target", CombatPressureTenAttackersVsOneTarget),
                new TestCase("combat pressure fifty vs fifty melee stays stable", CombatPressureFiftyVsFiftyMeleeStaysStable),
                new TestCase("combat pressure one hundred fifty vs one hundred fifty melee stays stable", CombatPressureOneHundredFiftyVsOneHundredFiftyMeleeStaysStable),
                new TestCase("combat pressure hotspot three attackers vs defender objective stays stable", CombatPressureHotspotThreeAttackersVsDefenderObjectiveStaysStable),
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
                new TestCase("assign build sets adjacent deterministic approach target", AssignBuildSetsAdjacentDeterministicApproachTarget),
                new TestCase("assign build gives distinct approach tiles for multiple villagers", AssignBuildGivesDistinctApproachTilesForMultipleVillagers),
                new TestCase("assign build rejects when no interaction tile is reachable", AssignBuildRejectsWhenNoInteractionTileIsReachable),
                new TestCase("villager paths to tc interaction tile from left", VillagerPathsToTcInteractionTileFromLeft),
                new TestCase("villager paths to tc interaction tile from right", VillagerPathsToTcInteractionTileFromRight),
                new TestCase("worker loop remains unstuck over long dry arabia run", WorkerLoopRemainsUnstuckOverLongDryArabiaRun),
                new TestCase("dry arabia tc build assignment progresses and updates population", DryArabiaTcBuildAssignmentProgressesAndUpdatesPopulation),
                new TestCase("playability invariant five villagers build assignment trace", PlayabilityInvariantFiveVillagersBuildAssignmentTrace),
                new TestCase("playability invariant mixed resource workers shared tc trace", PlayabilityInvariantMixedResourceWorkersSharedTcTrace),
                new TestCase("playability invariant ten workers around tc movement trace", PlayabilityInvariantTenWorkersAroundTcMovementTrace),
                new TestCase("playability invariant trained villagers gather trace", PlayabilityInvariantTrainedVillagersGatherTrace),
                new TestCase("playability invariant depletion continuation pressure trace", PlayabilityInvariantDepletionContinuationPressureTrace),
                new TestCase("simulation scenario harness mixed economy workflow", SimulationScenarioHarnessMixedEconomyWorkflow),
                new TestCase("sim scenario five worker repeated resource switch cycles", SimScenarioFiveWorkerRepeatedResourceSwitchCycles),
                new TestCase("sim scenario five worker move gather hotspot churn", SimScenarioFiveWorkerMoveGatherHotspotChurn),
                new TestCase("sim scenario five worker build gather move gather cycles", SimScenarioFiveWorkerBuildGatherMoveGatherCycles),
                new TestCase("sim scenario five workers food sustained progress", SimScenarioFiveWorkersFoodSustainedProgress),
                new TestCase("sim scenario five workers wood sustained progress", SimScenarioFiveWorkersWoodSustainedProgress),
                new TestCase("sim scenario five workers gold sustained progress", SimScenarioFiveWorkersGoldSustainedProgress),
                new TestCase("sim scenario dry arabia tc front resource flow regression", SimScenarioDryArabiaTcFrontResourceFlowRegression),
                new TestCase("sim matrix resource stall scenarios", SimMatrixResourceStallScenarios),
                new TestCase("sim matrix dropoff congestion scenarios", SimMatrixDropoffCongestionScenarios),
                new TestCase("sim matrix command replacement scenarios", SimMatrixCommandReplacementScenarios),
                new TestCase("sim matrix spawn overlap scenarios", SimMatrixSpawnOverlapScenarios),
                new TestCase("sim matrix pressure one twenty scenarios", SimMatrixPressureOneTwentyScenarios),
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
                new TestCase("chaos v4 stress smoke", ChaosV4StressSmoke),
                new TestCase("chaos v5 stress smoke", ChaosV5StressSmoke)
            };

            int failed = 0;
            int selected = 0;
            string filter = GetFilter(args);
            bool failFast = ShouldFailFast(args);
            if (ShouldShowHelp(args))
            {
                PrintHelp();
                return 0;
            }

            if (ShouldList(args))
            {
                foreach (TestCase test in tests)
                {
                    if (ShouldRun(test, filter))
                    {
                        selected++;
                        Console.WriteLine(test.Name);
                    }
                }

                string listSuffix = filter.Length == 0 ? "" : " filter=\"" + filter + "\"";
                Console.WriteLine("tests=" + selected + " listed=1" + listSuffix);
                return selected == 0 ? 1 : 0;
            }

            foreach (TestCase test in tests)
            {
                if (!ShouldRun(test, filter))
                {
                    continue;
                }

                selected++;
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
                    if (failFast)
                    {
                        break;
                    }
                }
            }

            if (selected == 0)
            {
                Console.WriteLine("tests=0 failed=0 filter=\"" + filter + "\"");
                return 1;
            }

            string suffix = filter.Length == 0 ? "" : " filter=\"" + filter + "\"";
            string failFastSuffix = failFast ? " failFast=1" : "";
            Console.WriteLine("tests=" + selected + " failed=" + failed + suffix + failFastSuffix);
            return failed == 0 ? 0 : 1;
        }


        private static string GetFilter(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--filter" && i + 1 < args.Length)
                {
                    return args[i + 1];
                }

                if (args[i].StartsWith("--filter=", StringComparison.Ordinal))
                {
                    return args[i].Substring("--filter=".Length);
                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    return args[i];
                }
            }

            return "";
        }

        private static bool ShouldList(string[] args)
        {
            return HasFlag(args, "--list");
        }

        private static bool ShouldFailFast(string[] args)
        {
            return HasFlag(args, "--fail-fast");
        }

        private static bool ShouldShowHelp(string[] args)
        {
            return HasFlag(args, "--help") || HasFlag(args, "-h");
        }

        private static bool HasFlag(string[] args, string flag)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == flag)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("RTS test runner");
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project tests\\RtsGame.Tests.csproj --no-build");
            Console.WriteLine("  dotnet run --project tests\\RtsGame.Tests.csproj --no-build -- --filter godot");
            Console.WriteLine("  dotnet run --project tests\\RtsGame.Tests.csproj --no-build -- --list");
            Console.WriteLine("  dotnet run --project tests\\RtsGame.Tests.csproj --no-build -- --fail-fast");
            Console.WriteLine("Options:");
            Console.WriteLine("  --filter <text>    Run tests whose names contain text.");
            Console.WriteLine("  --filter=<text>    Run tests whose names contain text.");
            Console.WriteLine("  --list             List selected test names without running them.");
            Console.WriteLine("  --fail-fast        Stop after the first failed selected test.");
            Console.WriteLine("  --help, -h         Show this help.");
        }

        private static bool ShouldRun(TestCase test, string filter)
        {
            if (filter.Length == 0)
            {
                return true;
            }

            return test.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }





























































        private static void AssertResourceContinuationChoosesAnotherNode(GatherProfileId profileId, ResourceType resourceType, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTwoNodeResourceAreaState(seed, profileId, out int firstNodeId, out int secondNodeId, out int areaId);
            ResourceNode first = FindResourceNodeById(state, firstNodeId);
            Unit worker = state.EntityState.Units[0];
            worker.Position = new FixedVector2(first.Position.X + Fixed.FromInt(1), first.Position.Y);
            first.RemainingAmount = GameData.VillagerGatherPerTick;

            RunGatherCommand(state, rules, firstNodeId, worker.Id);

            AssertEqual(0, first.RemainingAmount, "first " + resourceType + " node should deplete");
            AssertEqual(secondNodeId, worker.CurrentResourceNodeId, "worker should continue to another node in the same area for " + resourceType);
            AssertEqual(areaId, worker.CurrentResourceAreaId, "worker should keep long-term area target for " + resourceType);
            AssertEqual(InteractionReservationKind.ResourceNode, worker.ReservedInteractionKind, "worker should reserve next resource slot for " + resourceType);
            AssertEqual(secondNodeId, worker.ReservedInteractionTargetId, "reservation should target the continuation node for " + resourceType);
        }

        private static void RunGatherCommand(GameState state, GameRules rules, int resourceNodeId, int workerId)
        {
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceNodeId, new[] { workerId })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);
        }




































































































        private static SimScenarioHarness CreateDryArabiaMoveReliabilityHarness(
            ulong seed,
            int minimumVillagers,
            out GameState state,
            out int[] workers,
            out FixedVector2 tcPos,
            out int foodId)
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState setupState = GameInitializer.CreateDryArabiaTest01(seed);
            var scenario = new SimScenarioHarness(setupState, rules, 2, unchecked((uint)(seed % 1000000UL)));
            tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);

            scenario.Step(
                new CommandEnvelope(new CommandHeader(setupState.Tick, 0, unchecked((uint)(seed + 1)), CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)),
                new CommandEnvelope(new CommandHeader(setupState.Tick, 1, unchecked((uint)(seed + 2)), CommandType.NoOp), new NoOpCommand()));

            int tcId = FindUnderConstructionBuildingId(setupState, 0, BuildingTypeId.TownCenter);
            int[] builders = GetPlayerVillagerIds(setupState, 0);
            scenario.Step(
                new CommandEnvelope(new CommandHeader(setupState.Tick, 0, unchecked((uint)(seed + 3)), CommandType.AssignBuild), new AssignBuildCommand(tcId, builders)),
                new CommandEnvelope(new CommandHeader(setupState.Tick, 1, unchecked((uint)(seed + 4)), CommandType.NoOp), new NoOpCommand()));
            scenario.RunTicks(320, () => !setupState.EntityState.EntityLookup.ContainsKey(tcId) || !setupState.EntityState.Buildings[setupState.EntityState.EntityLookup[tcId].Index].IsUnderConstruction);
            AssertEqual(true, FindCompletedBuildingId(setupState, 0, BuildingTypeId.TownCenter) != 0, scenario.Fail("dry arabia reliability setup should complete tc"));

            while (GetPlayerVillagerIds(setupState, 0).Length < minimumVillagers)
            {
                int index = setupState.EntityState.Units.Count;
                EntityFactory.CreateUnit(
                    setupState,
                    0,
                    UnitTypeId.Villager,
                    FixedVector2.FromInts(tcPos.X.FloorToInt() - 4 + (index % 5), tcPos.Y.FloorToInt() + 6 + (index % 2)));
            }

            workers = GetPlayerVillagerIds(setupState, 0);
            Array.Sort(workers);
            foodId = FindNearbyResourceNodeId(setupState, tcPos, ResourceType.Food);
            AssertEqual(true, foodId != 0, scenario.Fail("dry arabia reliability setup should find nearby food"));
            state = setupState;
            return scenario;
        }

        private static int[] TakeSortedUnits(int[] unitIds, int count)
        {
            var copy = new int[unitIds.Length];
            Array.Copy(unitIds, copy, unitIds.Length);
            Array.Sort(copy);
            var selected = new int[count];
            for (int i = 0; i < count; i++)
            {
                selected[i] = copy[i];
            }

            return selected;
        }

        private static void IssueGroupMove(SimScenarioHarness scenario, int playerIndex, int[] unitIds, FixedVector2 target, uint sequence)
        {
            var command = new MoveUnitsCommand(unitIds, target);
            var header = new CommandHeader(scenario.State.Tick, playerIndex, sequence, CommandType.MoveUnits);
            AssertEqual(CommandValidationReason.Accepted, command.GetValidationReason(scenario.State, scenario.Rules, header), scenario.Fail("legal group move should validate before execution"));
            scenario.Step(
                new CommandEnvelope(header, command),
                new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1 - playerIndex, sequence + 1, CommandType.NoOp), new NoOpCommand()));

            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(scenario.State, unitIds[i]);
                AssertEqual(0, unit.CurrentResourceNodeId, scenario.Fail("group move should clear resource node intent"));
                AssertEqual(false, unit.ReservedInteractionKind == InteractionReservationKind.ResourceNode, scenario.Fail("group move should clear resource slot reservation"));
                if (unit.HasMoveTarget)
                {
                    AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, scenario.Fail("moving unit should reserve a move destination"));
                }
            }
        }

        private static void DriveGroupMoveReliabilityTicks(SimScenarioHarness scenario, int[] unitIds, int ticks, int minimumMovedUnits, string label)
        {
            var moved = new bool[unitIds.Length];
            var startX = new long[unitIds.Length];
            var startY = new long[unitIds.Length];
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(scenario.State, unitIds[i]);
                startX[i] = unit.Position.X.Raw;
                startY[i] = unit.Position.Y.Raw;
            }

            for (int tick = 0; tick < ticks; tick++)
            {
                scenario.StepNoOps();
                scenario.CaptureWorkerTrace(unitIds, 120);
                scenario.AssertCoreInvariants(label);
                AssertNoEndlessWorkerPhase(scenario.State, unitIds, WorkerTaskPhase.MovingToCommandMove, 900, scenario.Fail(label + " workers stuck moving-to-command-move"));

                for (int i = 0; i < unitIds.Length; i++)
                {
                    Unit unit = FindUnitById(scenario.State, unitIds[i]);
                    if (unit.Position.X.Raw != startX[i] || unit.Position.Y.Raw != startY[i])
                    {
                        moved[i] = true;
                    }

                    if (!unit.HasMoveTarget && unit.TaskPhase == WorkerTaskPhase.Idle)
                    {
                        AssertEqual(false, unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination, scenario.Fail(label + " idle unit should not keep move destination reservation"));
                    }

                    if (unit.HasMoveTarget && unit.TaskPhase == WorkerTaskPhase.MovingToCommandMove)
                    {
                        AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, scenario.Fail(label + " active command mover should keep move destination reservation"));
                    }
                }
            }

            int movedCount = 0;
            for (int i = 0; i < moved.Length; i++)
            {
                if (moved[i])
                {
                    movedCount++;
                }
            }

            AssertEqual(true, movedCount >= minimumMovedUnits, scenario.Fail(label + " expected bounded movement progress moved=" + movedCount + " required=" + minimumMovedUnits));
        }


















        private static void RunTwoToFiveVillagersGatherDepositCrossingRoutesStayStable(bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }

            GameState state = CreateOccupancyState(3051, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(24, 24));

            int berryAreaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(30, 26));
            int goldAreaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(19, 31));
            int woodAreaId = AddTestResourceArea(state, GatherProfileId.Tree, FixedVector2.FromInts(32, 20));
            int berryNodeId = AddTestResourceNodeToArea(state, berryAreaId, GatherProfileId.BerryBush, FixedVector2.FromInts(30, 26), 1200);
            int goldNodeId = AddTestResourceNodeToArea(state, goldAreaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(19, 31), 1200);
            int woodNodeId = AddTestResourceNodeToArea(state, woodAreaId, GatherProfileId.Tree, FixedVector2.FromInts(32, 20), 1200);

            int[] workers = CreateGridOfVillagers(state, 5, 27, 29, 3);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(berryNodeId, new[] { workers[0], workers[1] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.GatherResource), new GatherResourceCommand(goldNodeId, new[] { workers[2], workers[3] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 2, CommandType.GatherResource), new GatherResourceCommand(woodNodeId, new[] { workers[4] })));

            var runner = new TickRunner();
            var traces = new Queue<string>();
            var reservationChurn = CreateGatherReservationChurnTracker(workers);
            int initialFood = state.PlayerStates.Players[0].Resources.Food;
            int initialWood = state.PlayerStates.Players[0].Resources.Wood;
            int initialGold = state.PlayerStates.Players[0].Resources.Gold;
            for (int tick = 0; tick < 520; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, workers, traces, 80);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("crossing routes workers should not stack", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("crossing routes should keep unique reservations", traces));
                AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToResourceSlot, 260, BuildTraceFailureMessage("crossing routes should not stall moving-to-resource", traces));
                AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToDropoffSlot, 140, BuildTraceFailureMessage("crossing routes should not stall moving-to-dropoff", traces));
                AssertBoundedGatherReservationChurn(state, workers, reservationChurn, BuildTraceFailureMessage("crossing routes reservation churn should stay bounded", traces));
            }

            bool progressed = state.PlayerStates.Players[0].Resources.Food > initialFood
                || state.PlayerStates.Players[0].Resources.Wood > initialWood
                || state.PlayerStates.Players[0].Resources.Gold > initialGold;
            AssertEqual(true, progressed, BuildTraceFailureMessage("crossing routes workers should complete gather/deposit progress", traces));
        }



        private static void RunLeftGoldBlockerVillagerRecoversWithoutEndlessMoveToResource(bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }

            GameState state = CreateOccupancyState(3052, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(24, 24));
            int goldAreaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(17, 24));
            int leftGoldNodeId = AddTestResourceNodeToArea(state, goldAreaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(17, 24), 800);
            int[] workers = CreateGridOfVillagers(state, 1, 25, 24, 1);

            // Temporary local blocker near corridor that later clears.
            int blockerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(22, 24));

            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(leftGoldNodeId, workers)));
            buffer.Add(new CommandEnvelope(new CommandHeader(80, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { blockerId }, FixedVector2.FromInts(28, 24))));
            var runner = new TickRunner();
            int initialGold = state.PlayerStates.Players[0].Resources.Gold;
            for (int tick = 0; tick < 420; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "left gold blocker scenario should not stack");
                AssertNoDuplicateFinalPurposeReservations(state, "left gold blocker scenario should not duplicate reservations");
            }

            Unit worker = FindUnitById(state, workers[0]);
            bool progressed = state.PlayerStates.Players[0].Resources.Gold > initialGold;
            bool keptIntent = worker.CurrentResourceNodeId == leftGoldNodeId
                && worker.CurrentResourceAreaId == goldAreaId;
            AssertEqual(true, progressed || keptIntent, "left gold worker should keep intent or deposit after temporary corridor blockage");
        }



        private static void RunRepeatedMoveReplacementNearTcHotspotStaysStable(bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }

            GameState state = CreateOccupancyState(3053, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(24, 24));
            int[] units = CreateGridOfVillagers(state, 4, 24, 28, 2);
            var buffer = new CommandBuffer();
            for (int tick = 0; tick < 120; tick++)
            {
                int tx = tick % 2 == 0 ? 30 : 18;
                int ty = tick % 3 == 0 ? 29 : 21;
                buffer.Add(new CommandEnvelope(new CommandHeader(tick, 0, unchecked((uint)tick), CommandType.MoveUnits), new MoveUnitsCommand(units, FixedVector2.FromInts(tx, ty))));
            }

            var runner = new TickRunner();
            for (int tick = 0; tick < 220; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "repeated hotspot move replacement should not stack");
                AssertNoDuplicateFinalPurposeReservations(state, "repeated hotspot move replacement should not duplicate reservations");
                AssertEqual(true, state.DebugCounters.RejectedCommandCount <= tick + 1, "legal move command rejection spam should stay bounded tick=" + tick);
            }
        }



        private static void RunSixPlayerSevenTwentyVillagerEquivalentPressureStaysBounded(bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(6);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }

            GameState state = CreateOccupancyState(3054, 6);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(12, 12));
            AddCompletedTownCenter(state, 1, FixedVector2.FromInts(36, 12));
            AddCompletedTownCenter(state, 2, FixedVector2.FromInts(60, 12));
            AddCompletedTownCenter(state, 3, FixedVector2.FromInts(12, 36));
            AddCompletedTownCenter(state, 4, FixedVector2.FromInts(36, 36));
            AddCompletedTownCenter(state, 5, FixedVector2.FromInts(60, 36));

            int areaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(36, 24));
            int nodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(36, 24), 100000);

            var allWorkers = new List<int>(720);
            for (int player = 0; player < 6; player++)
            {
                int baseX = 4 + player * 20;
                int baseY = 4;
                for (int i = 0; i < 120; i++)
                {
                    int id = EntityFactory.CreateUnit(state, player, UnitTypeId.Villager, FixedVector2.FromInts(baseX + (i % 12), baseY + (i / 12)));
                    allWorkers.Add(id);
                }
            }

            var buffer = new CommandBuffer();
            for (int player = 0; player < 6; player++)
            {
                int start = player * 120;
                int[] workerSlice = allWorkers.GetRange(start, 120).ToArray();
                buffer.Add(new CommandEnvelope(new CommandHeader(0, player, unchecked((uint)player), CommandType.GatherResource), new GatherResourceCommand(nodeId, workerSlice)));
            }

            var runner = new TickRunner();
            int maxPathCalls = 0;
            int maxRetargets = 0;
            for (int tick = 0; tick < 90; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int pathCalls = state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls;
                if (pathCalls > maxPathCalls)
                {
                    maxPathCalls = pathCalls;
                }

                if (state.DebugCounters.ReservationRetargetCount > maxRetargets)
                {
                    maxRetargets = state.DebugCounters.ReservationRetargetCount;
                }

                AssertNoLiveUnitStacking(state, "720-worker equivalent pressure should not stack");
                AssertNoDuplicateFinalPurposeReservations(state, "720-worker equivalent pressure should not duplicate reservations");
            }

            AssertEqual(true, maxPathCalls <= GameData.PathQueryBudgetPerTick, "720-worker equivalent path query budget should hold max=" + maxPathCalls);
            AssertEqual(true, maxRetargets <= GameData.ReservationRetargetBudgetPerTick, "720-worker equivalent retarget budget should hold max=" + maxRetargets);
        }



        private static void RunSixPlayerTwelveHundredActiveUnitPressureStaysBounded(bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(6);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }

            GameState state = CreateOccupancyState(30541, 6);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(12, 12));
            AddCompletedTownCenter(state, 1, FixedVector2.FromInts(36, 12));
            AddCompletedTownCenter(state, 2, FixedVector2.FromInts(60, 12));
            AddCompletedTownCenter(state, 3, FixedVector2.FromInts(12, 36));
            AddCompletedTownCenter(state, 4, FixedVector2.FromInts(36, 36));
            AddCompletedTownCenter(state, 5, FixedVector2.FromInts(60, 36));

            int areaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(36, 24));
            int nodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(36, 24), 200000);

            var allWorkers = new List<int>(720);
            var allInfantry = new List<int>(480);
            for (int player = 0; player < 6; player++)
            {
                int workerBaseX = 4 + player * 20;
                int workerBaseY = 4;
                for (int i = 0; i < 120; i++)
                {
                    int id = EntityFactory.CreateUnit(state, player, UnitTypeId.Villager, FixedVector2.FromInts(workerBaseX + (i % 12), workerBaseY + (i / 12)));
                    allWorkers.Add(id);
                }

                int infantryBaseX = 4 + player * 20;
                int infantryBaseY = 52;
                for (int i = 0; i < 80; i++)
                {
                    int id = EntityFactory.CreateUnit(state, player, UnitTypeId.Infantry, FixedVector2.FromInts(infantryBaseX + (i % 10), infantryBaseY + (i / 10)));
                    allInfantry.Add(id);
                }
            }

            var buffer = new CommandBuffer();
            for (int player = 0; player < 6; player++)
            {
                int workerStart = player * 120;
                int[] workerSlice = allWorkers.GetRange(workerStart, 120).ToArray();
                buffer.Add(new CommandEnvelope(new CommandHeader(0, player, unchecked((uint)(6100 + player)), CommandType.GatherResource), new GatherResourceCommand(nodeId, workerSlice)));

                int infantryStart = player * 80;
                int[] movingInfantry = allInfantry.GetRange(infantryStart, 20).ToArray();
                int moveX = player < 3 ? 36 : 40;
                int moveY = player < 3 ? 26 : 22;
                buffer.Add(new CommandEnvelope(new CommandHeader(0, player, unchecked((uint)(6200 + player)), CommandType.MoveUnits), new MoveUnitsCommand(movingInfantry, FixedVector2.FromInts(moveX, moveY))));
            }

            var runner = new TickRunner();
            int maxPathCalls = 0;
            int maxRetargets = 0;
            int initialFood = state.PlayerStates.Players[0].Resources.Food;
            int initialWood = state.PlayerStates.Players[0].Resources.Wood;
            int initialGold = state.PlayerStates.Players[0].Resources.Gold;
            for (int tick = 0; tick < 80; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int pathCalls = state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls;
                if (pathCalls > maxPathCalls)
                {
                    maxPathCalls = pathCalls;
                }

                if (state.DebugCounters.ReservationRetargetCount > maxRetargets)
                {
                    maxRetargets = state.DebugCounters.ReservationRetargetCount;
                }

                AssertNoLiveUnitStacking(state, "1200-active-unit pressure should not stack");
                AssertNoDuplicateFinalPurposeReservations(state, "1200-active-unit pressure should not duplicate final-purpose reservations");
                AssertEqual(true, state.DebugCounters.RejectedCommandCount <= 12, "1200-active-unit legal command rejection spam should stay bounded tick=" + tick + " rej=" + state.DebugCounters.RejectedCommandCount);
            }

            AssertEqual(1200, state.EntityState.Units.Count, "pressure scenario should hold 1200 active units");
            AssertEqual(true, maxPathCalls <= GameData.PathQueryBudgetPerTick, "1200-active-unit path query budget should hold max=" + maxPathCalls);
            AssertEqual(true, maxRetargets <= GameData.ReservationRetargetBudgetPerTick, "1200-active-unit retarget budget should hold max=" + maxRetargets);
            bool gatheredAny = state.PlayerStates.Players[0].Resources.Food > initialFood
                || state.PlayerStates.Players[0].Resources.Wood > initialWood
                || state.PlayerStates.Players[0].Resources.Gold > initialGold;
            int[] playerZeroWorkers = allWorkers.GetRange(0, 120).ToArray();
            AssertEqual(true, gatheredAny || AnyWorkerHasResourceIntent(state, playerZeroWorkers), "1200-active-unit pressure should keep gather progress or worker intent");
        }



        private static void RunPressureWindowBudgetsStayBounded(bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }

            GameState state = CreateOccupancyState(3055, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(22, 22));
            int areaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(29, 22));
            int nodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(29, 22), 10000);
            int[] workers = CreateGridOfVillagers(state, 120, 22, 28, 12);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(nodeId, workers)));

            var runner = new TickRunner();
            int[] pathWindow = new int[GameData.ReservationRetargetBudgetWindowTicks];
            int[] retargetWindow = new int[GameData.ReservationRetargetBudgetWindowTicks];
            int[] rejectWindow = new int[GameData.ReservationRetargetBudgetWindowTicks];
            int rollingPath = 0;
            int rollingRetarget = 0;
            int rollingReject = 0;
            for (int tick = 0; tick < 200; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int slot = tick % GameData.ReservationRetargetBudgetWindowTicks;
                rollingPath -= pathWindow[slot];
                rollingRetarget -= retargetWindow[slot];
                rollingReject -= rejectWindow[slot];
                pathWindow[slot] = state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls;
                retargetWindow[slot] = state.DebugCounters.ReservationRetargetCount;
                rejectWindow[slot] = state.DebugCounters.RejectedCommandCount;
                rollingPath += pathWindow[slot];
                rollingRetarget += retargetWindow[slot];
                rollingReject += rejectWindow[slot];

                AssertEqual(true, rollingPath <= GameData.PathQueryBudgetPerTick * GameData.ReservationRetargetBudgetWindowTicks, "rolling path budget window should stay bounded tick=" + tick + " rolling=" + rollingPath);
                AssertEqual(true, rollingRetarget <= GameData.ReservationRetargetBudgetPerWindow, "rolling retarget budget window should stay bounded tick=" + tick + " rolling=" + rollingRetarget);
                AssertEqual(true, rollingReject <= GameData.LegalCommandRejectBudgetPerWindow, "rolling reject budget window should stay bounded tick=" + tick + " rolling=" + rollingReject);
                AssertNoLiveUnitStacking(state, "window budget pressure should not stack");
                AssertNoDuplicateFinalPurposeReservations(state, "window budget pressure should not duplicate reservations");
            }
        }



        private static void RunSixPlayerMixedPopulationTrafficRemainsDeterministic(bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(6);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }

            GameState first = GameInitializer.CreateDryArabiaTest01(3045);
            GameState second = GameInitializer.CreateDryArabiaTest01(3045);
            var firstBuffer = new CommandBuffer();
            var secondBuffer = new CommandBuffer();

            for (int player = 0; player < 6; player++)
            {
                int[] villagersFirst = GetPlayerVillagerIds(first, player);
                int[] villagersSecond = GetPlayerVillagerIds(second, player);
                AssertEqual(villagersFirst.Length, villagersSecond.Length, "deterministic six-player scenario should initialize equal villager counts for player " + player);
                if (villagersFirst.Length == 0)
                {
                    continue;
                }
                int foodNodeIdFirst = FindNearbyResourceNodeId(first, DryArabiaTest01MapDefinition.GetTownCenterZone(player), ResourceType.Food);
                int woodNodeIdFirst = FindNearbyResourceNodeId(first, DryArabiaTest01MapDefinition.GetTownCenterZone(player), ResourceType.Wood);
                int foodNodeIdSecond = FindNearbyResourceNodeId(second, DryArabiaTest01MapDefinition.GetTownCenterZone(player), ResourceType.Food);
                int woodNodeIdSecond = FindNearbyResourceNodeId(second, DryArabiaTest01MapDefinition.GetTownCenterZone(player), ResourceType.Wood);
                int splitFirst = villagersFirst.Length >= 2 ? villagersFirst.Length / 2 : 1;
                int splitSecond = villagersSecond.Length >= 2 ? villagersSecond.Length / 2 : 1;
                int[] groupAFirst = villagersFirst.Take(splitFirst).ToArray();
                int[] groupBFirst = villagersFirst.Skip(splitFirst).ToArray();
                int[] groupASecond = villagersSecond.Take(splitSecond).ToArray();
                int[] groupBSecond = villagersSecond.Skip(splitSecond).ToArray();

                firstBuffer.Add(new CommandEnvelope(new CommandHeader(0, player, unchecked((uint)(player * 2)), CommandType.GatherResource), new GatherResourceCommand(foodNodeIdFirst, groupAFirst)));
                secondBuffer.Add(new CommandEnvelope(new CommandHeader(0, player, unchecked((uint)(player * 2)), CommandType.GatherResource), new GatherResourceCommand(foodNodeIdSecond, groupASecond)));
                if (groupBFirst.Length > 0 && groupBSecond.Length > 0)
                {
                    firstBuffer.Add(new CommandEnvelope(new CommandHeader(0, player, unchecked((uint)(player * 2 + 1)), CommandType.GatherResource), new GatherResourceCommand(woodNodeIdFirst, groupBFirst)));
                    secondBuffer.Add(new CommandEnvelope(new CommandHeader(0, player, unchecked((uint)(player * 2 + 1)), CommandType.GatherResource), new GatherResourceCommand(woodNodeIdSecond, groupBSecond)));
                }
            }

            var runner = new TickRunner();
            for (int tick = 0; tick < 260; tick++)
            {
                runner.AdvanceOneTick(first, rules, firstBuffer);
                runner.AdvanceOneTick(second, rules, secondBuffer);
                AssertNoLiveUnitStacking(first, "six-player mixed-pop pressure should not stack");
                AssertNoDuplicateFinalPurposeReservations(first, "six-player mixed-pop pressure should avoid duplicate reservations");
                int pathCalls = first.DebugCounters.PathFindNextCalls + first.DebugCounters.PathFindCostCalls;
                AssertEqual(true, pathCalls <= GameData.PathQueryBudgetPerTick, "six-player mixed-pop path queries should stay bounded tick=" + tick + " pathCalls=" + pathCalls);
            }

            AssertEqual(first.LastChecksum, second.LastChecksum, "six-player mixed-pop pressure should remain deterministic");
        }

        private static void RunWorkerPressureScenario(ulong seed, int totalWorkers, int ticks, string label, bool enableV2Villagers)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            if (enableV2Villagers)
            {
                rules = rules.WithMovementSolverV2Villagers(true);
            }
            GameState state = CreateOccupancyState(seed);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            int foodAreaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 15));
            int woodAreaId = AddTestResourceArea(state, GatherProfileId.Tree, FixedVector2.FromInts(30, 15));
            int goldAreaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(21, 31));
            int foodNodeId = AddTestResourceNodeToArea(state, foodAreaId, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 15), 2000);
            int woodNodeId = AddTestResourceNodeToArea(state, woodAreaId, GatherProfileId.Tree, FixedVector2.FromInts(30, 15), 2000);
            int goldNodeId = AddTestResourceNodeToArea(state, goldAreaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(21, 31), 2000);

            int foodCount = totalWorkers / 3;
            int woodCount = totalWorkers / 3;
            int goldCount = totalWorkers - foodCount - woodCount;
            int rowWidth = totalWorkers >= 90 ? 12 : 8;
            int[] foodWorkers = CreateGridOfVillagers(state, foodCount, 8, 8, rowWidth);
            int[] woodWorkers = CreateGridOfVillagers(state, woodCount, 36, 8, rowWidth);
            int[] goldWorkers = CreateGridOfVillagers(state, goldCount, 20, 36, rowWidth);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(foodNodeId, foodWorkers)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 1, CommandType.GatherResource), new GatherResourceCommand(woodNodeId, woodWorkers)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 2, CommandType.GatherResource), new GatherResourceCommand(goldNodeId, goldWorkers)));

            int initialFood = state.PlayerStates.Players[0].Resources.Food;
            int initialWood = state.PlayerStates.Players[0].Resources.Wood;
            int initialGold = state.PlayerStates.Players[0].Resources.Gold;
            var runner = new TickRunner();
            for (int tick = 0; tick < ticks; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, label + " should not stack");
                AssertNoDuplicateFinalPurposeReservations(state, label + " should not duplicate final-purpose reservations");
                int pathCalls = state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls;
                AssertEqual(true, pathCalls <= GameData.PathQueryBudgetPerTick, label + " path query budget should stay bounded tick=" + tick + " pathCalls=" + pathCalls);
            }

            AssertEqual(true, state.PlayerStates.Players[0].Resources.Food > initialFood || AnyWorkerHasResourceIntent(state, foodWorkers), label + " food workers should make progress or keep gather intent");
            AssertEqual(true, state.PlayerStates.Players[0].Resources.Wood > initialWood || AnyWorkerHasResourceIntent(state, woodWorkers), label + " wood workers should make progress or keep gather intent");
            AssertEqual(true, state.PlayerStates.Players[0].Resources.Gold > initialGold || AnyWorkerHasResourceIntent(state, goldWorkers), label + " gold workers should make progress or keep gather intent");
        }






































        private static bool ContainsFieldAssignment(string source, string fieldName)
        {
            string marker = "." + fieldName;
            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int markerIndex = line.IndexOf(marker, System.StringComparison.Ordinal);
                if (markerIndex < 0)
                {
                    continue;
                }

                int equalsIndex = line.IndexOf('=', markerIndex + marker.Length);
                if (equalsIndex < 0)
                {
                    continue;
                }

                bool isComparison = line.Contains("==")
                    || line.Contains("!=")
                    || line.Contains("<=")
                    || line.Contains(">=");
                if (!isComparison)
                {
                    return true;
                }
            }

            return false;
        }























































        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        

        









































        private static void AssertFilename(GodotSpriteAssetId id, string expectedFileName)
        {
            GodotSpriteSheetLayout.TryGetMetadata(id, out var metadata);
            AssertEqual(expectedFileName, metadata.FileName, $"Asset {id} should have filename {expectedFileName}");
        }

        private static bool ContainsHotkey(GodotHotkeyHelpEntry[] entries, string input, string action)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Input == input && entries[i].Action == action)
                {
                    return true;
                }
            }

            return false;
        }









































































        private static void AssertVillagerPathsToTcInteractionTileFromSide(int xOffset, ulong seed)
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(seed);
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            Unit villager = state.EntityState.Units[0];
            villager.Position = FixedVector2.FromInts(tcPos.X.FloorToInt() + xOffset, tcPos.Y.FloorToInt());
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { villager.Id })));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            bool inRange = false;
            for (int i = 0; i < 120; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4000 + i * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(4001 + i * 2));
                runner.AdvanceOneTick(state, rules, buffer);
                Building building = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
                if (SpatialRules.IsUnitInBuildInteractionRange(villager, building))
                {
                    inRange = true;
                    break;
                }
            }

            AssertEqual(true, inRange, "villager should path to valid tc interaction tile from offset=" + xOffset);
        }




















        private static void RunFiveWorkerResourceSwitchScenario(ulong seed)
        {
            RunFiveWorkerScenarioWithSeed(seed, "resource-switch-seed-" + seed, (scenario, workers, nodeIds, tcId) =>
            {
                int[] moveTargetsX = { 31, 18, 26, 20 };
                int[] moveTargetsY = { 30, 24, 34, 19 };
                int[] unitOrder = BuildDeterministicFiveWorkerOrder(workers, (int)seed);
                int[] nodeOrder = BuildDeterministicNodeOrder(nodeIds, (int)seed);
                int[] group = new int[workers.Length];
                bool sawDeposit = false;
                bool sawCarryOrIntent = false;
                int startFood = scenario.State.PlayerStates.Players[0].Resources.Food;
                int startWood = scenario.State.PlayerStates.Players[0].Resources.Wood;
                int startGold = scenario.State.PlayerStates.Players[0].Resources.Gold;

                for (int cycle = 0; cycle < 8; cycle++)
                {
                    FillRotatedWorkerGroup(unitOrder, cycle, group);
                    int gatherNode = nodeOrder[cycle % nodeOrder.Length];
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(91000 + (cycle * 10))), CommandType.GatherResource), new GatherResourceCommand(gatherNode, group)),
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(91001 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));
                    DriveFiveWorkerScenarioTicks(scenario, workers, 70, 900, "resource-switch cycle gather " + cycle);
                    sawDeposit = sawDeposit || DidStockpileChange(scenario.State, 0, startFood, startWood, startGold);

                    int moveX = moveTargetsX[cycle % moveTargetsX.Length];
                    int moveY = moveTargetsY[cycle % moveTargetsY.Length];
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(91002 + (cycle * 10))), CommandType.MoveUnits), new MoveUnitsCommand(group, FixedVector2.FromInts(moveX, moveY))),
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(91003 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));
                    DriveFiveWorkerScenarioTicks(scenario, workers, 24, 900, "resource-switch cycle move " + cycle);

                    if (!sawCarryOrIntent)
                    {
                        for (int i = 0; i < workers.Length; i++)
                        {
                            Unit unit = FindUnitById(scenario.State, workers[i]);
                            if (unit.CarriedAmount > 0
                                || unit.CurrentResourceAreaId != 0
                                || unit.CurrentResourceNodeId != 0
                                || unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot
                                || unit.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot
                                || unit.TaskPhase == WorkerTaskPhase.Gathering)
                            {
                                sawCarryOrIntent = true;
                                break;
                            }
                        }
                    }
                }

                bool finalCarryOrIntent = sawCarryOrIntent || HasAnyActiveWorkerIntent(scenario.State, workers);
                for (int i = 0; i < workers.Length && !finalCarryOrIntent; i++)
                {
                    if (FindUnitById(scenario.State, workers[i]).CarriedAmount > 0)
                    {
                        finalCarryOrIntent = true;
                    }
                }

                AssertEqual(
                    true,
                    sawDeposit || DidStockpileChange(scenario.State, 0, startFood, startWood, startGold) || finalCarryOrIntent,
                    scenario.Fail("resource-switch scenario should make bounded gather progress or retain active worker intent"));
            });
        }

        private static void RunFiveWorkerMoveGatherHotspotScenario(ulong seed)
        {
            RunFiveWorkerScenarioWithSeed(seed, "move-gather-hotspot-seed-" + seed, (scenario, workers, nodeIds, tcId) =>
            {
                int[] moveTargetsX = { 28, 19, 30, 21, 27 };
                int[] moveTargetsY = { 20, 30, 26, 22, 34 };
                int[] unitOrder = BuildDeterministicFiveWorkerOrder(workers, (int)seed);
                int startFood = scenario.State.PlayerStates.Players[0].Resources.Food;
                int startWood = scenario.State.PlayerStates.Players[0].Resources.Wood;
                int startGold = scenario.State.PlayerStates.Players[0].Resources.Gold;
                bool sawActiveResourceIntent = false;

                for (int cycle = 0; cycle < 10; cycle++)
                {
                    int gatherNode = nodeIds[cycle % nodeIds.Length];
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(92000 + (cycle * 10))), CommandType.GatherResource), new GatherResourceCommand(gatherNode, unitOrder)),
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(92001 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));
                    DriveFiveWorkerScenarioTicks(scenario, workers, 45, 900, "hotspot churn gather " + cycle);
                    sawActiveResourceIntent = sawActiveResourceIntent || HasAnyActiveWorkerIntent(scenario.State, workers);

                    int moveX = moveTargetsX[cycle % moveTargetsX.Length];
                    int moveY = moveTargetsY[cycle % moveTargetsY.Length];
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(92002 + (cycle * 10))), CommandType.MoveUnits), new MoveUnitsCommand(unitOrder, FixedVector2.FromInts(moveX, moveY))),
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(92003 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));
                    DriveFiveWorkerScenarioTicks(scenario, workers, 18, 900, "hotspot churn move " + cycle);

                    for (int i = 0; i < workers.Length; i++)
                    {
                        Unit unit = FindUnitById(scenario.State, workers[i]);
                        if (unit.CurrentResourceAreaId != 0 || unit.CurrentResourceNodeId != 0 || unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot)
                        {
                            sawActiveResourceIntent = true;
                            break;
                        }
                    }
                }

                AssertEqual(true, sawActiveResourceIntent || DidStockpileChange(scenario.State, 0, startFood, startWood, startGold), scenario.Fail("hotspot churn scenario should keep resource intent and bounded progress"));
            });
        }

        private static void RunFiveWorkerBuildGatherMoveGatherScenario(ulong seed)
        {
            RunFiveWorkerScenarioWithSeed(seed, "build-gather-move-seed-" + seed, (scenario, workers, nodeIds, tcId) =>
            {
                FixedVector2[] buildSites =
                {
                    FixedVector2.FromInts(30, 26),
                    FixedVector2.FromInts(18, 26)
                };
                int[] moveTargetsX = { 26, 20, 31 };
                int[] moveTargetsY = { 34, 22, 28 };
                int startFood = scenario.State.PlayerStates.Players[0].Resources.Food;
                int startWood = scenario.State.PlayerStates.Players[0].Resources.Wood;
                int startGold = scenario.State.PlayerStates.Players[0].Resources.Gold;
                bool sawBuilderIntent = false;

                for (int cycle = 0; cycle < 6; cycle++)
                {
                    FixedVector2 site = buildSites[cycle % buildSites.Length];
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(93000 + (cycle * 10))), CommandType.PlaceTownCenter), new PlaceTownCenterCommand(site)),
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(93001 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));

                    int foundationId = FindUnderConstructionBuildingIdOrZero(scenario.State, 0, BuildingTypeId.TownCenter);
                    if (foundationId != 0)
                    {
                        sawBuilderIntent = true;
                        scenario.Step(
                            new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(93002 + (cycle * 10))), CommandType.AssignBuild), new AssignBuildCommand(foundationId, workers)),
                            new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(93003 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));
                        DriveFiveWorkerScenarioTicks(scenario, workers, 45, 900, "build-gather cycle build " + cycle);
                    }

                    int gatherNode = nodeIds[(cycle + 1) % nodeIds.Length];
                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(93004 + (cycle * 10))), CommandType.GatherResource), new GatherResourceCommand(gatherNode, workers)),
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(93005 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));
                    DriveFiveWorkerScenarioTicks(scenario, workers, 48, 900, "build-gather cycle gather " + cycle);

                    scenario.Step(
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 0, unchecked((uint)(93006 + (cycle * 10))), CommandType.MoveUnits), new MoveUnitsCommand(workers, FixedVector2.FromInts(moveTargetsX[cycle % moveTargetsX.Length], moveTargetsY[cycle % moveTargetsY.Length]))),
                        new CommandEnvelope(new CommandHeader(scenario.State.Tick, 1, unchecked((uint)(93007 + (cycle * 10))), CommandType.NoOp), new NoOpCommand()));
                    DriveFiveWorkerScenarioTicks(scenario, workers, 16, 900, "build-gather cycle move " + cycle);
                }

                AssertEqual(true, sawBuilderIntent, scenario.Fail("build-gather cycles should preserve legal builder intent at least once"));
                AssertEqual(
                    true,
                    DidStockpileChange(scenario.State, 0, startFood, startWood, startGold) || HasAnyActiveWorkerIntent(scenario.State, workers),
                    scenario.Fail("build-gather cycles should make bounded economy progress or retain active intent"));
            });
        }

        private static void RunFiveWorkersSingleResourceSustainedProgress(
            ulong seed,
            GatherProfileId profileId,
            ResourceType expectedCarryType,
            string label)
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(seed, 2);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(24, 24));
            int[] workers = CreateGridOfVillagers(state, 5, 25, 29, 3);
            int areaId = AddTestResourceArea(state, profileId, FixedVector2.FromInts(29, 47));
            int nodeId = AddTestResourceNodeToArea(state, areaId, profileId, FixedVector2.FromInts(29, 47), 6000);
            int startStock = GetStockpileValue(state, 0, expectedCarryType);
            var scenario = new SimScenarioHarness(state, rules, 2, unchecked((uint)(95000 + (int)(seed % 1000))));

            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 95001, CommandType.GatherResource), new GatherResourceCommand(nodeId, workers)),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 95002, CommandType.NoOp), new NoOpCommand()));

            int productiveWorkers = 0;
            var sawProductive = new Dictionary<int, bool>();
            var maxNoProgressInMoving = new Dictionary<int, int>();
            var reservationChurn = CreateGatherReservationChurnTracker(workers);
            for (int i = 0; i < workers.Length; i++)
            {
                sawProductive[workers[i]] = false;
                maxNoProgressInMoving[workers[i]] = 0;
            }

            for (int tick = 0; tick < 520; tick++)
            {
                scenario.StepNoOps();
                scenario.CaptureWorkerTrace(workers, 120);
                scenario.AssertCoreInvariants(label);
                AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToResourceSlot, 220, scenario.Fail(label + " worker stuck moving to resource"));
                AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToDropoffSlot, 140, scenario.Fail(label + " worker stuck moving to dropoff"));
                AssertBoundedGatherReservationChurn(state, workers, reservationChurn, scenario.Fail(label + " reservation churn should stay bounded"));

                for (int i = 0; i < workers.Length; i++)
                {
                    Unit unit = FindUnitById(state, workers[i]);
                    int noProgress = unit.LastMovedTick < 0 ? 0 : state.Tick - unit.LastMovedTick;
                    if (unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot || unit.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot)
                    {
                        if (noProgress > maxNoProgressInMoving[unit.Id])
                        {
                            maxNoProgressInMoving[unit.Id] = noProgress;
                        }
                    }

                    if (!sawProductive[unit.Id]
                        && (unit.TaskPhase == WorkerTaskPhase.Gathering
                            || (unit.CarriedAmount > 0 && unit.CarriedResourceType == expectedCarryType)))
                    {
                        sawProductive[unit.Id] = true;
                    }
                }
            }

            foreach (KeyValuePair<int, bool> pair in sawProductive)
            {
                if (pair.Value)
                {
                    productiveWorkers++;
                }
            }

            int endStock = GetStockpileValue(state, 0, expectedCarryType);
            AssertEqual(true, endStock > startStock, scenario.Fail(label + " stockpile should increase"));
            AssertEqual(true, productiveWorkers >= 3, scenario.Fail(label + " at least three of five workers should become productive in bounded window; productive=" + productiveWorkers));

            foreach (KeyValuePair<int, int> pair in maxNoProgressInMoving)
            {
                AssertEqual(true, pair.Value <= 220, scenario.Fail(label + " worker exceeded no-progress ceiling unit=" + pair.Key + " noProgress=" + pair.Value));
            }
        }

        private static void RunFiveWorkerScenarioWithSeed(
            ulong seed,
            string scenarioLabel,
            Action<SimScenarioHarness, int[], int[], int> run)
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(seed, 2);
            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(24, 24));
            state.PlayerStates.Players[0].Resources.Wood = 50000;
            state.PlayerStates.Players[0].Resources.Gold = 50000;
            int[] workers = CreateGridOfVillagers(state, 5, 25, 29, 3);

            int berryAreaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(29, 49));
            int goldAreaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(21, 57));
            int woodAreaId = AddTestResourceArea(state, GatherProfileId.Tree, FixedVector2.FromInts(23, 56));
            int berryNodeId = AddTestResourceNodeToArea(state, berryAreaId, GatherProfileId.BerryBush, FixedVector2.FromInts(29, 49), 5000);
            int goldNodeId = AddTestResourceNodeToArea(state, goldAreaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(21, 57), 5000);
            int woodNodeId = AddTestResourceNodeToArea(state, woodAreaId, GatherProfileId.Tree, FixedVector2.FromInts(23, 56), 5000);

            var scenario = new SimScenarioHarness(state, rules, 2, unchecked((uint)(94000 + (int)(seed % 1000))));
            run(scenario, workers, new[] { berryNodeId, goldNodeId, woodNodeId }, tcId);

            AssertNoLiveUnitStacking(state, scenario.Fail(scenarioLabel + " final stacking check"));
            AssertNoDuplicateFinalPurposeReservations(state, scenario.Fail(scenarioLabel + " final reservation check"));
            for (int i = 0; i < workers.Length; i++)
            {
                Unit unit = FindUnitById(state, workers[i]);
                bool idleHasStaleMoveReservation = unit.TaskPhase == WorkerTaskPhase.Idle
                    && !unit.HasMoveTarget
                    && unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination;
                AssertEqual(false, idleHasStaleMoveReservation, scenario.Fail(scenarioLabel + " idle worker should not keep stale move destination reservation unit=" + unit.Id));
            }
        }

        private static void DriveFiveWorkerScenarioTicks(SimScenarioHarness scenario, int[] workers, int ticks, int maxNoProgressTicks, string phaseLabel)
        {
            int startRejected = scenario.State.DebugCounters.RejectedCommandCount;

            for (int tick = 0; tick < ticks; tick++)
            {
                scenario.StepNoOps();
                scenario.CaptureWorkerTrace(workers, 120);
                scenario.AssertCoreInvariants(phaseLabel);
                AssertNoEndlessWorkerPhase(scenario.State, workers, WorkerTaskPhase.MovingToResourceSlot, maxNoProgressTicks, scenario.Fail(phaseLabel + " workers stuck moving-to-resource"));
                AssertNoEndlessWorkerPhase(scenario.State, workers, WorkerTaskPhase.MovingToDropoffSlot, maxNoProgressTicks, scenario.Fail(phaseLabel + " workers stuck moving-to-dropoff"));
                AssertNoEndlessWorkerPhase(scenario.State, workers, WorkerTaskPhase.MovingToCommandMove, maxNoProgressTicks, scenario.Fail(phaseLabel + " workers stuck moving-to-command-move"));
                AssertEqual(true, scenario.State.DebugCounters.ReservationRetargetCount <= GameData.ReservationRetargetBudgetPerTick, scenario.Fail(phaseLabel + " reservation retarget churn should stay under per-tick budget"));
            }

            int rejectedDelta = scenario.State.DebugCounters.RejectedCommandCount - startRejected;
            AssertEqual(true, rejectedDelta <= 6, scenario.Fail(phaseLabel + " should not spam legal command rejections delta=" + rejectedDelta));
        }

        private static bool HasAnyActiveWorkerIntent(GameState state, int[] workers)
        {
            for (int i = 0; i < workers.Length; i++)
            {
                Unit unit = FindUnitById(state, workers[i]);
                if (unit.CurrentBuildTargetId != 0
                    || unit.CurrentResourceAreaId != 0
                    || unit.CurrentResourceNodeId != 0
                    || unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot
                    || unit.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot
                    || unit.TaskPhase == WorkerTaskPhase.MovingToCommandMove
                    || unit.TaskPhase == WorkerTaskPhase.Building
                    || unit.TaskPhase == WorkerTaskPhase.Gathering
                    || unit.CarriedAmount > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DidStockpileChange(GameState state, int playerIndex, int startFood, int startWood, int startGold)
        {
            return state.PlayerStates.Players[playerIndex].Resources.Food > startFood
                || state.PlayerStates.Players[playerIndex].Resources.Wood > startWood
                || state.PlayerStates.Players[playerIndex].Resources.Gold > startGold;
        }

        private static int GetStockpileValue(GameState state, int playerIndex, ResourceType resourceType)
        {
            if (resourceType == ResourceType.Food)
            {
                return state.PlayerStates.Players[playerIndex].Resources.Food;
            }

            if (resourceType == ResourceType.Wood)
            {
                return state.PlayerStates.Players[playerIndex].Resources.Wood;
            }

            if (resourceType == ResourceType.Gold)
            {
                return state.PlayerStates.Players[playerIndex].Resources.Gold;
            }

            return 0;
        }

        private static int[] BuildDeterministicFiveWorkerOrder(int[] workers, int seed)
        {
            var ordered = new List<int>(workers);
            ordered.Sort();
            int[] output = ordered.ToArray();
            int rotate = output.Length == 0 ? 0 : seed % output.Length;
            RotateLeft(output, rotate);
            return output;
        }

        private static int[] BuildDeterministicNodeOrder(int[] nodeIds, int seed)
        {
            var ordered = new List<int>(nodeIds);
            ordered.Sort();
            int[] output = ordered.ToArray();
            int rotate = output.Length == 0 ? 0 : seed % output.Length;
            RotateLeft(output, rotate);
            return output;
        }

        private static void FillRotatedWorkerGroup(int[] orderedWorkers, int cycle, int[] output)
        {
            for (int i = 0; i < orderedWorkers.Length; i++)
            {
                output[i] = orderedWorkers[(i + cycle) % orderedWorkers.Length];
            }
        }

        private static void RotateLeft(int[] values, int count)
        {
            if (values.Length == 0)
            {
                return;
            }

            int shift = count % values.Length;
            if (shift == 0)
            {
                return;
            }

            int[] clone = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                clone[i] = values[(i + shift) % values.Length];
            }

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = clone[i];
            }
        }

















        private static void AdvanceTradeUntilFirstDeposit(GameState state, GameRules rules, TickRunner runner, CommandBuffer buffer, int maxTicks)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                if (i > 0)
                {
                    AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
                if (state.PlayerStates.Players[0].Resources.Gold >= 20)
                {
                    return;
                }
            }
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
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, buildingId, 240, 2, 2);
        }

        private static void AdvanceUntilBuildingComplete(GameState state, GameRules rules, CommandBuffer buffer, TickRunner runner, int buildingId, int maxTicks, int startTick, uint startSequence)
        {
            int tick = startTick;
            uint sequence = startSequence;
            while (tick < startTick + maxTicks && state.EntityState.Buildings[state.EntityState.EntityLookup[buildingId].Index].IsUnderConstruction)
            {
                AddNoOp(buffer, tick, 0, sequence++);
                if (rules.MaxPlayers > 1)
                {
                    AddNoOp(buffer, tick, 1, sequence++);
                }

                runner.AdvanceOneTick(state, rules, buffer);
                tick++;
            }
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

        private static void QueueImmediateVillager(Building building)
        {
            building.TrainingQueue.Add(new TrainingQueueItem(UnitTypeId.Villager, 1));
        }

        private static void AdvanceSingleNoOp(GameState state, GameRules rules, int tick, uint sequence)
        {
            var buffer = new CommandBuffer();
            AddNoOp(buffer, tick, 0, sequence);
            new TickRunner().AdvanceOneTick(state, rules, buffer);
        }

        private static FixedVector2 RunImmediateSpawnAndReturnPosition(ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(seed, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            QueueImmediateVillager(townCenter);
            AdvanceSingleNoOp(state, rules, 0, 0);
            return state.EntityState.Units[state.EntityState.Units.Count - 1].Position;
        }

        private static Building FindBuildingById(GameState state, int buildingId)
        {
            if (!state.EntityState.EntityLookup.TryGetValue(buildingId, out EntityRef entityRef)
                || entityRef.Kind != EntityKind.Building
                || entityRef.Index < 0
                || entityRef.Index >= state.EntityState.Buildings.Count)
            {
                throw new InvalidOperationException("building not found id=" + buildingId);
            }

            return state.EntityState.Buildings[entityRef.Index];
        }

        private static int FindUnderConstructionBuildingId(GameState state, int ownerPlayerIndex, BuildingTypeId buildingTypeId)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.OwnerPlayerIndex == ownerPlayerIndex
                    && building.BuildingTypeId == buildingTypeId
                    && building.IsUnderConstruction
                    && !building.IsDead)
                {
                    return building.Id;
                }
            }

            throw new InvalidOperationException("under-construction building not found owner=" + ownerPlayerIndex + " type=" + buildingTypeId);
        }

        private static int FindUnderConstructionBuildingIdOrZero(GameState state, int ownerPlayerIndex, BuildingTypeId buildingTypeId)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.OwnerPlayerIndex == ownerPlayerIndex
                    && building.BuildingTypeId == buildingTypeId
                    && building.IsUnderConstruction
                    && !building.IsDead)
                {
                    return building.Id;
                }
            }

            return 0;
        }

        private static int FindCompletedBuildingId(GameState state, int ownerPlayerIndex, BuildingTypeId buildingTypeId)
        {
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.OwnerPlayerIndex == ownerPlayerIndex
                    && building.BuildingTypeId == buildingTypeId
                    && !building.IsUnderConstruction
                    && !building.IsDead)
                {
                    return building.Id;
                }
            }

            return 0;
        }

        private static int FindNearestResourceNodeId(GameState state, FixedVector2 origin, ResourceType resourceType)
        {
            int id = 0;
            long bestDistance = long.MaxValue;
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted || node.ResourceType != resourceType)
                {
                    continue;
                }

                long distance = (node.Position - origin).LengthSquaredRaw();
                if (id == 0 || distance < bestDistance || (distance == bestDistance && node.Id < id))
                {
                    id = node.Id;
                    bestDistance = distance;
                }
            }

            if (id == 0)
            {
                throw new InvalidOperationException("resource node not found type=" + resourceType);
            }

            return id;
        }

        private static void AssertResourceNodeStable(GameState state, int expectedId, ResourceType expectedType, int expectedTileX, int expectedTileY)
        {
            ResourceNode node = FindResourceNodeById(state, expectedId);
            AssertEqual(expectedType, node.ResourceType, "resource id should match expected type id=" + expectedId);
            AssertEqual(Fixed.FromInt(expectedTileX).Raw, node.Position.X.Raw, "resource id should match expected x tile id=" + expectedId);
            AssertEqual(Fixed.FromInt(expectedTileY).Raw, node.Position.Y.Raw, "resource id should match expected y tile id=" + expectedId);
        }

        private static ResourceNode FindResourceNodeById(GameState state, int resourceNodeId)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                if (state.EconomyState.ResourceNodes[i].Id == resourceNodeId)
                {
                    return state.EconomyState.ResourceNodes[i];
                }
            }

            throw new InvalidOperationException("resource node not found id=" + resourceNodeId);
        }

        private static ResourceNode CreateTestResourceNode(GameState state, GatherProfileId profileId, FixedVector2 position, int amount)
        {
            GatherProfile profile = GameData.GetGatherProfile(profileId);
            int areaId = state.EconomyState.NextResourceAreaId++;
            state.EconomyState.ResourceAreas.Add(new ResourceArea
            {
                Id = areaId,
                AreaType = ResolveTestAreaType(profile.ResourceType),
                ResourceType = profile.ResourceType,
                GatherProfileId = profileId,
                Position = position
            });

            var node = new ResourceNode
            {
                Id = state.EconomyState.NextResourceNodeId++,
                ResourceAreaId = areaId,
                ResourceType = profile.ResourceType,
                NodeType = profile.NodeType,
                GatherProfileId = profileId,
                Position = position,
                RemainingAmount = amount
            };
            state.EconomyState.ResourceNodes.Add(node);
            return node;
        }

        private static GameState CreateSingleNodeResourceAreaState(ulong seed, GatherProfileId profileId, out int nodeId, out int areaId)
        {
            GameState state = CreateOccupancyState(seed, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            areaId = AddTestResourceArea(state, profileId, FixedVector2.FromInts(10, 10));
            nodeId = AddTestResourceNodeToArea(state, areaId, profileId, FixedVector2.FromInts(10, 10), GameData.VillagerGatherPerTick);
            return state;
        }

        private static GameState CreateTwoNodeResourceAreaState(ulong seed, GatherProfileId profileId, out int firstNodeId, out int secondNodeId, out int areaId)
        {
            GameState state = CreateOccupancyState(seed, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            areaId = AddTestResourceArea(state, profileId, FixedVector2.FromInts(10, 10));
            firstNodeId = AddTestResourceNodeToArea(state, areaId, profileId, FixedVector2.FromInts(10, 10), GameData.VillagerGatherPerTick);
            secondNodeId = AddTestResourceNodeToArea(state, areaId, profileId, FixedVector2.FromInts(12, 10), GameData.StartingWoodAmount);
            return state;
        }

        private static int AddTestResourceArea(GameState state, GatherProfileId profileId, FixedVector2 position)
        {
            GatherProfile profile = GameData.GetGatherProfile(profileId);
            int areaId = state.EconomyState.NextResourceAreaId++;
            state.EconomyState.ResourceAreas.Add(new ResourceArea
            {
                Id = areaId,
                AreaType = ResolveTestAreaType(profile.ResourceType),
                ResourceType = profile.ResourceType,
                GatherProfileId = profileId,
                Position = position
            });

            return areaId;
        }

        private static int AddTestResourceNodeToArea(GameState state, int areaId, GatherProfileId profileId, FixedVector2 position, int amount)
        {
            GatherProfile profile = GameData.GetGatherProfile(profileId);
            int nodeId = state.EconomyState.NextResourceNodeId++;
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = nodeId,
                ResourceAreaId = areaId,
                ResourceType = profile.ResourceType,
                NodeType = profile.NodeType,
                GatherProfileId = profileId,
                Position = position,
                RemainingAmount = amount
            });

            return nodeId;
        }

        private static GameState CreateDropoffReservationState(ulong seed, ResourceType resourceType, out int townCenterId)
        {
            GameState state = CreateOccupancyState(seed, 1);
            townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            int first = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 10));
            int second = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 11));
            int third = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 12));
            SetFullCarrier(state, first, resourceType);
            SetFullCarrier(state, second, resourceType);
            SetFullCarrier(state, third, resourceType);
            return state;
        }

        private static GameState CreateGroupGatherState(ulong seed, int workerCount, out int resourceId)
        {
            GameState state = CreateOccupancyState(seed, 1);
            for (int i = 0; i < workerCount; i++)
            {
                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(6, 8 + i));
            }

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            int areaId = AddTestResourceArea(state, GatherProfileId.Tree, FixedVector2.FromInts(12, 10));
            resourceId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.Tree, FixedVector2.FromInts(12, 10), GameData.StartingWoodAmount);
            return state;
        }

        private static GameState CreateGroupBerryAreaState(ulong seed, int workerCount, out int areaId, out int[] nodeIds, out int[] unitIds)
        {
            GameState state = CreateOccupancyState(seed, 1);
            unitIds = new int[workerCount];
            for (int i = 0; i < workerCount; i++)
            {
                unitIds[i] = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(6, 8 + i));
            }

            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            areaId = AddTestResourceArea(state, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 10));
            nodeIds = new int[3];
            nodeIds[0] = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(12, 10), GameData.StartingFoodAmount);
            nodeIds[1] = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(13, 11), GameData.StartingFoodAmount);
            nodeIds[2] = AddTestResourceNodeToArea(state, areaId, GatherProfileId.BerryBush, FixedVector2.FromInts(11, 11), GameData.StartingFoodAmount);
            return state;
        }

        private static int[] CreateLineOfUnits(GameState state, int count, int ownerPlayerIndex, int startX, int startY, int deltaX, int deltaY)
        {
            var unitIds = new int[count];
            for (int i = 0; i < count; i++)
            {
                unitIds[i] = EntityFactory.CreateUnit(
                    state,
                    ownerPlayerIndex,
                    UnitTypeId.Scout,
                    FixedVector2.FromInts(startX + i * deltaX, startY + i * deltaY));
            }

            return unitIds;
        }

        private static int[] CreateGridOfVillagers(GameState state, int count, int startX, int startY, int columns)
        {
            var unitIds = new int[count];
            for (int i = 0; i < count; i++)
            {
                unitIds[i] = EntityFactory.CreateUnit(
                    state,
                    0,
                    UnitTypeId.Villager,
                    FixedVector2.FromInts(startX + i % columns, startY + i / columns));
            }

            return unitIds;
        }

        private static bool AnyWorkerHasResourceIntent(GameState state, int[] unitIds)
        {
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (unit.CurrentResourceAreaId != 0 || unit.CurrentResourceNodeId != 0 || unit.CarriedAmount > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindNearbyResourceNodeId(GameState state, FixedVector2 origin, ResourceType resourceType)
        {
            int bestId = 0;
            long bestDistance = long.MaxValue;
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted || node.ResourceType != resourceType)
                {
                    continue;
                }

                long distance = (node.Position - origin).LengthSquaredRaw();
                if (bestId == 0 || distance < bestDistance || (distance == bestDistance && node.Id < bestId))
                {
                    bestId = node.Id;
                    bestDistance = distance;
                }
            }

            if (bestId == 0)
            {
                throw new InvalidOperationException("nearby resource node not found type=" + resourceType);
            }

            return bestId;
        }

        private static void SetFullCarrier(GameState state, int unitId, ResourceType resourceType)
        {
            Unit unit = FindUnitById(state, unitId);
            unit.CarriedResourceType = resourceType;
            unit.CarriedAmount = GameData.VillagerCarryCapacity;
        }

        private static Unit FindUnitById(GameState state, int unitId)
        {
            if (!state.EntityState.EntityLookup.TryGetValue(unitId, out EntityRef entityRef)
                || entityRef.Kind != EntityKind.Unit
                || entityRef.Index < 0
                || entityRef.Index >= state.EntityState.Units.Count)
            {
                throw new InvalidOperationException("unit not found id=" + unitId);
            }

            return state.EntityState.Units[entityRef.Index];
        }

        private static void AssertMultipleFullCarriersReserveDistinctDropoffSlots(ResourceType resourceType, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateDropoffReservationState(seed, resourceType, out int townCenterId);
            var buffer = new CommandBuffer();
            AddNoOp(buffer, 0, 0, (uint)seed);
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            int firstId = state.EntityState.Units[0].Id;
            int secondId = state.EntityState.Units[1].Id;
            int thirdId = state.EntityState.Units[2].Id;
            AssertDistinctReservations(
                state,
                new[] { firstId, secondId, thirdId },
                InteractionReservationKind.Dropoff,
                townCenterId,
                "full " + resourceType + " carriers should reserve different dropoff slots");
        }


        private static ResourceAreaType ResolveTestAreaType(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Food:
                    return ResourceAreaType.BerryPatch;
                case ResourceType.Wood:
                    return ResourceAreaType.Forest;
                case ResourceType.Gold:
                    return ResourceAreaType.GoldDeposit;
                default:
                    return ResourceAreaType.None;
            }
        }

        private static ResourceArea FindResourceAreaById(GameState state, int resourceAreaId)
        {
            for (int i = 0; i < state.EconomyState.ResourceAreas.Count; i++)
            {
                if (state.EconomyState.ResourceAreas[i].Id == resourceAreaId)
                {
                    return state.EconomyState.ResourceAreas[i];
                }
            }

            throw new InvalidOperationException("resource area not found id=" + resourceAreaId);
        }

        private static int FindFirstResourceNodeIdByType(GameState state, ResourceType resourceType)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (!node.IsDepleted && node.ResourceType == resourceType)
                {
                    return node.Id;
                }
            }

            throw new InvalidOperationException("resource node not found type=" + resourceType);
        }

        private static void AssertGatherCommandKeepsSelectedResourceTarget(ResourceType resourceType, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(seed, 1);
            int resourceId = FindFirstResourceNodeIdByType(state, resourceType);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);
            AssertEqual(resourceId, state.EntityState.Units[0].CurrentResourceNodeId, "gather target should persist exact selected resource id");
        }

        private static void AssertVillagerReturnsToSameTargetAfterDeposit(ResourceType resourceType, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(seed, 1);
            int resourceId = FindFirstResourceNodeIdByType(state, resourceType);
            ResourceNode node = FindResourceNodeById(state, resourceId);
            state.EntityState.Units[0].Position = new FixedVector2(node.Position.X + Fixed.FromInt(1), node.Position.Y);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 1; tick <= 150; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(9100 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            Unit unit = state.EntityState.Units[0];
            AssertEqual(resourceId, unit.CurrentResourceNodeId, "worker should return to exact assigned resource id after deposit for " + resourceType);
        }

        private static void AssertBlockedCarrierRetargetsAndDeposits(ResourceType resourceType, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(seed, 1);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(25, 20));
            int resourceId = state.EconomyState.NextResourceNodeId++;
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = resourceId,
                ResourceType = resourceType,
                Position = FixedVector2.FromInts(26, 20),
                RemainingAmount = GameData.StartingFoodAmount
            });
            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateBuildingInteractionTiles(state, tc);
            Unit worker = state.EntityState.Units[state.EntityState.EntityLookup[workerId].Index];
            SpatialRules.TileCoord blockedTile = default;
            bool foundBlockedWithAlternate = false;
            int workerTileX = SpatialRules.GetTileX(worker.Position);
            int workerTileY = SpatialRules.GetTileY(worker.Position);
            for (int i = 0; i < ring.Count && !foundBlockedWithAlternate; i++)
            {
                for (int j = 0; j < ring.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (DeterministicPathfinder.TryFindNextTile(state, workerTileX, workerTileY, ring[j].X, ring[j].Y, out _, out _))
                    {
                        blockedTile = ring[i];
                        foundBlockedWithAlternate = true;
                        break;
                    }
                }
            }

            AssertEqual(true, foundBlockedWithAlternate, "test setup should provide an alternate reachable dropoff interaction tile");
            worker.CurrentResourceNodeId = resourceId;
            worker.CarriedResourceType = resourceType;
            worker.CarriedAmount = GameData.VillagerCarryCapacity;
            worker.HasMoveTarget = true;
            worker.MoveTarget = FixedVector2.FromInts(blockedTile.X, blockedTile.Y);
            worker.LastMovedTick = 0;
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(blockedTile.X, blockedTile.Y), false);
            state.Tick = GameData.NoProgressTimeoutTicks;

            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            AddNoOp(buffer, state.Tick, 0, 7000);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, worker.HasMoveTarget, "blocked full carrier should retarget and keep dropoff intent");
            AssertEqual(resourceId, worker.CurrentResourceNodeId, "temporary congestion should not clear resource target");

            int startFood = state.PlayerStates.Players[0].Resources.Food;
            int startWood = state.PlayerStates.Players[0].Resources.Wood;
            int startGold = state.PlayerStates.Players[0].Resources.Gold;
            bool retargetedAwayFromBlockedTile = false;
            for (int i = 0; i < 80 && worker.CarriedAmount > 0; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(7100 + i));
                runner.AdvanceOneTick(state, rules, buffer);
                if (SpatialRules.GetTileX(worker.MoveTarget) != blockedTile.X || SpatialRules.GetTileY(worker.MoveTarget) != blockedTile.Y)
                {
                    retargetedAwayFromBlockedTile = true;
                }
            }

            AssertEqual(true, retargetedAwayFromBlockedTile, "carrier should retarget away from stale blocked tile during recovery");
            AssertEqual(0, worker.CarriedAmount, "carrier should eventually deposit after deterministic retarget");
            AssertEqual(resourceId, worker.CurrentResourceNodeId, "worker should preserve assigned gather target after deposit");
            if (resourceType == ResourceType.Food)
            {
                AssertEqual(startFood + GameData.VillagerCarryCapacity, state.PlayerStates.Players[0].Resources.Food, "food deposit should apply after retarget");
            }
            else if (resourceType == ResourceType.Wood)
            {
                AssertEqual(startWood + GameData.VillagerCarryCapacity, state.PlayerStates.Players[0].Resources.Wood, "wood deposit should apply after retarget");
            }
            else if (resourceType == ResourceType.Gold)
            {
                AssertEqual(startGold + GameData.VillagerCarryCapacity, state.PlayerStates.Players[0].Resources.Gold, "gold deposit should apply after retarget");
            }
        }

        private static GameState CreateConstructionPacingState(ulong seed, out Building foundation)
        {
            GameState state = CreateOccupancyState(seed, 1);
            int builderId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 10));
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            foundation = FindBuildingById(state, foundationId);
            foundation.AssignedBuilderIds.Add(builderId);
            Unit builder = FindUnitById(state, builderId);
            builder.CurrentBuildTargetId = foundationId;
            return state;
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

        private static GameState CreateResearchReadyState(ulong seed, int playerCount, out int buildingId)
        {
            GameState state = GameInitializer.CreateNomadStart(seed, playerCount);
            buildingId = SetupResearchReadyState(state, 0);
            return state;
        }

        private static int SetupResearchReadyState(GameState state, int playerIndex)
        {
            int buildingId = AddCompletedTownCenter(state, playerIndex, FixedVector2.FromInts(10, 10));
            state.PlayerStates.Players[playerIndex].Resources.Food = GameData.InfantryAttack1FoodCost;
            state.PlayerStates.Players[playerIndex].Resources.Gold = GameData.InfantryAttack1GoldCost;
            return buildingId;
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
            EntityFactory.CreateUnit(state, 0, UnitTypeId.TradeCart, FixedVector2.FromInts(2, 20));
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

        private static void AssertDryArabiaTcPlacementAccepted(int playerIndex)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(124);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(
                new CommandHeader(0, playerIndex, 0, CommandType.PlaceTownCenter),
                new PlaceTownCenterCommand(DryArabiaTest01MapDefinition.GetTownCenterZone(playerIndex))));
            for (int player = 0; player < 2; player++)
            {
                if (player == playerIndex)
                {
                    continue;
                }

                buffer.Add(new CommandEnvelope(new CommandHeader(0, player, 0, CommandType.NoOp), new NoOpCommand()));
            }

            int beforeBuildings = state.EntityState.Buildings.Count;
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(beforeBuildings + 1, state.EntityState.Buildings.Count, "player " + playerIndex + " should be able to place TC in intended dry arabia zone");
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "player " + playerIndex + " TC placement zone should not reject");
        }

        private static int CountUnits(GameState state, int playerIndex, UnitTypeId unitTypeId)
        {
            int count = 0;
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.OwnerPlayerIndex == playerIndex && unit.UnitTypeId == unitTypeId && !unit.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountNeutralTradePosts(GameState state)
        {
            int count = 0;
            for (int i = 0; i < state.EntityState.Buildings.Count; i++)
            {
                Building building = state.EntityState.Buildings[i];
                if (building.OwnerPlayerIndex == GameData.NeutralOwnerPlayerIndex && building.BuildingTypeId == BuildingTypeId.TradePost && !building.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasResourceAt(GameState state, ResourceType resourceType, FixedVector2 position)
        {
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.ResourceType == resourceType && node.Position.X.Raw == position.X.Raw && node.Position.Y.Raw == position.Y.Raw && !node.IsDepleted)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasNearbyResource(GameState state, FixedVector2 origin, ResourceType resourceType, int maxDistanceTiles)
        {
            long maxRaw = Fixed.FromInt(maxDistanceTiles).Raw;
            long maxSquaredRaw = checked(maxRaw * maxRaw);
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.ResourceType != resourceType || node.IsDepleted)
                {
                    continue;
                }

                if ((origin - node.Position).LengthSquaredRaw() <= maxSquaredRaw)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasResourceAreaWithNodeNear(GameState state, FixedVector2 origin, ResourceAreaType areaType, int maxDistanceTiles)
        {
            long maxRaw = Fixed.FromInt(maxDistanceTiles).Raw;
            long maxSquaredRaw = checked(maxRaw * maxRaw);
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted)
                {
                    continue;
                }

                ResourceArea area = FindResourceAreaById(state, node.ResourceAreaId);
                if (area.AreaType != areaType)
                {
                    continue;
                }

                if ((origin - node.Position).LengthSquaredRaw() <= maxSquaredRaw)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasResourceInteractionSlotsNear(GameState state, FixedVector2 origin, ResourceAreaType areaType, int maxDistanceTiles)
        {
            long maxRaw = Fixed.FromInt(maxDistanceTiles).Raw;
            long maxSquaredRaw = checked(maxRaw * maxRaw);
            for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
            {
                ResourceNode node = state.EconomyState.ResourceNodes[i];
                if (node.IsDepleted)
                {
                    continue;
                }

                ResourceArea area = FindResourceAreaById(state, node.ResourceAreaId);
                if (area.AreaType != areaType)
                {
                    continue;
                }

                if ((origin - node.Position).LengthSquaredRaw() > maxSquaredRaw)
                {
                    continue;
                }

                List<SpatialRules.TileCoord> slots = SpatialRules.EnumerateResourceInteractionTiles(state, node);
                if (slots.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static Building FindLatestPlayerBuilding(GameState state, int playerIndex, BuildingTypeId buildingTypeId)
        {
            for (int i = state.EntityState.Buildings.Count - 1; i >= 0; i--)
            {
                Building building = state.EntityState.Buildings[i];
                if (!building.IsDead && building.OwnerPlayerIndex == playerIndex && building.BuildingTypeId == buildingTypeId)
                {
                    return building;
                }
            }

            throw new InvalidOperationException("building not found player=" + playerIndex + " type=" + buildingTypeId);
        }

        private static int[] GetPlayerVillagerIds(GameState state, int playerIndex)
        {
            var ids = new List<int>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!unit.IsDead && unit.OwnerPlayerIndex == playerIndex && unit.UnitTypeId == UnitTypeId.Villager)
                {
                    ids.Add(unit.Id);
                }
            }

            ids.Sort();
            return ids.ToArray();
        }

        private static bool AnyPlayerVillagerCanReachTile(GameState state, int playerIndex, int targetX, int targetY)
        {
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.OwnerPlayerIndex != playerIndex || unit.UnitTypeId != UnitTypeId.Villager)
                {
                    continue;
                }

                if (DeterministicPathfinder.TryFindNextTile(
                    state,
                    unit.Position.X.FloorToInt(),
                    unit.Position.Y.FloorToInt(),
                    targetX,
                    targetY,
                    out _,
                    out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasResource(GameSnapshot snapshot, ResourceType resourceType)
        {
            for (int i = 0; i < snapshot.Resources.Count; i++)
            {
                if (snapshot.Resources[i].ResourceType == resourceType)
                {
                    return true;
                }
            }

            return false;
        }

        private static UnitSnapshot FindUnitSnapshot(GameSnapshot snapshot, int unitId)
        {
            for (int i = 0; i < snapshot.Units.Count; i++)
            {
                if (snapshot.Units[i].Id == unitId)
                {
                    return snapshot.Units[i];
                }
            }

            throw new InvalidOperationException("unit snapshot not found entity=" + unitId);
        }

        private static BuildingSnapshot FindBuildingSnapshot(GameSnapshot snapshot, int buildingId)
        {
            for (int i = 0; i < snapshot.Buildings.Count; i++)
            {
                if (snapshot.Buildings[i].Id == buildingId)
                {
                    return snapshot.Buildings[i];
                }
            }

            throw new InvalidOperationException("building snapshot not found entity=" + buildingId);
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

        private static bool HasGodotPrimitive(GodotFrameDto frame, VisualPrimitiveKind kind)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                if (frame.Primitives[i].Kind == (int)kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasGodotPrimitiveWithType(GodotFrameDto frame, VisualPrimitiveKind kind, int typeId)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                if (frame.Primitives[i].Kind == (int)kind && frame.Primitives[i].TypeId == typeId)
                {
                    return true;
                }
            }

            return false;
        }

        private static GodotPrimitiveDto FindGodotPrimitive(GodotFrameDto frame, VisualPrimitiveKind kind, int entityId)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                if (frame.Primitives[i].Kind == (int)kind && frame.Primitives[i].EntityId == entityId)
                {
                    return frame.Primitives[i];
                }
            }

            throw new InvalidOperationException("godot primitive not found kind=" + kind + " entity=" + entityId);
        }

        private static GodotPrimitiveDto FindGodotPrimitiveWithType(GodotFrameDto frame, VisualPrimitiveKind kind, int typeId)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                if (frame.Primitives[i].Kind == (int)kind && frame.Primitives[i].TypeId == typeId)
                {
                    return frame.Primitives[i];
                }
            }

            throw new InvalidOperationException("godot primitive not found kind=" + kind + " type=" + typeId);
        }

        private static GodotPrimitiveDto FindGodotResourcePrimitive(GodotFrameDto frame, int resourceId)
        {
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if ((primitive.Kind == (int)VisualPrimitiveKind.FoodResourceCircle
                        || primitive.Kind == (int)VisualPrimitiveKind.WoodResourceCircle
                        || primitive.Kind == (int)VisualPrimitiveKind.GoldResourceCircle)
                    && primitive.EntityId == resourceId)
                {
                    return primitive;
                }
            }

            throw new InvalidOperationException("godot resource primitive not found id=" + resourceId);
        }

        private static bool HasGodotUnitStatusWithType(GodotFrameDto frame, int unitTypeId)
        {
            for (int i = 0; i < frame.UnitStatuses.Length; i++)
            {
                if (frame.UnitStatuses[i].UnitTypeId == unitTypeId)
                {
                    return true;
                }
            }

            return false;
        }

        private static GodotClientFacade CreateGodotFacadeWithCompletedCapital(ulong seed)
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(seed);
            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            facade.AdvanceTicks(2);
            return facade;
        }

        private static GodotFrameDto CreateGodotInteractionFrame(GodotPrimitiveDto[] primitives)
        {
            return CreateGodotInteractionFrame(primitives, new GodotBuildingStatusDto[0]);
        }

        private static GodotFrameDto CreateGodotInteractionFrame(GodotPrimitiveDto[] primitives, GodotBuildingStatusDto[] buildingStatuses)
        {
            return CreateGodotInteractionFrame(primitives, buildingStatuses, new GodotUnitStatusDto[0]);
        }

        private static GodotFrameDto CreateGodotInteractionFrame(
            GodotPrimitiveDto[] primitives,
            GodotBuildingStatusDto[] buildingStatuses,
            GodotUnitStatusDto[] unitStatuses)
        {
            return new GodotFrameDto(
                0,
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(false, -1, -1, 0),
                primitives,
                unitStatuses,
                buildingStatuses);
        }

        private static GodotFrameDto CreateGodotHudFrame(
            int tick,
            GodotLocalPlayerDto localPlayer,
            GodotUnitStatusDto[] unitStatuses,
            GodotBuildingStatusDto[] buildingStatuses,
            int rejectedCommandCount = 0)
        {
            return new GodotFrameDto(
                tick,
                0,
                localPlayer,
                new GodotMatchDto(false, -1, -1, rejectedCommandCount),
                new GodotPrimitiveDto[0],
                unitStatuses,
                buildingStatuses);
        }

        private static GodotPrimitiveDto CreateGodotPrimitive(VisualPrimitiveKind kind, int entityId, int ownerPlayerIndex, int x, int y)
        {
            return CreateGodotPrimitive(kind, entityId, ownerPlayerIndex, 0, x, y, false);
        }

        private static GodotPrimitiveDto CreateGodotPrimitiveWithType(VisualPrimitiveKind kind, int entityId, int ownerPlayerIndex, int typeId, int x, int y)
        {
            return CreateGodotPrimitive(kind, entityId, ownerPlayerIndex, typeId, x, y, false);
        }

        private static GodotPrimitiveDto CreateGodotPrimitiveWithSize(VisualPrimitiveKind kind, int entityId, int ownerPlayerIndex, int typeId, int x, int y, int sizeTiles)
        {
            return new GodotPrimitiveDto(
                (int)kind,
                entityId,
                typeId,
                ownerPlayerIndex,
                Fixed.FromInt(x).Raw,
                Fixed.FromInt(y).Raw,
                0,
                0,
                Fixed.FromInt(sizeTiles).Raw,
                10,
                10,
                false);
        }

        private static GodotPrimitiveDto CreateGodotPrimitiveWithDimensions(VisualPrimitiveKind kind, int entityId, int ownerPlayerIndex, int typeId, int x, int y, int widthTiles, int heightTiles)
        {
            int maxSize = System.Math.Max(widthTiles, heightTiles);
            return new GodotPrimitiveDto(
                (int)kind,
                entityId,
                typeId,
                ownerPlayerIndex,
                Fixed.FromInt(x).Raw,
                Fixed.FromInt(y).Raw,
                0,
                0,
                Fixed.FromInt(maxSize).Raw,
                Fixed.FromInt(widthTiles).Raw,
                Fixed.FromInt(heightTiles).Raw,
                10,
                10,
                false);
        }

        private static GodotPrimitiveDto CreateGodotPrimitiveWithCapital(VisualPrimitiveKind kind, int entityId, int ownerPlayerIndex, int x, int y)
        {
            return CreateGodotPrimitive(kind, entityId, ownerPlayerIndex, 0, x, y, true);
        }

        private static GodotPrimitiveDto CreateGodotPrimitive(
            VisualPrimitiveKind kind,
            int entityId,
            int ownerPlayerIndex,
            int typeId,
            int x,
            int y,
            bool isCapital)
        {
            return new GodotPrimitiveDto(
                (int)kind,
                entityId,
                typeId,
                ownerPlayerIndex,
                Fixed.FromInt(x).Raw,
                Fixed.FromInt(y).Raw,
                0,
                0,
                Fixed.FromInt(1).Raw,
                10,
                10,
                isCapital);
        }

        private static GodotBuildingStatusDto FindGodotBuildingStatus(GodotFrameDto frame, int buildingId)
        {
            for (int i = 0; i < frame.BuildingStatuses.Length; i++)
            {
                if (frame.BuildingStatuses[i].BuildingId == buildingId)
                {
                    return frame.BuildingStatuses[i];
                }
            }

            throw new InvalidOperationException("godot building status not found entity=" + buildingId);
        }

        private static GodotUnitStatusDto FindGodotUnitStatus(GodotFrameDto frame, int unitId)
        {
            for (int i = 0; i < frame.UnitStatuses.Length; i++)
            {
                if (frame.UnitStatuses[i].UnitId == unitId)
                {
                    return frame.UnitStatuses[i];
                }
            }

            throw new InvalidOperationException("godot unit status not found entity=" + unitId);
        }














        private static bool PlaceTownCenterWouldBeValidOnDryArabia(int playerIndex, int tileX, int tileY, ulong seed)
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(seed);
            var command = new PlaceTownCenterCommand(FixedVector2.FromInts(tileX, tileY));
            return command.IsValid(state, GameRules.CreatePhaseZeroDefaults(2), new CommandHeader(state.Tick, playerIndex, 0, CommandType.PlaceTownCenter));
        }

        private static bool PlaceTownCenterWouldBeValidOnDryArabiaWithPlacedTownCenter(int playerIndex, int tileX, int tileY, ulong seed)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(seed);
            FixedVector2 zone = DryArabiaTest01MapDefinition.GetTownCenterZone(playerIndex);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, playerIndex, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(zone)));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1 - playerIndex, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, buffer);
            return new PlaceTownCenterCommand(FixedVector2.FromInts(tileX, tileY))
                .IsValid(state, rules, new CommandHeader(state.Tick, playerIndex, 1, CommandType.PlaceTownCenter));
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

    }
}


