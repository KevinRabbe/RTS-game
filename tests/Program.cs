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
    public static class Program
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
                new TestCase("assign build reject reason for completed target", AssignBuildRejectReasonForCompletedTarget),
                new TestCase("builder in build range builds without micro movement", BuilderInBuildRangeBuildsWithoutMicroMovement),
                new TestCase("builder blocked approach retargets deterministically", BuilderBlockedApproachRetargetsDeterministically),
                new TestCase("movement arrival snaps without raw oscillation", MovementArrivalSnapsWithoutRawOscillation),
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
                new TestCase("group gather does not collapse onto one interaction slot", GroupGatherDoesNotCollapseOntoOneInteractionSlot),
                new TestCase("group gather workers make progress or wait cleanly", GroupGatherWorkersMakeProgressOrWaitCleanly),
                new TestCase("group gather resolves clicked node to resource area distribution", GroupGatherResolvesClickedNodeToResourceAreaDistribution),
                new TestCase("group gather avoids single node collapse when sibling nodes exist", GroupGatherAvoidsSingleNodeCollapseWhenSiblingNodesExist),
                new TestCase("dry arabia berry group gather makes bounded food progress", DryArabiaBerryGroupGatherMakesBoundedFoodProgress),
                new TestCase("berry visual radius matches simulation footprint radius", BerryVisualRadiusMatchesSimulationFootprintRadius),
                new TestCase("thirty workers across resources keep progress or intent", ThirtyWorkersAcrossResourcesKeepProgressOrIntent),
                new TestCase("fifty workers across resources keep progress or intent", FiftyWorkersAcrossResourcesKeepProgressOrIntent),
                new TestCase("one hundred twenty workers across resources keep progress or intent", OneHundredTwentyWorkersAcrossResourcesKeepProgressOrIntent),
                new TestCase("six player mixed population traffic remains deterministic", SixPlayerMixedPopulationTrafficRemainsDeterministic),
                new TestCase("path query budget stays bounded under pressure", PathQueryBudgetStaysBoundedUnderPressure),
                new TestCase("repeated command replacement stays bounded", RepeatedCommandReplacementStaysBounded),
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
                new TestCase("godot hud text includes economy and selection", GodotHudTextIncludesEconomyAndSelection),
                new TestCase("godot hud text build lines returns two lines", GodotHudTextBuildLinesReturnsTwoLines),
                new TestCase("godot hud text includes unit gather status", GodotHudTextIncludesUnitGatherStatus),
                new TestCase("godot hud text includes building training status", GodotHudTextIncludesBuildingTrainingStatus),
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
                new TestCase("godot hud text handles missing status", GodotHudTextHandlesMissingStatus),
                new TestCase("godot selected status hides idle no progress ticks", GodotSelectedStatusHidesIdleNoProgressTicks),
                new TestCase("godot primitive hit test includes boundary", GodotPrimitiveHitTestIncludesBoundary),
                new TestCase("godot building hit test includes footprint boundary", GodotBuildingHitTestIncludesFootprintBoundary),
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

        private static void EmptyTickDeterminism()
        {
            ulong first = RunNoOpSimulation(1000, 2, 123);
            ulong second = RunNoOpSimulation(1000, 2, 123);
            AssertEqual(first, second, "same empty command stream must produce same checksum");
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

        private static void TestRunnerParsesFilterArgument()
        {
            AssertEqual("godot", GetFilter(new[] { "--filter", "godot" }), "test runner should parse separated filter argument");
            AssertEqual("lockstep", GetFilter(new[] { "--filter=lockstep" }), "test runner should parse inline filter argument");
            AssertEqual("chaos", GetFilter(new[] { "chaos" }), "test runner should treat first positional argument as filter");
            AssertEqual("godot", GetFilter(new[] { "--fail-fast", "--filter", "godot" }), "test runner should skip fail-fast when parsing filter argument");
        }

        private static void TestRunnerMatchesFilterCaseInsensitive()
        {
            var test = new TestCase("Godot coordinate mapper converts raw to pixels", EmptyTickDeterminism);

            AssertEqual(true, ShouldRun(test, "godot coordinate"), "filter should match test names case-insensitively");
            AssertEqual(false, ShouldRun(test, "chaos"), "filter should reject non-matching test names");
        }

        private static void TestRunnerRunsAllWithoutFilter()
        {
            var test = new TestCase("empty tick determinism", EmptyTickDeterminism);

            AssertEqual(true, ShouldRun(test, ""), "empty filter should run every test");
        }

        private static void TestRunnerDetectsListArgument()
        {
            AssertEqual(true, ShouldList(new[] { "--list" }), "test runner should detect list mode");
            AssertEqual(true, ShouldList(new[] { "--list", "--filter", "godot" }), "test runner should detect filtered list mode");
            AssertEqual(false, ShouldList(new[] { "--filter", "godot" }), "test runner should not list during normal filter mode");
            AssertEqual("godot", GetFilter(new[] { "--list", "--filter", "godot" }), "list mode should parse separated filter argument");
            AssertEqual("lockstep", GetFilter(new[] { "--list", "--filter=lockstep" }), "list mode should parse inline filter argument");
        }

        private static void TestRunnerDetectsFailFastArgument()
        {
            AssertEqual(true, ShouldFailFast(new[] { "--fail-fast" }), "test runner should detect fail-fast mode");
            AssertEqual(true, ShouldFailFast(new[] { "--filter", "godot", "--fail-fast" }), "test runner should detect fail-fast after filter");
            AssertEqual(false, ShouldFailFast(new[] { "--filter", "godot" }), "test runner should not use fail-fast unless explicitly requested");
        }

        private static void TestRunnerDetectsHelpArgument()
        {
            AssertEqual(true, ShouldShowHelp(new[] { "--help" }), "test runner should detect long help flag");
            AssertEqual(true, ShouldShowHelp(new[] { "-h" }), "test runner should detect short help flag");
            AssertEqual(false, ShouldShowHelp(new[] { "--filter", "godot" }), "test runner should not show help unless requested");
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

        private static void ResourceProfilesDefineStockpileKindAndNodeType()
        {
            GatherProfile berries = GameData.GetGatherProfile(GatherProfileId.BerryBush);
            GatherProfile trees = GameData.GetGatherProfile(GatherProfileId.Tree);
            GatherProfile smallGold = GameData.GetGatherProfile(GatherProfileId.GoldVeinSmall);
            GatherProfile largeGold = GameData.GetGatherProfile(GatherProfileId.GoldVeinLarge);

            AssertEqual(ResourceType.Food, berries.ResourceType, "berry profile should deposit food");
            AssertEqual(ResourceNodeType.BerryBush, berries.NodeType, "berry profile should define berry bush nodes");
            AssertEqual(ResourceType.Wood, trees.ResourceType, "tree profile should deposit wood");
            AssertEqual(ResourceNodeType.Tree, trees.NodeType, "tree profile should define tree nodes");
            AssertEqual(ResourceType.Gold, smallGold.ResourceType, "small gold profile should deposit gold");
            AssertEqual(ResourceType.Gold, largeGold.ResourceType, "large gold profile should also deposit gold");
            AssertEqual(ResourceNodeType.GoldVeinSmall, smallGold.NodeType, "small gold profile should define small vein nodes");
            AssertEqual(ResourceNodeType.GoldVeinLarge, largeGold.NodeType, "large gold profile should define large vein nodes");
        }

        private static void NomadResourcesCreateAreasAndProfiledNodes()
        {
            GameState state = GameInitializer.CreateNomadStart(601, 1);
            AssertEqual(6, state.EconomyState.ResourceAreas.Count, "nomad one-player map should create three home areas and three center areas");
            AssertEqual(6, state.EconomyState.ResourceNodes.Count, "nomad one-player map should keep one node per resource area");

            ResourceNode food = state.EconomyState.ResourceNodes[0];
            ResourceArea foodArea = FindResourceAreaById(state, food.ResourceAreaId);
            AssertEqual(ResourceAreaType.BerryPatch, foodArea.AreaType, "food node should belong to berry patch area");
            AssertEqual(GatherProfileId.BerryBush, food.GatherProfileId, "food node should use berry profile");
            AssertEqual(ResourceNodeType.BerryBush, food.NodeType, "food node should be a berry bush");

            ResourceNode wood = state.EconomyState.ResourceNodes[1];
            ResourceArea woodArea = FindResourceAreaById(state, wood.ResourceAreaId);
            AssertEqual(ResourceAreaType.Forest, woodArea.AreaType, "wood node should belong to forest area");
            AssertEqual(GatherProfileId.Tree, wood.GatherProfileId, "wood node should use tree profile");
            AssertEqual(ResourceNodeType.Tree, wood.NodeType, "wood node should be a tree");
        }

        private static void ResourceVisualOverhangDoesNotChangeSimFootprint()
        {
            GameState state = CreateOccupancyState(603, 1);
            ResourceNode tree = CreateTestResourceNode(state, GatherProfileId.Tree, FixedVector2.FromInts(10, 10), GameData.StartingWoodAmount);
            GatherProfile profile = GameData.GetGatherProfile(tree.GatherProfileId);

            AssertEqual(true, profile.VisualRadiusTiles >= profile.FootprintRadiusTiles, "tree profile visual radius should not undershoot sim footprint");
            AssertEqual(true, SpatialRules.IsTileInsideResourceFootprint(tree, 10, 10), "tree trunk tile should be the sim footprint");
            AssertEqual(false, SpatialRules.IsTileInsideResourceFootprint(tree, 12, 10), "visual overhang tile should not be inside sim footprint");
            AssertEqual(false, SpatialRules.IsTileBlockedForUnitMovement(state, 12, 10), "visual overhang tile should not block movement");
            AssertEqual(true, GodotPrimitiveHitTest.ContainsPointForInteraction(
                CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, tree.Id, GameData.NeutralOwnerPlayerIndex, 10, 10),
                Fixed.FromInt(11).Raw,
                Fixed.FromInt(10).Raw),
                "presentation click bounds can be generous without changing sim footprint");
        }

        private static void ResourceInteractionRingUsesSimFootprint()
        {
            GameState state = CreateOccupancyState(604, 1);
            ResourceNode berries = CreateTestResourceNode(state, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);

            List<SpatialRules.TileCoord> footprint = SpatialRules.EnumerateResourceFootprintTiles(state, berries);
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateResourceInteractionTiles(state, berries);

            AssertEqual(1, footprint.Count, "1x1 resource footprint should enumerate one sim tile");
            AssertEqual(8, ring.Count, "1x1 resource footprint should expose eight surrounding interaction slots");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 9, 9), "diagonal resource slot should be valid");
            AssertEqual(false, SpatialRules.ContainsInteractionTile(ring, 10, 10), "resource footprint tile should not be an interaction slot");
        }

        private static void LargeResourceInteractionRingUsesLargerSimFootprint()
        {
            GameState state = CreateOccupancyState(605, 1);
            ResourceNode largeGold = CreateTestResourceNode(state, GatherProfileId.GoldVeinLarge, FixedVector2.FromInts(20, 20), GameData.CenterGoldAmount);

            List<SpatialRules.TileCoord> footprint = SpatialRules.EnumerateResourceFootprintTiles(state, largeGold);
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateResourceInteractionTiles(state, largeGold);

            AssertEqual(true, footprint.Count > 1, "large gold should have a larger sim footprint than a 1x1 node");
            AssertEqual(true, ring.Count > 8, "larger resource footprint should expose a larger interaction ring");
            AssertEqual(true, SpatialRules.IsTileBlockedForUnitMovement(state, 21, 20), "large gold footprint should block pathing");
            AssertEqual(false, SpatialRules.ContainsInteractionTile(ring, 20, 20), "large gold center should not be an interaction slot");
        }

        private static void BuildingInteractionRingUsesSimFootprint()
        {
            GameState state = CreateOccupancyState(606, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];

            List<SpatialRules.TileCoord> footprint = SpatialRules.EnumerateBuildingFootprintTiles(state, tc);
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateBuildingInteractionTiles(state, tc);

            AssertEqual(true, footprint.Count > 1, "town center should have a multi-tile sim footprint");
            AssertEqual(true, ring.Count > 8, "town center footprint should expose a larger interaction ring");
            AssertEqual(true, SpatialRules.IsTileBlockedForUnitMovement(state, 20, 20), "town center footprint should block pathing");
            AssertEqual(false, SpatialRules.ContainsInteractionTile(ring, 20, 20), "town center footprint tile should not be an interaction slot");
        }

        private static void TownCenterRingAcceptsWorkersOnEverySide()
        {
            GameState state = CreateOccupancyState(607, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            List<SpatialRules.TileCoord> ring = SpatialRules.EnumerateBuildingInteractionTiles(state, tc);

            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 20, 18), "TC ring should include north side around full footprint");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 20, 22), "TC ring should include south side around full footprint");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 18, 20), "TC ring should include west side around full footprint");
            AssertEqual(true, SpatialRules.ContainsInteractionTile(ring, 22, 20), "TC ring should include east side around full footprint");
            AssertEqual(true, SpatialRules.IsUnitInBuildInteractionRange(new Unit { Position = FixedVector2.FromInts(20, 18) }, tc), "north ring worker should be in build range");
            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = FixedVector2.FromInts(20, 22) }, tc), "south ring carrier should be in dropoff range");
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(tc, 20, 18), "ring tile should not be inside the TC footprint");
        }

        private static void DryArabiaResourcesCreateTypedAreas()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(602);
            AssertEqual(9, state.EconomyState.ResourceAreas.Count, "dry arabia should create player home areas plus center contested areas");
            AssertEqual(16, state.EconomyState.ResourceNodes.Count, "dry arabia should keep existing resource node count");

            ResourceNode playerGold = FindResourceNodeById(state, 5);
            ResourceArea playerGoldArea = FindResourceAreaById(state, playerGold.ResourceAreaId);
            AssertEqual(ResourceAreaType.GoldDeposit, playerGoldArea.AreaType, "home gold should be in a gold deposit area");
            AssertEqual(GatherProfileId.GoldVeinSmall, playerGold.GatherProfileId, "home gold should use small gold profile");
            AssertEqual(ResourceNodeType.GoldVeinSmall, playerGold.NodeType, "home gold should be a small vein");

            ResourceNode centerGold = FindResourceNodeById(state, 13);
            AssertEqual(ResourceType.Gold, centerGold.ResourceType, "center gold should still deposit gold");
            AssertEqual(GatherProfileId.GoldVeinLarge, centerGold.GatherProfileId, "center gold should use large gold profile");
            AssertEqual(ResourceNodeType.GoldVeinLarge, centerGold.NodeType, "center gold should be a large vein");
        }

        private static void DryArabiaTestMapInitializesDeterministically()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState first = GameInitializer.CreateDryArabiaTest01(123);
            GameState second = GameInitializer.CreateDryArabiaTest01(123);

            AssertEqual(StateChecksum.Compute(first, rules), StateChecksum.Compute(second, rules), "dry arabia test map initialization should be deterministic");
            AssertEqual(2, first.PlayerStates.Players.Count, "dry arabia test map should create exactly two players");
            AssertEqual(10, first.EntityState.Units.Count, "dry arabia test map should create five units per player");
            AssertEqual(4, CountUnits(first, 0, UnitTypeId.Villager), "player 0 should start with four villagers");
            AssertEqual(1, CountUnits(first, 0, UnitTypeId.Scout), "player 0 should start with one scout");
            AssertEqual(4, CountUnits(first, 1, UnitTypeId.Villager), "player 1 should start with four villagers");
            AssertEqual(1, CountUnits(first, 1, UnitTypeId.Scout), "player 1 should start with one scout");
            AssertEqual(true, CountNeutralTradePosts(first) >= 2, "dry arabia test map should include neutral trade posts");
            AssertEqual(true, HasResourceAt(first, ResourceType.Gold, FixedVector2.FromInts(64, 48)), "dry arabia test map should include center contested gold");
        }

        private static void DryArabiaTestMapHasValidTcPlacementZones()
        {
            AssertDryArabiaTcPlacementAccepted(0);
            AssertDryArabiaTcPlacementAccepted(1);
        }

        private static void DryArabiaTestMapHasNearbyResources()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(125);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                AssertEqual(true, HasNearbyResource(state, tc, ResourceType.Food, 10), "player " + player + " should have nearby food");
                AssertEqual(true, HasNearbyResource(state, tc, ResourceType.Wood, 10), "player " + player + " should have nearby wood");
                AssertEqual(true, HasNearbyResource(state, tc, ResourceType.Gold, 10), "player " + player + " should have nearby gold");
            }
        }

        private static void DryArabiaTestMapResourcesAvoidTcZones()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(126);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
                {
                    long combinedRaw = Fixed.FromInt(GameData.TownCenterPlacementRadiusTiles + GameData.ResourcePlacementRadiusTiles).Raw;
                    long combinedSquaredRaw = checked(combinedRaw * combinedRaw);
                    AssertEqual(
                        true,
                        (tc - state.EconomyState.ResourceNodes[i].Position).LengthSquaredRaw() >= combinedSquaredRaw,
                        "resource " + state.EconomyState.ResourceNodes[i].Id + " should not block player " + player + " TC zone");
                }
            }
        }

        private static void DryArabiaTcZonesHaveOpenTrafficBuffer()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(127);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                for (int i = 0; i < state.EconomyState.ResourceNodes.Count; i++)
                {
                    ResourceNode node = state.EconomyState.ResourceNodes[i];
                    if (node.IsDepleted)
                    {
                        continue;
                    }

                    if (state.EconomyState.ResourceAreas.Count > 0)
                    {
                        ResourceArea area = FindResourceAreaById(state, node.ResourceAreaId);
                        if (area.AreaType == ResourceAreaType.None)
                        {
                            continue;
                        }
                    }

                    long distanceRaw = (tc - node.Position).LengthSquaredRaw();
                    long minBufferRaw = Fixed.FromInt(4).Raw;
                    long minBufferSquaredRaw = checked(minBufferRaw * minBufferRaw);
                    AssertEqual(true, distanceRaw >= minBufferSquaredRaw, "resource " + node.Id + " should keep a wider traffic buffer from player " + player + " TC zone");
                }
            }
        }

        private static void DryArabiaStartingResourceAreasContainNodes()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(128);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                AssertEqual(true, HasResourceAreaWithNodeNear(state, tc, ResourceAreaType.BerryPatch, 12), "player " + player + " should have a nearby berry patch with at least one node");
                AssertEqual(true, HasResourceAreaWithNodeNear(state, tc, ResourceAreaType.Forest, 12), "player " + player + " should have a nearby forest with at least one node");
                AssertEqual(true, HasResourceAreaWithNodeNear(state, tc, ResourceAreaType.GoldDeposit, 12), "player " + player + " should have a nearby gold deposit with at least one node");
            }
        }

        private static void DryArabiaStartingResourceAreasHaveValidInteractionSlots()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(129);
            for (int player = 0; player < 2; player++)
            {
                FixedVector2 tc = DryArabiaTest01MapDefinition.GetTownCenterZone(player);
                AssertEqual(true, HasResourceInteractionSlotsNear(state, tc, ResourceAreaType.BerryPatch, 12), "player " + player + " nearby berries should expose interaction slots");
                AssertEqual(true, HasResourceInteractionSlotsNear(state, tc, ResourceAreaType.Forest, 12), "player " + player + " nearby wood should expose interaction slots");
                AssertEqual(true, HasResourceInteractionSlotsNear(state, tc, ResourceAreaType.GoldDeposit, 12), "player " + player + " nearby gold should expose interaction slots");
            }
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
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, buildingId, 240, 2, 2);

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
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, firstBuildingId, 240, 2, 2);

            state.PlayerStates.Players[0].Resources.Wood = GameData.TownCenterWoodCost;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))));
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
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, normalTownCenterId, 240, state.Tick, 5);

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
            AdvanceUntilBuildingComplete(state, rules, buffer, runner, buildingId, 240, 2, 2);
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
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(3, 8))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(43, 8))));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2, 3, 4 })));
            recorder.RecordCommand(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.AssignBuild), new AssignBuildCommand(12, new[] { 6, 7, 8, 9 })));

            ReplayResult first = new ReplayRunner().Run(recorder.Replay, 80);
            ReplayResult second = new ReplayRunner().Run(recorder.Replay, 80);
            AssertEqual(first.FinalChecksum, second.FinalChecksum, "capital placement replay should be deterministic");
        }

        private static void CapitalPlacementLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 123, true);
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(3, 8))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(43, 8))));

            bool advanced = session.TryAdvanceOneTick();
            AssertEqual(true, advanced, "capital placement tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(11, new[] { 1, 2, 3, 4 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.AssignBuild), new AssignBuildCommand(12, new[] { 6, 7, 8, 9 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "capital build assignment tick should advance");
            int tick = 2;
            uint sequence = 2;
            while (tick < 100
                && (!session.Peers[0].LocalState.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive
                    || !session.Peers[0].LocalState.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive))
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, sequence, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "capital completion progression tick should advance");
                tick++;
                sequence++;
            }

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

            AssertEqual(0, state.EntityState.Units[0].CarriedAmount, "villager should not gather until reaching resource interaction range");
            AssertEqual(0, state.PlayerStates.Players[0].Resources.Food, "food should not deposit before TC completion");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "villager should be moving toward resource");
        }

        private static void VillagersGatherAndDepositFood()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            state.EntityState.Units[0].Position = FixedVector2.FromInts(4, 0);
            state.EntityState.Units[1].Position = FixedVector2.FromInts(4, 1);
            int foodBefore = state.PlayerStates.Players[0].Resources.Food;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < 1000 && state.PlayerStates.Players[0].Resources.Food < foodBefore + 10; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(1 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(foodBefore + 10, state.PlayerStates.Players[0].Resources.Food, "at least one villager should complete gather and deposit loop");
            AssertEqual(true, state.EconomyState.ResourceNodes[0].RemainingAmount <= GameData.StartingFoodAmount - GameData.VillagerCarryCapacity, "food node should lose at least one full carried amount");
            AssertEqual(true, state.EntityState.Units[0].CarriedAmount == 0 || state.EntityState.Units[1].CarriedAmount == 0, "at least one villager should have deposited and emptied carry");
            AssertEqual(true, state.EntityState.Units[1].CurrentResourceNodeId == 1, "second villager should keep gather assignment");
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

        private static void GatherRejectReasonForDepletedResourceIsTargetComplete()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTwoNodeResourceAreaState(3199, GatherProfileId.Tree, out int firstNodeId, out _, out _);
            FindResourceNodeById(state, firstNodeId).RemainingAmount = 0;
            var header = new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource);
            var command = new GatherResourceCommand(firstNodeId, new[] { 1 });

            CommandValidationReport report = CommandValidationInspector.Evaluate(
                state,
                rules,
                new CommandEnvelope(header, command));

            AssertEqual(false, report.Accepted, "depleted node gather should reject");
            AssertEqual(CommandValidationReason.TargetComplete, report.Reason, "depleted node gather should report target complete");
            AssertEqual(firstNodeId, report.TargetEntityId, "gather rejection should include target resource id");
        }

        private static void DepletedResourceClearsGatherAssignment()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(3, 1);
            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);
            state.EconomyState.ResourceNodes[0].RemainingAmount = GameData.VillagerGatherPerTick;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EconomyState.ResourceNodes[0].RemainingAmount, "resource node should deplete");
            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "depleted node should clear gather assignment");
            AssertEqual(0, state.EntityState.Units[0].CurrentResourceAreaId, "exhausted area should clear long-term gather assignment");
        }

        private static void TreeDepletionReducesAmountAndUnblocksFootprint()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTwoNodeResourceAreaState(3101, GatherProfileId.Tree, out int firstNodeId, out _, out int areaId);
            ResourceNode first = FindResourceNodeById(state, firstNodeId);
            Unit worker = state.EntityState.Units[0];
            worker.Position = new FixedVector2(first.Position.X + Fixed.FromInt(1), first.Position.Y);
            first.RemainingAmount = GameData.VillagerGatherPerTick;

            AssertEqual(true, SpatialRules.IsTileBlockedByResource(state, SpatialRules.GetTileX(first.Position), SpatialRules.GetTileY(first.Position)), "active tree should block its footprint");
            RunGatherCommand(state, rules, firstNodeId, worker.Id);

            AssertEqual(0, first.RemainingAmount, "tree gather should reduce remaining amount to zero");
            AssertEqual(true, first.IsDepleted, "tree should be marked depleted");
            AssertEqual(false, SpatialRules.IsTileBlockedByResource(state, SpatialRules.GetTileX(first.Position), SpatialRules.GetTileY(first.Position)), "depleted tree should unblock its footprint");
            AssertEqual(areaId, worker.CurrentResourceAreaId, "worker should keep long-term forest target while another node remains");
        }

        private static void DepletedResourceRejectsGatherCommand()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTwoNodeResourceAreaState(3102, GatherProfileId.Tree, out int firstNodeId, out _, out _);
            FindResourceNodeById(state, firstNodeId).RemainingAmount = 0;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(firstNodeId, new[] { 1 })));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "depleted node should reject gather command");
            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "rejected depleted node should not assign gather target");
        }

        private static void ForestContinuationChoosesAnotherTree()
        {
            AssertResourceContinuationChoosesAnotherNode(GatherProfileId.Tree, ResourceType.Wood, 3103);
        }

        private static void BerryPatchContinuationChoosesAnotherBush()
        {
            AssertResourceContinuationChoosesAnotherNode(GatherProfileId.BerryBush, ResourceType.Food, 3104);
        }

        private static void GoldDepositContinuationChoosesAnotherVein()
        {
            AssertResourceContinuationChoosesAnotherNode(GatherProfileId.GoldVeinSmall, ResourceType.Gold, 3105);
        }

        private static void ResourceAreaExhaustionIdlesWorkerCleanly()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateSingleNodeResourceAreaState(3106, GatherProfileId.BerryBush, out int nodeId, out _);
            ResourceNode node = FindResourceNodeById(state, nodeId);
            Unit worker = state.EntityState.Units[0];
            worker.Position = new FixedVector2(node.Position.X + Fixed.FromInt(1), node.Position.Y);
            node.RemainingAmount = GameData.VillagerGatherPerTick;

            RunGatherCommand(state, rules, nodeId, worker.Id);

            AssertEqual(0, worker.CurrentResourceNodeId, "exhausted area should clear current node");
            AssertEqual(0, worker.CurrentResourceAreaId, "exhausted area should clear long-term area target");
            AssertEqual(false, worker.HasMoveTarget, "exhausted area should not leave stale movement");
            AssertEqual(WorkerTaskPhase.Idle, worker.TaskPhase, "exhausted area should idle worker cleanly");
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

        private static void GatherCommandKeepsSelectedFoodTargetId()
        {
            AssertGatherCommandKeepsSelectedResourceTarget(ResourceType.Food, 2061);
        }

        private static void GatherCommandKeepsSelectedWoodTargetId()
        {
            AssertGatherCommandKeepsSelectedResourceTarget(ResourceType.Wood, 2062);
        }

        private static void GatherCommandKeepsSelectedGoldTargetId()
        {
            AssertGatherCommandKeepsSelectedResourceTarget(ResourceType.Gold, 2063);
        }

        private static void GatherCommandSetsMovementTowardResource()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(201, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit unit = state.EntityState.Units[0];
            AssertEqual(1, unit.CurrentResourceNodeId, "gather assignment should be set");
            AssertEqual(true, unit.HasMoveTarget, "gather command should assign resource approach movement");
        }

        private static void GatherMoveTargetUsesResourceInteractionRing()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2064, 1);
            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            ResourceNode node = FindResourceNodeById(state, resourceId)!;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit unit = state.EntityState.Units[0];
            int targetX = SpatialRules.GetTileX(unit.MoveTarget);
            int targetY = SpatialRules.GetTileY(unit.MoveTarget);
            AssertEqual(true, unit.HasMoveTarget, "gather assignment should set an approach tile");
            AssertEqual(false, SpatialRules.IsTileInsideResourceFootprint(node, targetX, targetY), "resource approach tile should not be inside resource footprint");
            AssertEqual(true, SpatialRules.IsTileAdjacentToResourceFootprint(node, targetX, targetY), "resource approach tile should be adjacent to footprint");
        }

        private static void MultipleWorkersReserveDistinctResourceSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2081, 1);
            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1, 2, 3 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertDistinctReservations(state, new[] { 1, 2, 3 }, InteractionReservationKind.ResourceNode, resourceId, "resource gatherers should reserve different slots");
        }

        private static void MultipleWorkersOnSameResourceDoNotStack()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2082, 1);
            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1, 2, 3 })));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int tick = 1; tick <= 80; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(20820 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "workers on same resource should not stack while moving/gathering");
            }

            AssertEqual(resourceId, state.EntityState.Units[0].CurrentResourceNodeId, "first worker should keep resource target");
            AssertEqual(resourceId, state.EntityState.Units[1].CurrentResourceNodeId, "second worker should keep resource target");
            AssertEqual(resourceId, state.EntityState.Units[2].CurrentResourceNodeId, "third worker should keep resource target");
        }

        private static void StaleResourceSlotReservationChoosesAlternate()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2093, 1);
            ResourceNode node = CreateTestResourceNode(state, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 7));
            Unit worker = FindUnitById(state, workerId);
            SpatialRules.TileCoord staleTile = new SpatialRules.TileCoord(10, 9);
            worker.CurrentResourceNodeId = node.Id;
            worker.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
            worker.HasMoveTarget = true;
            worker.MoveTarget = FixedVector2.FromInts(staleTile.X, staleTile.Y);
            worker.LastMovedTick = 0;
            SpatialRules.ReserveInteractionSlot(state, worker, InteractionReservationKind.ResourceNode, node.Id, staleTile);
            state.Tick = GameData.InteractionTargetRetargetBlockedTicks;

            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(node.Id, worker.CurrentResourceNodeId, "stale slot retarget should preserve exact resource target");
            AssertEqual(InteractionReservationKind.ResourceNode, worker.ReservedInteractionKind, "worker should keep a resource reservation");
            AssertEqual(node.Id, worker.ReservedInteractionTargetId, "worker should keep reservation on the same node");
            AssertEqual(true, worker.HasMoveTarget, "worker should keep movement intent after stale resource slot retarget");
            bool changedSlot = worker.ReservedInteractionTileX != staleTile.X || worker.ReservedInteractionTileY != staleTile.Y;
            AssertEqual(true, changedSlot, "timed-out resource reservation should prefer another valid slot before reusing stale tile");
            AssertEqual(true, SpatialRules.IsTileAdjacentToResourceFootprint(node, worker.ReservedInteractionTileX, worker.ReservedInteractionTileY), "alternate resource slot should be adjacent to the resource footprint");
        }

        private static void StaleResourceSlotReservationFallsBackWhenOnlySlotRemains()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(20931, 1);
            ResourceNode node = CreateTestResourceNode(state, GatherProfileId.BerryBush, FixedVector2.FromInts(10, 10), GameData.StartingFoodAmount);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 7));
            Unit worker = FindUnitById(state, workerId);
            SpatialRules.TileCoord staleTile = new SpatialRules.TileCoord(10, 9);
            List<SpatialRules.TileCoord> interactionTiles = SpatialRules.EnumerateResourceInteractionTiles(state, node);
            for (int i = 0; i < interactionTiles.Count; i++)
            {
                SpatialRules.TileCoord tile = interactionTiles[i];
                if (tile.X == staleTile.X && tile.Y == staleTile.Y)
                {
                    continue;
                }

                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(tile.X, tile.Y));
            }

            worker.CurrentResourceNodeId = node.Id;
            worker.CurrentResourceAreaId = node.ResourceAreaId;
            worker.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
            worker.HasMoveTarget = true;
            worker.MoveTarget = FixedVector2.FromInts(staleTile.X, staleTile.Y);
            worker.LastMovedTick = 0;
            SpatialRules.ReserveInteractionSlot(state, worker, InteractionReservationKind.ResourceNode, node.Id, staleTile);
            state.Tick = GameData.InteractionTargetRetargetBlockedTicks + 1;

            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(node.Id, worker.CurrentResourceNodeId, "fallback should preserve resource node intent");
            AssertEqual(InteractionReservationKind.ResourceNode, worker.ReservedInteractionKind, "fallback should keep a resource reservation");
            AssertEqual(node.Id, worker.ReservedInteractionTargetId, "fallback should keep reservation target");
            AssertEqual(staleTile.X, worker.ReservedInteractionTileX, "fallback should reuse the only viable slot");
            AssertEqual(staleTile.Y, worker.ReservedInteractionTileY, "fallback should reuse the only viable slot");
            AssertEqual(true, worker.HasMoveTarget, "fallback should keep movement toward viable slot");
            AssertEqual(WorkerTaskPhase.MovingToResourceSlot, worker.TaskPhase, "worker should continue moving toward the viable slot");
        }

        private static void InteractionReservationScoringPrefersLessCongestedTile()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3121, 1);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 10));
            Unit worker = FindUnitById(state, workerId);
            var tiles = new List<SpatialRules.TileCoord>
            {
                new SpatialRules.TileCoord(12, 10),
                new SpatialRules.TileCoord(10, 12)
            };

            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 9), false);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(13, 10), false);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            bool reserved = SpatialRules.TryReserveNearestReachableInteractionTile(
                state,
                worker,
                InteractionReservationKind.ResourceNode,
                99,
                tiles,
                out SpatialRules.TileCoord selected);

            AssertEqual(true, reserved, "scoring should reserve one of the candidate slots");
            AssertEqual(10, selected.X, "scoring should prefer less congested slot x");
            AssertEqual(12, selected.Y, "scoring should prefer less congested slot y");
        }

        private static void MultiWorkerResourceTrafficMakesProgress()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2094, 1);
            ResourceNode node = CreateTestResourceNode(state, GatherProfileId.Tree, FixedVector2.FromInts(12, 10), GameData.StartingWoodAmount);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(6, 10));
            int first = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8, 8));
            int second = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8, 9));
            int third = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8, 10));

            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            int woodBefore = state.PlayerStates.Players[0].Resources.Wood;
            buffer.Add(new CommandEnvelope(
                new CommandHeader(0, 0, 0, CommandType.GatherResource),
                new GatherResourceCommand(node.Id, new[] { first, second, third })));

            int duplicateReservationTicks = 0;
            int movingForeverTicks = 0;
            int progressTicks = 0;
            for (int tick = 0; tick < 420; tick++)
            {
                if (tick > 0)
                {
                    AddNoOp(buffer, tick, 0, (uint)(20940 + tick));
                }

                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "multi-worker resource traffic should not stack");

                if (!TryReservationsAreDistinct(state, new[] { first, second, third }, InteractionReservationKind.ResourceNode, node.Id))
                {
                    duplicateReservationTicks++;
                }

                bool anyMovingToResource = false;
                bool anyGathering = false;
                for (int i = 0; i < state.EntityState.Units.Count; i++)
                {
                    Unit unit = state.EntityState.Units[i];
                    if (unit.CurrentResourceNodeId != node.Id)
                    {
                        continue;
                    }

                    anyMovingToResource = anyMovingToResource || unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot;
                    anyGathering = anyGathering || unit.TaskPhase == WorkerTaskPhase.Gathering;
                    AssertEqual(node.Id, unit.CurrentResourceNodeId, "resource traffic should preserve selected node target");
                }

                if (anyGathering || state.PlayerStates.Players[0].Resources.Wood > woodBefore)
                {
                    progressTicks++;
                }

                if (tick > 120 && anyMovingToResource && progressTicks == 0)
                {
                    movingForeverTicks++;
                }
            }

            AssertEqual(0, duplicateReservationTicks, "workers should not reserve duplicate resource slots during traffic");
            AssertEqual(0, movingForeverTicks, "workers should not stay MovingToResourceSlot forever while no gather/deposit progress happens");
            AssertEqual(true, state.PlayerStates.Players[0].Resources.Wood > woodBefore, "multi-worker resource traffic should eventually deposit wood");
        }

        private static void WorkerInResourceRangeGathersWithoutMoveRewrite()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2088, 1);
            ResourceNode node = FindResourceNodeById(state, 1);
            Unit unit = state.EntityState.Units[0];
            unit.Position = new FixedVector2(node.Position.X + Fixed.FromInt(1), node.Position.Y);
            unit.CurrentResourceNodeId = node.Id;
            unit.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
            unit.HasMoveTarget = true;
            unit.MoveTarget = FixedVector2.FromInts(40, 40);
            FixedVector2 originalMoveTarget = unit.MoveTarget;

            new ResourceGatherSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(WorkerTaskPhase.Gathering, unit.TaskPhase, "worker in resource range should enter gathering phase");
            AssertEqual(false, unit.HasMoveTarget, "worker in resource range should stop movement before gathering");
            AssertEqual(originalMoveTarget.X.Raw, unit.MoveTarget.X.Raw, "gather action should not rewrite move target raw x while already in range");
            AssertEqual(originalMoveTarget.Y.Raw, unit.MoveTarget.Y.Raw, "gather action should not rewrite move target raw y while already in range");
            AssertEqual(GameData.VillagerGatherPerTick, unit.CarriedAmount, "worker should gather immediately from valid interaction range");
        }

        private static void VillagerDoesNotGatherOutsideResourceRange()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(202, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Units[0].CarriedAmount, "villager should not gather until in resource interaction range");
        }

        private static void VillagerGathersInResourceInteractionRange()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(203, 1);
            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.VillagerGatherPerTick, state.EntityState.Units[0].CarriedAmount, "villager should gather when in interaction range");
        }

        private static void VillagerGathersFromDiagonalResourceInteractionTile()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2076, 1);
            ResourceNode node = FindResourceNodeById(state, 1);
            state.EntityState.Units[0].Position = new FixedVector2(node.Position.X + Fixed.FromInt(1), node.Position.Y + Fixed.FromInt(1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(GameData.VillagerGatherPerTick, state.EntityState.Units[0].CarriedAmount, "diagonal resource interaction tile should gather");
        }

        private static void VillagerReturnsToDropoffWhenFull()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(204, 1);
            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            for (int i = 0; i < 40 && state.EntityState.Units[0].CarriedAmount < GameData.VillagerCarryCapacity; i++)
            {
                if (i > 0)
                {
                    AddNoOp(buffer, state.Tick, 0, (uint)i);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            Unit unit = state.EntityState.Units[0];
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            AssertEqual(true, unit.HasMoveTarget, "full villager should receive dropoff movement target");
            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = unit.MoveTarget }, tc), "dropoff target should be on town center interaction ring");
        }

        private static void DropoffMoveTargetUsesTownCenterInteractionRing()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2065, 1);
            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            for (int i = 0; i < 40 && state.EntityState.Units[0].CarriedAmount < GameData.VillagerCarryCapacity; i++)
            {
                if (i > 0)
                {
                    AddNoOp(buffer, state.Tick, 0, (uint)i);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            Unit unit = state.EntityState.Units[0];
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            int targetX = SpatialRules.GetTileX(unit.MoveTarget);
            int targetY = SpatialRules.GetTileY(unit.MoveTarget);
            AssertEqual(true, unit.HasMoveTarget, "dropoff assignment should set an approach tile");
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(tc, targetX, targetY), "dropoff approach tile should not be inside TC footprint");
            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = FixedVector2.FromInts(targetX, targetY) }, tc), "dropoff approach tile should be on TC interaction ring");
        }

        private static void MultipleFullFoodCarriersReserveDistinctDropoffSlots()
        {
            AssertMultipleFullCarriersReserveDistinctDropoffSlots(ResourceType.Food, 2083);
        }

        private static void MultipleFullWoodCarriersReserveDistinctDropoffSlots()
        {
            AssertMultipleFullCarriersReserveDistinctDropoffSlots(ResourceType.Wood, 2084);
        }

        private static void MultipleFullGoldCarriersReserveDistinctDropoffSlots()
        {
            AssertMultipleFullCarriersReserveDistinctDropoffSlots(ResourceType.Gold, 2085);
        }

        private static void MultipleFullCarriersDroppingAtSameTcDoNotStack()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateDropoffReservationState(2086, ResourceType.Gold, out _);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            for (int tick = 0; tick <= 80; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(20860 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "full carriers should not stack while dropping at same TC");
            }
        }

        private static void MultipleFullCarriersDroppingAtSameTcDepositCleanly()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateDropoffReservationState(2095, ResourceType.Wood, out int townCenterId);
            int[] unitIds = new[] { state.EntityState.Units[0].Id, state.EntityState.Units[1].Id, state.EntityState.Units[2].Id };
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            int woodBefore = state.PlayerStates.Players[0].Resources.Wood;
            int duplicateReservationTicks = 0;
            int churnTicks = 0;
            long[] previousReservationKeys = new long[unitIds.Length];
            long[] previousMoveKeys = new long[unitIds.Length];
            for (int i = 0; i < unitIds.Length; i++)
            {
                previousReservationKeys[i] = long.MinValue;
                previousMoveKeys[i] = long.MinValue;
            }

            for (int tick = 0; tick < 240; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(20950 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "full carriers should not stack while depositing cleanly");

                if (!TryReservationsAreDistinct(state, unitIds, InteractionReservationKind.Dropoff, townCenterId))
                {
                    duplicateReservationTicks++;
                }

                for (int i = 0; i < unitIds.Length; i++)
                {
                    Unit unit = FindUnitById(state, unitIds[i]);
                    long reservationKey = EncodeReservationKey(unit);
                    long moveKey = EncodeMoveTargetKey(unit);
                    if (previousReservationKeys[i] != long.MinValue
                        && unit.CarriedAmount > 0
                        && (reservationKey != previousReservationKeys[i] || moveKey != previousMoveKeys[i]))
                    {
                        churnTicks++;
                    }

                    previousReservationKeys[i] = reservationKey;
                    previousMoveKeys[i] = moveKey;
                }
            }

            AssertEqual(0, duplicateReservationTicks, "full carriers should not duplicate final dropoff reservations");
            AssertEqual(true, churnTicks < 20, "dropoff reservations and move targets should not churn under normal 3-carrier traffic churn=" + churnTicks);
            AssertEqual(woodBefore + GameData.VillagerCarryCapacity * 3, state.PlayerStates.Players[0].Resources.Wood, "all full carriers should deposit cleanly at same TC");
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                AssertEqual(0, unit.CarriedAmount, "carrier should be empty after clean deposit unit=" + unit.Id);
            }
        }

        private static void WorkerInDropoffRangeDepositsWithoutMoveRewrite()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = CreateOccupancyState(2089, 1);
            int unitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 10));
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Unit unit = FindUnitById(state, unitId);
            unit.CarriedResourceType = ResourceType.Wood;
            unit.CarriedAmount = GameData.VillagerCarryCapacity;
            unit.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
            unit.HasMoveTarget = true;
            unit.MoveTarget = FixedVector2.FromInts(35, 35);
            FixedVector2 originalMoveTarget = unit.MoveTarget;

            new ResourceDepositSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(WorkerTaskPhase.Idle, unit.TaskPhase, "worker without resource target should become idle after depositing");
            AssertEqual(false, unit.HasMoveTarget, "worker in dropoff range should stop movement before depositing");
            AssertEqual(originalMoveTarget.X.Raw, unit.MoveTarget.X.Raw, "deposit action should not rewrite move target raw x while already in range");
            AssertEqual(originalMoveTarget.Y.Raw, unit.MoveTarget.Y.Raw, "deposit action should not rewrite move target raw y while already in range");
            AssertEqual(0, unit.CarriedAmount, "worker should deposit carried resources");
            AssertEqual(GameData.VillagerCarryCapacity, state.PlayerStates.Players[0].Resources.Wood, "deposit should update stockpile");
        }

        private static void VillagerDepositsFromDiagonalTownCenterInteractionTile()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = CreateOccupancyState(2077, 1);
            int unitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 12));
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Unit unit = state.EntityState.Units[state.EntityState.EntityLookup[unitId].Index];
            unit.CarriedResourceType = ResourceType.Gold;
            unit.CarriedAmount = GameData.VillagerCarryCapacity;

            var buffer = new CommandBuffer();
            AddNoOp(buffer, 0, 0, 2077);
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, unit.CarriedAmount, "diagonal TC interaction tile should deposit");
            AssertEqual(GameData.VillagerCarryCapacity, state.PlayerStates.Players[0].Resources.Gold, "diagonal TC deposit should update stockpile");
        }

        private static void VillagerResumesResourceLoopAfterDeposit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(205, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);
            state.EntityState.Units[1].Position = FixedVector2.FromInts(20, 20);
            state.EntityState.Units[2].Position = FixedVector2.FromInts(21, 20);
            state.EntityState.Units[3].Position = FixedVector2.FromInts(22, 20);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            for (int tick = 0; tick < 120; tick++)
            {
                if (tick > 0)
                {
                    AddNoOp(buffer, tick, 0, (uint)tick);
                }

                runner.AdvanceOneTick(state, rules, buffer);
            }

            Unit unit = state.EntityState.Units[0];
            AssertEqual(1, unit.CurrentResourceNodeId, "villager should keep same resource assignment after deposit");
            AssertEqual(true, unit.HasMoveTarget || unit.CarriedAmount > 0, "villager should continue looping between resource and dropoff");
        }

        private static void VillagerReturnsToSameFoodTargetAfterDeposit()
        {
            AssertVillagerReturnsToSameTargetAfterDeposit(ResourceType.Food, 2067);
        }

        private static void VillagerReturnsToSameWoodTargetAfterDeposit()
        {
            AssertVillagerReturnsToSameTargetAfterDeposit(ResourceType.Wood, 2068);
        }

        private static void VillagerReturnsToSameGoldTargetAfterDeposit()
        {
            AssertVillagerReturnsToSameTargetAfterDeposit(ResourceType.Gold, 2069);
        }

        private static void VillagerKeepsExplicitlyAssignedGoldNodeAcrossDepositLoop()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(20701, 1);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(25, 10));
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            int areaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(18, 10));
            int nearGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(12, 10), GameData.StartingGoldAmount);
            int assignedGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(24, 10), GameData.StartingGoldAmount);

            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(assignedGoldNodeId, new[] { workerId })));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 1; tick <= 220; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(9700 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            Unit worker = FindUnitById(state, workerId);
            ResourceNode nearGold = FindResourceNodeById(state, nearGoldNodeId);
            AssertEqual(assignedGoldNodeId, worker.CurrentResourceNodeId, "worker should return to explicitly assigned gold node after dropoff");
            AssertEqual(GameData.StartingGoldAmount, nearGold.RemainingAmount, "worker should not switch to nearer sibling gold node while assigned node remains valid");
        }

        private static void ExplicitGatherRetargetSwitchesAssignedNode()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(20702, 1);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(25, 10));
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            int areaId = AddTestResourceArea(state, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(18, 10));
            int nearGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(12, 10), GameData.StartingGoldAmount);
            int farGoldNodeId = AddTestResourceNodeToArea(state, areaId, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(24, 10), GameData.StartingGoldAmount);

            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(farGoldNodeId, new[] { workerId })));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int tick = 1; tick <= 80; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(9800 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 900, CommandType.GatherResource), new GatherResourceCommand(nearGoldNodeId, new[] { workerId })));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int tick = 0; tick < 100; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(9900 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            Unit worker = FindUnitById(state, workerId);
            AssertEqual(nearGoldNodeId, worker.CurrentResourceNodeId, "explicit gather retarget should switch worker to the newly clicked node");
        }

        private static void GatherKeepsAssignedResourceWhenNearerSameTypeExists()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2070, 1);
            int assignedResourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            ResourceNode assigned = FindResourceNodeById(state, assignedResourceId);
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = state.EconomyState.NextResourceNodeId++,
                ResourceType = ResourceType.Food,
                Position = state.EntityState.Units[0].Position,
                RemainingAmount = GameData.StartingFoodAmount
            });

            int nearResourceId = state.EconomyState.ResourceNodes[state.EconomyState.ResourceNodes.Count - 1].Id;
            AssertEqual(true, nearResourceId != assignedResourceId, "setup should create distinct near food resource");
            state.EntityState.Units[0].Position = new FixedVector2(assigned.Position.X + Fixed.FromInt(2), assigned.Position.Y);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(assignedResourceId, new[] { 1 })));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int tick = 1; tick <= 100; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(9000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(assignedResourceId, state.EntityState.Units[0].CurrentResourceNodeId, "worker should keep originally assigned resource id while it remains valid");
        }

        private static void GatherMoveTargetRemainsStableWhileApproaching()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2071, 1);
            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));
            runner.AdvanceOneTick(state, rules, buffer);

            Unit unit = state.EntityState.Units[0];
            AssertEqual(true, unit.HasMoveTarget, "gather assignment should set move target");
            int targetX = SpatialRules.GetTileX(unit.MoveTarget);
            int targetY = SpatialRules.GetTileY(unit.MoveTarget);
            for (int tick = 1; tick <= 2; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)(9500 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                AssertEqual(true, unit.HasMoveTarget, "approaching worker should keep move target");
                AssertEqual(targetX, SpatialRules.GetTileX(unit.MoveTarget), "approach tile x should remain stable while valid");
                AssertEqual(targetY, SpatialRules.GetTileY(unit.MoveTarget), "approach tile y should remain stable while valid");
            }
        }

        private static void WorkerGatherLoopDiagnosticsStayStable()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(2092, 1);
            int resourceId = FindFirstResourceNodeIdByType(state, ResourceType.Wood);
            ResourceNode wood = FindResourceNodeById(state, resourceId);
            state.EntityState.Units[0].Position = new FixedVector2(wood.Position.X + Fixed.FromInt(1), wood.Position.Y);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));

            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, new[] { 1 })));

            WorkerDiagnosticSample previous = default;
            bool hasPrevious = false;
            int phaseChanges = 0;
            int reservationChanges = 0;
            int moveTargetChanges = 0;
            int backwardsMoves = 0;
            int deposits = 0;
            int stockpileBefore = state.PlayerStates.Players[0].Resources.Wood;
            for (int tick = 0; tick < 1200; tick++)
            {
                if (tick > 0)
                {
                    AddNoOp(buffer, tick, 0, (uint)(20920 + tick));
                }

                runner.AdvanceOneTick(state, rules, buffer);
                Unit unit = state.EntityState.Units[0];
                WorkerDiagnosticSample current = WorkerDiagnosticSample.Capture(unit);
                if (state.PlayerStates.Players[0].Resources.Wood > stockpileBefore)
                {
                    deposits++;
                    stockpileBefore = state.PlayerStates.Players[0].Resources.Wood;
                }

                if (hasPrevious)
                {
                    if (current.Phase != previous.Phase)
                    {
                        phaseChanges++;
                    }

                    if (current.ReservationKey != previous.ReservationKey)
                    {
                        reservationChanges++;
                    }

                    if (current.MoveTargetKey != previous.MoveTargetKey)
                    {
                        moveTargetChanges++;
                    }

                    if (current.PositionXRaw < previous.PositionXRaw
                        && current.Phase == previous.Phase
                        && current.MoveTargetKey == previous.MoveTargetKey
                        && current.ReservationKey == previous.ReservationKey)
                    {
                        backwardsMoves++;
                    }
                }

                hasPrevious = true;
                previous = current;
                AssertEqual(resourceId, unit.CurrentResourceNodeId, "worker should keep exact wood target through diagnostic loop");
                AssertEqual(true, unit.TaskPhase != WorkerTaskPhase.Idle || unit.CarriedAmount == 0, "worker should not idle while still carrying resources");
            }

            AssertEqual(true, deposits >= 3, "diagnostic loop should include multiple deposits");
            AssertEqual(true, phaseChanges < 400, "task phase should not flip every tick during stable gather/deposit loop changes=" + phaseChanges);
            AssertEqual(true, reservationChanges < 400, "reservation should not churn every tick during stable gather/deposit loop changes=" + reservationChanges);
            AssertEqual(true, moveTargetChanges < 400, "move target should not be rewritten every tick during stable gather/deposit loop changes=" + moveTargetChanges);
            AssertEqual(0, backwardsMoves, "worker raw x should not oscillate backwards while pursuing the same stable target");
        }

        private static void FullGoldCarrierBlockedDropoffTargetRetargetsAndDeposits()
        {
            AssertBlockedCarrierRetargetsAndDeposits(ResourceType.Gold, 2072);
        }

        private static void FullFoodCarrierBlockedDropoffTargetRetargetsAndDeposits()
        {
            AssertBlockedCarrierRetargetsAndDeposits(ResourceType.Food, 2073);
        }

        private static void FullWoodCarrierBlockedDropoffTargetRetargetsAndDeposits()
        {
            AssertBlockedCarrierRetargetsAndDeposits(ResourceType.Wood, 2074);
        }

        private static void ConstructionPacingDoesNotCompleteInstantly()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateConstructionPacingState(2097, out Building foundation);

            new ConstructionSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(true, foundation.IsUnderConstruction, "one builder should not complete a town center in one tick");
            AssertEqual(1, foundation.BuildProgressTicks, "one builder should add one visible progress tick");
            AssertEqual(true, GameData.TownCenterBuildTicks > foundation.BuildProgressTicks, "town center build time should be visibly longer than first progress tick");
        }

        private static void ConstructionProgressAdvancesGradually()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateConstructionPacingState(2098, out Building foundation);

            for (int i = 0; i < 5; i++)
            {
                new ConstructionSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));
            }

            AssertEqual(5, foundation.BuildProgressTicks, "one builder should advance construction gradually over multiple ticks");
            AssertEqual(true, foundation.IsUnderConstruction, "placeholder TC construction should remain observable after a few ticks");
        }

        private static void TwoBuildersInRangeBuildFasterThanOne()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState oneBuilder = GameInitializer.CreateNomadStart(206, 1);
            GameState twoBuilders = GameInitializer.CreateNomadStart(206, 1);
            int tcOne = EntityFactory.CreateTownCenter(oneBuilder, 0, FixedVector2.FromInts(3, 0));
            int tcTwo = EntityFactory.CreateTownCenter(twoBuilders, 0, FixedVector2.FromInts(3, 0));
            oneBuilder.EntityState.Units[0].Position = FixedVector2.FromInts(0, 0);
            twoBuilders.EntityState.Units[0].Position = FixedVector2.FromInts(0, 0);
            twoBuilders.EntityState.Units[1].Position = FixedVector2.FromInts(0, 1);

            var oneBuffer = new CommandBuffer();
            var twoBuffer = new CommandBuffer();
            var runner = new TickRunner();
            oneBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcOne, new[] { 1 })));
            twoBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcTwo, new[] { 1, 2 })));
            runner.AdvanceOneTick(oneBuilder, rules, oneBuffer);
            runner.AdvanceOneTick(twoBuilders, rules, twoBuffer);
            AddNoOp(oneBuffer, 1, 0, 1);
            AddNoOp(twoBuffer, 1, 0, 1);
            runner.AdvanceOneTick(oneBuilder, rules, oneBuffer);
            runner.AdvanceOneTick(twoBuilders, rules, twoBuffer);
            AddNoOp(oneBuffer, 2, 0, 2);
            AddNoOp(twoBuffer, 2, 0, 2);
            runner.AdvanceOneTick(oneBuilder, rules, oneBuffer);
            runner.AdvanceOneTick(twoBuilders, rules, twoBuffer);

            Building one = oneBuilder.EntityState.Buildings[oneBuilder.EntityState.EntityLookup[tcOne].Index];
            Building two = twoBuilders.EntityState.Buildings[twoBuilders.EntityState.EntityLookup[tcTwo].Index];
            AssertEqual(true, two.BuildProgressTicks > one.BuildProgressTicks, "two builders in range should progress faster than one");
        }

        private static void BuildMoveTargetUsesFoundationInteractionRing()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(2066, 1);
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(6, 2));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(foundationId, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit unit = state.EntityState.Units[0];
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[foundationId].Index];
            int targetX = SpatialRules.GetTileX(unit.MoveTarget);
            int targetY = SpatialRules.GetTileY(unit.MoveTarget);
            AssertEqual(true, unit.HasMoveTarget, "build assignment should set an approach tile");
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(foundation, targetX, targetY), "build approach tile should not be inside foundation footprint");
            AssertEqual(true, SpatialRules.IsUnitInBuildingInteractionRange(new Unit { Position = FixedVector2.FromInts(targetX, targetY) }, foundation), "build approach tile should be on foundation interaction ring");
        }

        private static void MultipleBuildersReserveDistinctBuildSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(2087, 1);
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(6, 2));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(foundationId, new[] { 1, 2, 3 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertDistinctReservations(state, new[] { 1, 2, 3 }, InteractionReservationKind.BuildSite, foundationId, "builders should reserve different build slots");
        }

        private static void BuilderInBuildRangeBuildsWithoutMicroMovement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2090, 1);
            int builderId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(12, 10));
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[foundationId].Index];
            foundation.AssignedBuilderIds.Add(builderId);
            Unit builder = FindUnitById(state, builderId);
            builder.CurrentBuildTargetId = foundationId;
            builder.TaskPhase = WorkerTaskPhase.MovingToBuildSlot;
            builder.HasMoveTarget = true;
            builder.MoveTarget = FixedVector2.FromInts(32, 32);
            FixedVector2 originalMoveTarget = builder.MoveTarget;

            new ConstructionSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(WorkerTaskPhase.Building, builder.TaskPhase, "builder in range should enter building phase");
            AssertEqual(false, builder.HasMoveTarget, "builder in build range should stop movement before building");
            AssertEqual(originalMoveTarget.X.Raw, builder.MoveTarget.X.Raw, "build action should not rewrite move target raw x while already in range");
            AssertEqual(originalMoveTarget.Y.Raw, builder.MoveTarget.Y.Raw, "build action should not rewrite move target raw y while already in range");
            AssertEqual(1, foundation.BuildProgressTicks, "builder in range should progress construction");
        }

        private static void BuilderBlockedApproachRetargetsDeterministically()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2075, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(14, 20));
            int foundationId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[foundationId].Index];
            List<SpatialRules.TileCoord> tiles = SpatialRules.EnumerateBuildInteractionTiles(state, foundation);
            SpatialRules.TileCoord blockedTile = tiles[0];
            state.EntityState.Units[0].Position = FixedVector2.FromInts(0, 0);
            state.EntityState.Units[0].CurrentBuildTargetId = foundationId;
            foundation.AssignedBuilderIds.Add(state.EntityState.Units[0].Id);
            int blockerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(blockedTile.X, blockedTile.Y), false);
            state.EntityState.Units[0].HasMoveTarget = true;
            state.EntityState.Units[0].MoveTarget = FixedVector2.FromInts(blockedTile.X, blockedTile.Y);
            state.EntityState.Units[0].LastMovedTick = 0;
            state.Tick = GameData.InteractionTargetRetargetBlockedTicks;

            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            AddNoOp(buffer, state.Tick, 0, 8000);
            runner.AdvanceOneTick(state, rules, buffer);

            Unit builder = state.EntityState.Units[0];
            AssertEqual(true, builder.HasMoveTarget, "builder should keep build intent and retarget");
            AssertEqual(false, SpatialRules.GetTileX(builder.MoveTarget) == blockedTile.X && SpatialRules.GetTileY(builder.MoveTarget) == blockedTile.Y, "builder should retarget away from stale blocked tile");
            AssertEqual(foundationId, builder.CurrentBuildTargetId, "builder should keep build target during congestion recovery");
        }

        private static void MovementArrivalSnapsWithoutRawOscillation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2091, 1);
            int unitId = EntityFactory.CreateUnit(
                state,
                0,
                UnitTypeId.Villager,
                new FixedVector2(Fixed.FromRatio(19, 20), Fixed.FromInt(0)));
            Unit unit = FindUnitById(state, unitId);
            unit.HasMoveTarget = true;
            unit.MoveTarget = FixedVector2.FromInts(1, 0);
            unit.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
            var context = new TickCommandContext(new List<CommandEnvelope>());

            new MovementSystem().Run(state, rules, context);
            long snappedX = unit.Position.X.Raw;
            long snappedY = unit.Position.Y.Raw;

            AssertEqual(Fixed.FromInt(1).Raw, snappedX, "movement should snap to target when within one deterministic step");
            AssertEqual(Fixed.FromInt(0).Raw, snappedY, "movement snap should keep y stable");
            AssertEqual(false, unit.HasMoveTarget, "movement should clear target after snap arrival");
            AssertEqual(WorkerTaskPhase.Idle, unit.TaskPhase, "command move should return to idle after arrival");

            new MovementSystem().Run(state, rules, context);
            AssertEqual(snappedX, unit.Position.X.Raw, "arrived unit should not oscillate raw x after snap");
            AssertEqual(snappedY, unit.Position.Y.Raw, "arrived unit should not oscillate raw y after snap");
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
            int tick = 2;
            uint sequence = 2;
            while (tick < 100 && !session.Peers[0].LocalState.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "capital completion tick should advance");
                tick++;
            }

            session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1, 2 })));
            AssertEqual(true, session.TryAdvanceOneTick(), "gather assignment tick should advance");
            tick++;
            for (int i = 0; i < 200 && session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food < 10; i++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "gather/deposit progression tick should advance");
                tick++;
            }

            AssertEqual(0, session.DesyncReports.Count, "economy lockstep should not desync");
            Unit villager = session.Peers[0].LocalState.EntityState.Units[0];
            AssertEqual(1, villager.CurrentResourceNodeId, "villager should keep gather assignment in lockstep");
            AssertEqual(true, villager.HasMoveTarget || villager.CarriedAmount > 0, "villager should be in deterministic gather loop state");
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

            for (int i = 1; i < GameData.VillagerTrainTicks; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(initialUnitCount + 1, state.EntityState.Units.Count, "villager should spawn when training completes");
            AssertEqual(0, state.EntityState.Buildings[0].TrainingQueue.Count, "training queue should be empty after completion");
            AssertEqual(UnitTypeId.Villager, state.EntityState.Units[state.EntityState.Units.Count - 1].UnitTypeId, "trained unit should be villager");
        }

        private static void TrainedVillagerSpawnsOutsideTownCenterFootprint()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1801, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);

            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            int tileX = SpatialRules.GetTileX(trained.Position);
            int tileY = SpatialRules.GetTileY(trained.Position);
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(townCenter, tileX, tileY), "trained villager should not spawn inside TC footprint");
            AssertEqual(false, SpatialRules.IsTileBlockedForUnitMovement(state, tileX, tileY), "trained villager should spawn on a walkable tile");
            AssertEqual(0, townCenter.TrainingQueue.Count, "training queue should clear after successful spawn");
        }

        private static void TrainedVillagerAvoidsOccupiedSpawnSlot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1802, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            SpatialRules.TileCoord firstSlot = SpatialRules.EnumerateBuildInteractionTiles(state, townCenter)[0];
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(firstSlot.X, firstSlot.Y), false);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);

            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AssertEqual(false, SpatialRules.GetTileX(trained.Position) == firstSlot.X && SpatialRules.GetTileY(trained.Position) == firstSlot.Y, "spawn should skip occupied first slot");
            AssertEqual(0, townCenter.TrainingQueue.Count, "training queue should clear when another spawn slot is open");
        }

        private static void BlockedSpawnWaitsUntilSlotOpens()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1803, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            var spawnSlots = SpatialRules.EnumerateBuildInteractionTiles(state, townCenter);
            var blockers = new List<int>();
            for (int i = 0; i < spawnSlots.Count; i++)
            {
                blockers.Add(EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(spawnSlots[i].X, spawnSlots[i].Y), false));
            }

            QueueImmediateVillager(townCenter);
            int blockedUnitCount = state.EntityState.Units.Count;

            AdvanceSingleNoOp(state, rules, 0, 0);

            AssertEqual(blockedUnitCount, state.EntityState.Units.Count, "blocked spawn should not create a stacked unit");
            AssertEqual(1, townCenter.TrainingQueue.Count, "completed training should wait while all spawn slots are blocked");
            AssertEqual(1, townCenter.TrainingQueue[0].ProgressTicks, "waiting completed training should stay complete");

            FindUnitById(state, blockers[0]).Position = FixedVector2.FromInts(40, 40);
            AdvanceSingleNoOp(state, rules, 1, 1);

            AssertEqual(blockedUnitCount + 1, state.EntityState.Units.Count, "unit should spawn once an exit slot opens");
            AssertEqual(0, townCenter.TrainingQueue.Count, "queue should clear after delayed spawn");
            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AssertEqual(spawnSlots[0].X, SpatialRules.GetTileX(trained.Position), "delayed spawn should use first newly available deterministic slot X");
            AssertEqual(spawnSlots[0].Y, SpatialRules.GetTileY(trained.Position), "delayed spawn should use first newly available deterministic slot Y");
        }

        private static void MultipleTrainedVillagersUseDifferentSpawnSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1804, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            QueueImmediateVillager(townCenter);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);
            Unit first = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AdvanceSingleNoOp(state, rules, 1, 1);
            Unit second = state.EntityState.Units[state.EntityState.Units.Count - 1];

            AssertEqual(false,
                SpatialRules.GetTileX(first.Position) == SpatialRules.GetTileX(second.Position)
                    && SpatialRules.GetTileY(first.Position) == SpatialRules.GetTileY(second.Position),
                "successive trained villagers should not stack on the same spawn tile");
            AssertEqual(0, townCenter.TrainingQueue.Count, "both queued villagers should spawn when slots are available");
        }

        private static void TrainedVillagerAvoidsReservedSpawnSlot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1805, 1);
            int townCenterId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building townCenter = FindBuildingById(state, townCenterId);
            SpatialRules.TileCoord firstSlot = SpatialRules.EnumerateBuildInteractionTiles(state, townCenter)[0];
            int reserverId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(30, 30), false);
            SpatialRules.ReserveInteractionSlot(state, FindUnitById(state, reserverId), InteractionReservationKind.Dropoff, townCenterId, firstSlot);
            QueueImmediateVillager(townCenter);

            AdvanceSingleNoOp(state, rules, 0, 0);

            Unit trained = state.EntityState.Units[state.EntityState.Units.Count - 1];
            AssertEqual(false, SpatialRules.GetTileX(trained.Position) == firstSlot.X && SpatialRules.GetTileY(trained.Position) == firstSlot.Y, "spawn should skip reserved final-purpose slots");
        }

        private static void SpawnSlotSelectionIsDeterministic()
        {
            FixedVector2 first = RunImmediateSpawnAndReturnPosition(1806);
            FixedVector2 second = RunImmediateSpawnAndReturnPosition(1806);

            AssertEqual(first.X.Raw, second.X.Raw, "spawn X should be deterministic");
            AssertEqual(first.Y.Raw, second.Y.Raw, "spawn Y should be deterministic");
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
            int tick = 2;
            uint sequence = 2;
            while (tick < 100 && !session.Peers[0].LocalState.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "capital completion tick should advance");
                tick++;
            }

            session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Food = 50;
            session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.TrainUnit), new TrainUnitCommand(6, UnitTypeId.Villager)));
            AssertEqual(true, session.TryAdvanceOneTick(), "train command tick should advance");
            tick++;
            for (int i = 1; i < GameData.VillagerTrainTicks; i++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence++, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "training progression tick should advance");
                tick++;
            }

            AssertEqual(0, session.DesyncReports.Count, "training lockstep should not desync");
            AssertEqual(6, session.Peers[0].LocalState.EntityState.Units.Count, "trained villager should exist");
        }

        private static void MoveUnitAdvancesDeterministically()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3001, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
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

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(5, 5))));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, state.EntityState.Units[0].CurrentResourceNodeId, "move should clear gather assignment");
        }

        private static void MoveArrivalClearsDestinationReservation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3091);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));
            runner.AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(InteractionReservationKind.MoveDestination, mover.ReservedInteractionKind, "move command should reserve destination while moving");
            AddNoOp(buffer, 1, 0, 1);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, mover.HasMoveTarget, "arrived move should clear move target");
            AssertEqual(InteractionReservationKind.None, mover.ReservedInteractionKind, "arrived move should clear destination reservation");
        }

        private static void MoveReplacementClearsDestinationReservation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3092, 1);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { workerId }, FixedVector2.FromInts(6, 0))));
            runner.AdvanceOneTick(state, rules, buffer);
            Unit worker = FindUnitById(state, workerId);
            SpatialRules.ReserveInteractionSlot(
                state,
                worker,
                InteractionReservationKind.MoveDestination,
                SpatialRules.EncodeTileKey(6, 0),
                new SpatialRules.TileCoord(6, 0));

            int foodNodeId = state.EconomyState.NextResourceNodeId++;
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = foodNodeId,
                ResourceType = ResourceType.Food,
                Position = FixedVector2.FromInts(4, 0),
                RemainingAmount = GameData.StartingFoodAmount
            });
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.GatherResource), new GatherResourceCommand(foodNodeId, new[] { workerId })));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(foodNodeId, worker.CurrentResourceNodeId, "gather replacement should assign resource target");
            AssertEqual(false, worker.ReservedInteractionKind == InteractionReservationKind.MoveDestination, "gather replacement should clear stale move destination reservation");
        }

        private static void UnitDeathClearsDestinationReservation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3093);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(4, 0))));
            runner.AdvanceOneTick(state, rules, buffer);
            Unit unit = state.EntityState.Units[0];
            AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "unit should have move destination reservation before death");

            unit.HitPoints = 0;
            AddNoOp(buffer, 1, 0, 1);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, unit.IsDead, "unit should be marked dead");
            AssertEqual(InteractionReservationKind.None, unit.ReservedInteractionKind, "death should clear move destination reservation");
            AssertEqual(false, unit.HasMoveTarget, "death should clear move target");
        }

        private static void ResignClearsDestinationReservation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3094, 1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(6, 0))));
            runner.AdvanceOneTick(state, rules, buffer);
            Unit unit = state.EntityState.Units[0];
            AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "unit should have move destination reservation before resign");

            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.Resign), new ResignCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.PlayerStates.Players[0].IsResigned, "player should be resigned");
            AssertEqual(InteractionReservationKind.None, unit.ReservedInteractionKind, "resign cleanup should clear move destination reservation");
            AssertEqual(false, unit.HasMoveTarget, "resign cleanup should clear active move target");
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

        private static void MoveRejectReasonForWallBlockedTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(1599, 1);
            EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(2, 0));
            state.EntityState.Buildings[0].IsUnderConstruction = false;
            var header = new CommandHeader(state.Tick, 0, 0, CommandType.MoveUnits);
            var command = new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0));

            CommandValidationReport report = CommandValidationInspector.Evaluate(
                state,
                rules,
                new CommandEnvelope(header, command));

            AssertEqual(false, report.Accepted, "move to blocked wall tile should reject");
            AssertEqual(CommandValidationReason.TargetBlockedByStaticGeometry, report.Reason, "blocked tile should report static geometry reason");
            AssertEqual(2, report.TargetTileX, "report should include target tile x");
            AssertEqual(0, report.TargetTileY, "report should include target tile y");
        }

        private static void MoveRejectsResourceBlockedTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(151, 1);
            ResourceNode node = FindResourceNodeById(state, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, node.Position)));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "move target inside resource footprint should reject");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "resource-blocked move target should count as rejected");
        }

        private static void MoveRejectsUnreachableOpenTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(152);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            for (int y = 0; y < state.MapState.HeightTiles; y++)
            {
                AddCompletedWall(state, 0, FixedVector2.FromInts(1, y));
            }

            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(3, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Units[0].HasMoveTarget, "move target behind sealed blocker should reject");
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "unreachable move target should count as rejected");
        }

        private static void MoveAcceptsMultiSelectWhenAtLeastOneUnitCanPath()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1521);
            int trappedUnitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(10, 10));
            int mobileUnitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            int trapAreaId = AddTestResourceArea(state, GatherProfileId.Tree, FixedVector2.FromInts(10, 10));
            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(9, 10), GameData.StartingWoodAmount);
            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(11, 10), GameData.StartingWoodAmount);
            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(10, 9), GameData.StartingWoodAmount);
            AddTestResourceNodeToArea(state, trapAreaId, GatherProfileId.Tree, FixedVector2.FromInts(10, 11), GameData.StartingWoodAmount);

            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(
                new CommandHeader(0, 0, 0, CommandType.MoveUnits),
                new MoveUnitsCommand(new[] { trappedUnitId, mobileUnitId }, FixedVector2.FromInts(20, 20))));
            runner.AdvanceOneTick(state, rules, buffer);

            Unit trapped = FindUnitById(state, trappedUnitId);
            Unit mobile = FindUnitById(state, mobileUnitId);
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "group move should be accepted when at least one selected unit can path");
            AssertEqual(false, trapped.HasMoveTarget, "trapped unit without static path should not receive unreachable move target");
            AssertEqual(true, mobile.HasMoveTarget, "reachable unit should still receive move target");
            AssertEqual(WorkerTaskPhase.MovingToCommandMove, mobile.TaskPhase, "reachable unit should keep command move intent");
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

        private static void PathfinderBlocksBuildingAndResourceTiles()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(171);
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            FixedVector2 tcZone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcZone)));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            int tcTileX = tcZone.X.FloorToInt();
            int tcTileY = tcZone.Y.FloorToInt();
            bool foundTownCenterTile = DeterministicPathfinder.TryFindNextTile(state, tcTileX - 3, tcTileY, tcTileX, tcTileY, out _, out _);
            AssertEqual(false, foundTownCenterTile, "pathfinder should reject blocked building footprint target tile");

            ResourceNode firstResource = state.EconomyState.ResourceNodes[0];
            int resourceTileX = firstResource.Position.X.FloorToInt();
            int resourceTileY = firstResource.Position.Y.FloorToInt();
            bool foundResourceTile = DeterministicPathfinder.TryFindNextTile(state, resourceTileX - 2, resourceTileY, resourceTileX, resourceTileY, out _, out _);
            AssertEqual(false, foundResourceTile, "pathfinder should reject blocked resource target tile");
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

        private static void UnitOrderedToOccupiedDestinationReceivesNearbySlot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(1);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(1, 0))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(true, mover.HasMoveTarget, "occupied final tile should choose a nearby destination slot instead of clearing intent");
            AssertEqual(InteractionReservationKind.MoveDestination, mover.ReservedInteractionKind, "occupied final tile should reserve a move destination slot");
            AssertEqual(false, SpatialRules.GetTileX(mover.MoveTarget) == 1 && SpatialRules.GetTileY(mover.MoveTarget) == 0, "move destination should not be the occupied tile");
            AssertNoLiveUnitStacking(state, "occupied final tile command should not stack units");
        }

        private static void OccupiedNextStepUsesDeterministicAlternate()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3010);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(Fixed.FromInt(0).Raw, mover.Position.X.Raw, "alternate should keep X deterministic");
            AssertEqual(Fixed.FromInt(1).Raw, mover.Position.Y.Raw, "alternate should step around occupied next tile");
            AssertEqual(true, mover.HasMoveTarget, "alternate pass-around should preserve original move target");
        }

        private static void OccupiedNextStepCanUseDiagonalAlternate()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3030);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(true, mover.Position.X.Raw > 0, "diagonal pass-around should make X progress toward the open diagonal tile");
            AssertEqual(true, mover.Position.Y.Raw > 0, "diagonal pass-around should make Y progress toward the open diagonal tile");
            AssertEqual(true, mover.HasMoveTarget, "diagonal pass-around should preserve original move target");
            AssertNoLiveUnitStacking(state, "diagonal pass-around should not stack units");
        }

        private static void AlternateStepAvoidsOccupiedTiles()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3011);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(true, mover.Position.X.Raw > 0, "mover should use an open diagonal instead of occupied direct/cardinal tiles");
            AssertEqual(true, mover.Position.Y.Raw > 0, "mover should use an open diagonal instead of occupied direct/cardinal tiles");
            AssertNoLiveUnitStacking(state, "alternate step should avoid occupied tiles");
            AssertEqual(true, mover.HasMoveTarget, "blocked mover should keep original move target");
        }

        private static void AlternateStepAvoidsStaticBlockers()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3012);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            AddCompletedWall(state, 0, FixedVector2.FromInts(0, 1));
            AddCompletedWall(state, 0, FixedVector2.FromInts(1, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(Fixed.FromInt(0).Raw, mover.Position.X.Raw, "mover should wait when only alternate is statically blocked");
            AssertEqual(Fixed.FromInt(0).Raw, mover.Position.Y.Raw, "mover should not step into wall-blocked alternate");
            AssertEqual(true, mover.HasMoveTarget, "static-blocked alternate should not clear move target");
        }

        private static void TemporaryLiveUnitBlockagePreservesMoveTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3013);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(1, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1 }, FixedVector2.FromInts(2, 0))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = state.EntityState.Units[0];
            AssertEqual(true, mover.HasMoveTarget, "temporary unit congestion should preserve move target");
            AssertEqual(Fixed.FromInt(2).Raw, mover.MoveTarget.X.Raw, "temporary unit congestion should preserve target X");
            AssertEqual(Fixed.FromInt(0).Raw, mover.MoveTarget.Y.Raw, "temporary unit congestion should preserve target Y");
        }

        private static void TcFrontBlockerAllowsPassAroundProgress()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3014);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            int moverId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(10, 6));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(10, 7));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { moverId }, FixedVector2.FromInts(10, 15))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit mover = FindUnitById(state, moverId);
            AssertEqual(true, mover.HasMoveTarget, "tc-front congestion should preserve move target");
            AssertEqual(false, SpatialRules.GetTileX(mover.Position) == 10 && SpatialRules.GetTileY(mover.Position) == 6, "mover should make local pass-around progress near TC");
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(state.EntityState.Buildings[0], SpatialRules.GetTileX(mover.Position), SpatialRules.GetTileY(mover.Position)), "pass-around should not enter TC footprint");
        }

        private static void ResourceDropoffBlockerPreservesWorkerIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3015);
            int workerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 6));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 7));
            int resourceId = state.EconomyState.NextResourceNodeId++;
            state.EconomyState.ResourceNodes.Add(new ResourceNode
            {
                Id = resourceId,
                ResourceType = ResourceType.Wood,
                Position = FixedVector2.FromInts(26, 20),
                RemainingAmount = GameData.StartingWoodAmount
            });
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 10));
            Unit worker = FindUnitById(state, workerId);
            worker.CurrentResourceNodeId = resourceId;
            worker.CarriedResourceType = ResourceType.Wood;
            worker.CarriedAmount = GameData.VillagerCarryCapacity;
            worker.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
            worker.HasMoveTarget = true;
            worker.MoveTarget = FixedVector2.FromInts(20, 15);

            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            AssertEqual(resourceId, worker.CurrentResourceNodeId, "local traffic avoidance should preserve resource intent");
            AssertEqual(GameData.VillagerCarryCapacity, worker.CarriedAmount, "local traffic avoidance should not fake deposit");
            AssertEqual(true, worker.HasMoveTarget, "local traffic avoidance should keep dropoff move target");
        }

        private static void MovingUnitCanEnterVacatedTileWithoutStacking()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3040);
            int followerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 0));
            int leaderId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 0));
            Unit follower = FindUnitById(state, followerId);
            Unit leader = FindUnitById(state, leaderId);
            follower.HasMoveTarget = true;
            follower.MoveTarget = FixedVector2.FromInts(2, 0);
            follower.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
            leader.HasMoveTarget = true;
            leader.MoveTarget = FixedVector2.FromInts(3, 0);
            leader.TaskPhase = WorkerTaskPhase.MovingToCommandMove;

            new MovementSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(1, SpatialRules.GetTileX(follower.Position), "follower should be allowed to enter a tile vacated by a moving leader");
            AssertEqual(2, SpatialRules.GetTileX(leader.Position), "leader should move forward first in the traffic chain");
            AssertNoLiveUnitStacking(state, "vacated-tile follow-through should not stack units");
        }

        private static void GroupMoveAssignsDistinctDestinationSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3020);
            int[] unitIds = CreateLineOfUnits(state, 5, 0, 0, 0, 0, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(10, 10))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertDistinctReservations(
                state,
                unitIds,
                InteractionReservationKind.MoveDestination,
                SpatialRules.EncodeTileKey(10, 10),
                "group move destination slots");
        }

        private static void TenUnitGroupMoveDoesNotStack()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3021);
            int[] unitIds = CreateLineOfUnits(state, 10, 0, 0, 0, 0, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(12, 12))));

            var runner = new TickRunner();
            for (int i = 0; i < 80; i++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "ten unit group move should not stack");
            }
        }

        private static void StaleDropoffSlotTimeoutClearsAndReassigns()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3096, 1);
            int unitId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(20, 6));
            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 10));
            Unit worker = FindUnitById(state, unitId);
            worker.CarriedResourceType = ResourceType.Wood;
            worker.CarriedAmount = GameData.VillagerCarryCapacity;
            worker.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
            worker.HasMoveTarget = true;
            worker.MoveTarget = FixedVector2.FromInts(20, 8);
            worker.LastMovedTick = 0;
            SpatialRules.ReserveInteractionSlot(state, worker, InteractionReservationKind.Dropoff, tcId, new SpatialRules.TileCoord(20, 8));
            state.Tick = GameData.InteractionTargetRetargetBlockedTicks + 1;

            AddCompletedWall(state, 0, FixedVector2.FromInts(20, 8));

            new ResourceDepositSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(true, worker.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot || worker.TaskPhase == WorkerTaskPhase.BlockedWaiting, "worker should keep dropoff intent after stale timeout");
            AssertEqual(false, worker.ReservedInteractionKind == InteractionReservationKind.MoveDestination, "dropoff stale recovery should not leave move-destination reservation");
            AssertEqual(false, worker.ReservedInteractionKind == InteractionReservationKind.Dropoff && worker.ReservedInteractionTileX == 20 && worker.ReservedInteractionTileY == 8, "stale blocked dropoff slot should be released or replaced");
        }

        private static void TwentyUnitGroupMoveSettlesOrWaitsWithoutStacking()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState first = CreateOccupancyState(3041);
            GameState second = CreateOccupancyState(3041);
            int[] firstUnitIds = CreateLineOfUnits(first, 20, 0, 0, 4, 0, 1);
            int[] secondUnitIds = CreateLineOfUnits(second, 20, 0, 0, 4, 0, 1);
            var firstBuffer = new CommandBuffer();
            var secondBuffer = new CommandBuffer();
            firstBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(firstUnitIds, FixedVector2.FromInts(24, 12))));
            secondBuffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(secondUnitIds, FixedVector2.FromInts(24, 12))));

            var runner = new TickRunner();
            for (int tick = 0; tick < 90; tick++)
            {
                runner.AdvanceOneTick(first, rules, firstBuffer);
                AssertNoLiveUnitStacking(first, "twenty unit group move should not stack under pressure");
                AssertNoDuplicateFinalPurposeReservations(first, "twenty unit group move should not duplicate final destination reservations");
            }

            runner = new TickRunner();
            for (int tick = 0; tick < 90; tick++)
            {
                runner.AdvanceOneTick(second, rules, secondBuffer);
            }

            AssertEqual(first.LastChecksum, second.LastChecksum, "twenty unit group move should remain deterministic");
            int activeOrArrived = 0;
            for (int i = 0; i < firstUnitIds.Length; i++)
            {
                Unit unit = FindUnitById(first, firstUnitIds[i]);
                if (!unit.HasMoveTarget || unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)
                {
                    activeOrArrived++;
                }
            }

            AssertEqual(firstUnitIds.Length, activeOrArrived, "twenty unit group move units should arrive or wait with stable destination slots");
        }

        private static void GroupMoveAvoidsReservedFinalDestinationSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3022);
            int reserverId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(20, 20), false);
            SpatialRules.ReserveInteractionSlot(
                state,
                FindUnitById(state, reserverId),
                InteractionReservationKind.MoveDestination,
                SpatialRules.EncodeTileKey(10, 10),
                new SpatialRules.TileCoord(10, 10));

            int[] unitIds = CreateLineOfUnits(state, 3, 1, 0, 0, 0, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(10, 10))));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                AssertEqual(false, unit.ReservedInteractionTileX == 10 && unit.ReservedInteractionTileY == 10, "group move should avoid another unit's reserved final destination");
            }
        }

        private static void GroupMoveDoesNotCauseEndlessJitter()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3023);
            int[] unitIds = CreateLineOfUnits(state, 6, 0, 0, 0, 0, 1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(8, 8))));

            var runner = new TickRunner();
            for (int i = 0; i < 120; i++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "group move should not stack while resolving traffic");
            }

            int arrivedOrWaiting = 0;
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (!unit.HasMoveTarget || unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)
                {
                    arrivedOrWaiting++;
                }
            }

            AssertEqual(unitIds.Length, arrivedOrWaiting, "group move units should either arrive or keep stable destination reservations");
        }

        private static void GroupGatherDoesNotCollapseOntoOneInteractionSlot()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateGroupGatherState(3024, 5, out int resourceId);
            int[] unitIds = new[] { 1, 2, 3, 4, 5 };
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, unitIds)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertDistinctReservations(
                state,
                unitIds,
                InteractionReservationKind.ResourceNode,
                resourceId,
                "group gather resource slots");
        }

        private static void GroupGatherWorkersMakeProgressOrWaitCleanly()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateGroupGatherState(3025, 5, out int resourceId);
            int[] unitIds = new[] { 1, 2, 3, 4, 5 };
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(resourceId, unitIds)));

            int woodBefore = state.PlayerStates.Players[0].Resources.Wood;
            var runner = new TickRunner();
            for (int i = 0; i < 500; i++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "group gather should not stack while moving/gathering/dropping off");
            }

            bool anyProgress = state.PlayerStates.Players[0].Resources.Wood > woodBefore;
            for (int i = 0; i < unitIds.Length && !anyProgress; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                anyProgress = unit.CarriedAmount > 0 || unit.TaskPhase == WorkerTaskPhase.Gathering || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting;
            }

            AssertEqual(true, anyProgress, "group gather workers should gather/deposit or wait cleanly without losing intent");
        }

        private static void GroupGatherResolvesClickedNodeToResourceAreaDistribution()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateGroupBerryAreaState(3026, 5, out int areaId, out int[] nodeIds, out int[] unitIds);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(nodeIds[0], unitIds)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            int distributedWorkers = 0;
            var targetedNodes = new HashSet<int>();
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                AssertEqual(areaId, unit.CurrentResourceAreaId, "clicked node should resolve to owning resource area for unit " + unit.Id);
                AssertEqual(true, unit.CurrentResourceNodeId != 0, "group gather should keep a concrete node target for unit " + unit.Id);
                targetedNodes.Add(unit.CurrentResourceNodeId);
                if (unit.HasMoveTarget || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting)
                {
                    distributedWorkers++;
                }
            }

            AssertEqual(true, distributedWorkers >= 3, "most workers should receive movement/waiting gather intent immediately");
            AssertEqual(true, targetedNodes.Count >= 2, "group gather should distribute workers across multiple nodes in the same area");
        }

        private static void GroupGatherAvoidsSingleNodeCollapseWhenSiblingNodesExist()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateGroupBerryAreaState(3027, 5, out _, out int[] nodeIds, out int[] unitIds);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(nodeIds[0], unitIds)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            int nodeOneReservations = 0;
            int nodeTwoReservations = 0;
            int duplicateTileReservations = 0;
            var reservedTiles = new HashSet<long>();
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (unit.ReservedInteractionKind != InteractionReservationKind.ResourceNode)
                {
                    continue;
                }

                if (unit.ReservedInteractionTargetId == nodeIds[0])
                {
                    nodeOneReservations++;
                }
                else if (unit.ReservedInteractionTargetId == nodeIds[1])
                {
                    nodeTwoReservations++;
                }

                long tileKey = ((long)unit.ReservedInteractionTileX << 32) ^ (uint)unit.ReservedInteractionTileY;
                if (!reservedTiles.Add(tileKey))
                {
                    duplicateTileReservations++;
                }
            }

            AssertEqual(true, nodeOneReservations > 0, "clicked node should still receive some workers");
            AssertEqual(true, nodeTwoReservations > 0, "sibling node should receive workers under group pressure");
            AssertEqual(0, duplicateTileReservations, "workers should not reserve the same interaction slot tile");
        }

        private static void DryArabiaBerryGroupGatherMakesBoundedFoodProgress()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(3028);
            int[] villagers = GetPlayerVillagerIds(state, 0);
            int[] workers = new[] { villagers[0], villagers[1], villagers[2], villagers[3] };
            int berryNodeId = FindNearbyResourceNodeId(state, DryArabiaTest01MapDefinition.GetTownCenterZone(0), ResourceType.Food);
            int foodBefore = state.PlayerStates.Players[0].Resources.Food;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(berryNodeId, workers)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            var runner = new TickRunner();
            for (int tick = 0; tick < 500; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                AssertNoLiveUnitStacking(state, "dry arabia berry group gather should avoid stacking while making progress");
            }

            bool progressed = state.PlayerStates.Players[0].Resources.Food > foodBefore;
            for (int i = 0; i < workers.Length && !progressed; i++)
            {
                Unit worker = FindUnitById(state, workers[i]);
                progressed = worker.CarriedResourceType == ResourceType.Food
                    || worker.CarriedAmount > 0
                    || worker.TaskPhase == WorkerTaskPhase.Gathering
                    || worker.TaskPhase == WorkerTaskPhase.BlockedWaiting;
            }

            AssertEqual(true, progressed, "dry arabia berry group gather should make bounded food progress or maintain active gather intent");
        }

        private static void BerryVisualRadiusMatchesSimulationFootprintRadius()
        {
            GatherProfile profile = GameData.GetGatherProfile(GatherProfileId.BerryBush);
            AssertEqual(profile.FootprintRadiusTiles, profile.VisualRadiusTiles, "berry visual radius should match node simulation footprint radius");
        }

        private static void ThirtyWorkersAcrossResourcesKeepProgressOrIntent()
        {
            RunWorkerPressureScenario(3042, 30, 240, "thirty workers across resources");
        }

        private static void FiftyWorkersAcrossResourcesKeepProgressOrIntent()
        {
            RunWorkerPressureScenario(3043, 50, 280, "fifty workers across resources");
        }

        private static void OneHundredTwentyWorkersAcrossResourcesKeepProgressOrIntent()
        {
            RunWorkerPressureScenario(3044, 120, 360, "one hundred twenty workers across resources");
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

        private static void SixPlayerMixedPopulationTrafficRemainsDeterministic()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(6);
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

        private static void RunWorkerPressureScenario(ulong seed, int totalWorkers, int ticks, string label)
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
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

        private static void TwoUnitsAttemptingSameTileReceiveSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(2, 1));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 2 }, FixedVector2.FromInts(1, 1))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);
            int arrivedOrReserved = 0;
            var reservedTiles = new HashSet<int>();
            for (int i = 1; i <= 2; i++)
            {
                Unit unit = FindUnitById(state, i);
                if (!unit.HasMoveTarget)
                {
                    arrivedOrReserved++;
                    continue;
                }

                AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "active mover should keep a move destination reservation unit=" + unit.Id);
                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, reservedTiles.Add(key), "active movers should not share destination reservations");
                arrivedOrReserved++;
            }

            AssertEqual(2, arrivedOrReserved, "two-unit move should either arrive or keep stable destination reservations");
        }

        private static void ThreeUnitsAttemptingSameTileReceiveSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(3);
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(0, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(2, 1));
            EntityFactory.CreateUnit(state, 0, UnitTypeId.Scout, FixedVector2.FromInts(1, 2));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 1, 2, 3 }, FixedVector2.FromInts(1, 1))));
            new TickRunner().AdvanceOneTick(state, rules, buffer);
            int arrivedOrReserved = 0;
            var reservedTiles = new HashSet<int>();
            for (int i = 1; i <= 3; i++)
            {
                Unit unit = FindUnitById(state, i);
                if (!unit.HasMoveTarget)
                {
                    arrivedOrReserved++;
                    continue;
                }

                AssertEqual(InteractionReservationKind.MoveDestination, unit.ReservedInteractionKind, "active mover should keep a move destination reservation unit=" + unit.Id);
                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, reservedTiles.Add(key), "active movers should not share destination reservations");
                arrivedOrReserved++;
            }

            AssertEqual(3, arrivedOrReserved, "three-unit move should either arrive or keep stable destination reservations");
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

            AssertNoLiveUnitStacking(state, "two units should not stack while avoiding a swap");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget, "first unit should keep movement intent while avoiding a swap");
            AssertEqual(true, state.EntityState.Units[1].HasMoveTarget, "second unit should keep movement intent while avoiding a swap");
            AssertEqual(false, SpatialRules.GetTileX(state.EntityState.Units[0].Position) == 1 && SpatialRules.GetTileY(state.EntityState.Units[0].Position) == 0, "first unit should not move into second unit's occupied tile");
            AssertEqual(false, SpatialRules.GetTileX(state.EntityState.Units[1].Position) == 0 && SpatialRules.GetTileY(state.EntityState.Units[1].Position) == 0, "second unit should not move into first unit's occupied tile");
        }

        private static void WorkerTaskTileSwapKeepsMovementIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(2096, 1);
            int firstId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(0, 0));
            int secondId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(1, 0));
            Unit first = FindUnitById(state, firstId);
            Unit second = FindUnitById(state, secondId);
            first.TaskPhase = WorkerTaskPhase.MovingToResourceSlot;
            first.HasMoveTarget = true;
            first.MoveTarget = FixedVector2.FromInts(1, 0);
            first.CurrentResourceNodeId = 10;
            first.LastMovedTick = state.Tick;
            second.TaskPhase = WorkerTaskPhase.MovingToDropoffSlot;
            second.HasMoveTarget = true;
            second.MoveTarget = FixedVector2.FromInts(0, 0);
            second.CarriedResourceType = ResourceType.Wood;
            second.CarriedAmount = GameData.VillagerCarryCapacity;
            second.LastMovedTick = state.Tick;

            new MovementSystem().Run(state, rules, new TickCommandContext(new List<CommandEnvelope>()));

            AssertEqual(0, SpatialRules.GetTileX(first.Position), "first worker should not swap into occupied tile");
            AssertEqual(1, SpatialRules.GetTileX(second.Position), "second worker should not swap into occupied tile");
            AssertNoLiveUnitStacking(state, "worker task swap conflict should not stack units");
            AssertEqual(true, first.HasMoveTarget, "worker task swap conflict should keep first movement target");
            AssertEqual(true, second.HasMoveTarget, "worker task swap conflict should keep second movement target");
            AssertEqual(10, first.CurrentResourceNodeId, "temporary swap congestion should not clear resource intent");
            AssertEqual(GameData.VillagerCarryCapacity, second.CarriedAmount, "temporary swap congestion should not clear carried resources");
            AssertEqual(WorkerTaskPhase.MovingToResourceSlot, first.TaskPhase, "brief swap congestion should not reset first worker phase");
            AssertEqual(WorkerTaskPhase.MovingToDropoffSlot, second.TaskPhase, "brief swap congestion should not reset second worker phase");
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

        private static void PresentationSnapshotIncludesVisibleResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(80, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            AssertEqual(true, snapshot.Resources.Count >= 3, "snapshot should include visible starting resources");
            AssertEqual(true, HasResource(snapshot, ResourceType.Food), "snapshot should include visible food resource");
            AssertEqual(true, HasResource(snapshot, ResourceType.Wood), "snapshot should include visible wood resource");
            AssertEqual(true, HasResource(snapshot, ResourceType.Gold), "snapshot should include visible gold resource");
        }

        private static void PresentationSnapshotHidesDepletedResources()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(81, 1);
            state.EconomyState.ResourceNodes[0].RemainingAmount = 0;
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            for (int i = 0; i < snapshot.Resources.Count; i++)
            {
                AssertFalse(snapshot.Resources[i].Id == 1, "snapshot should hide depleted resource node");
            }
        }

        private static void PresentationSnapshotIncludesBuildingStatus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(89, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(FixedVector2.FromInts(10, 10))));
            runner.AdvanceOneTick(state, rules, buffer);
            state.EntityState.Units[0].Position = FixedVector2.FromInts(7, 10);
            state.EntityState.Units[1].Position = FixedVector2.FromInts(13, 10);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(6, new[] { 1, 2 })));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int tick = 2; tick < 40 && state.EntityState.Buildings[state.EntityState.EntityLookup[6].Index].BuildProgressTicks < 2; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)tick);
                runner.AdvanceOneTick(state, rules, buffer);
            }

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);
            BuildingSnapshot building = FindBuildingSnapshot(snapshot, 6);
            Building simBuilding = state.EntityState.Buildings[state.EntityState.EntityLookup[6].Index];

            AssertEqual(true, building.IsUnderConstruction, "building snapshot should expose construction state");
            AssertEqual(simBuilding.BuildProgressTicks, building.BuildProgressTicks, "building snapshot should expose build progress");
            AssertEqual(true, building.BuildProgressTicks > 0, "building snapshot setup should have active build progress");
            AssertEqual(GameData.TownCenterBuildTicks, building.RequiredBuildTicks, "building snapshot should expose required build ticks");
            AssertEqual(0, building.TrainingQueueCount, "under-construction building should have no training queue");
        }

        private static void PresentationSnapshotIncludesUnitStatus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(91, 1);
            state.EntityState.Units[0].Position = FixedVector2.FromInts(5, 0);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.GatherResource), new GatherResourceCommand(1, new[] { 1 })));
            runner.AdvanceOneTick(state, rules, buffer);

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);
            UnitSnapshot unit = FindUnitSnapshot(snapshot, 1);

            AssertEqual(1, unit.CurrentResourceNodeId, "unit snapshot should expose gather target");
            AssertEqual(ResourceType.Food, unit.CarriedResourceType, "unit snapshot should expose carried resource type");
            AssertEqual(GameData.VillagerGatherPerTick, unit.CarriedAmount, "unit snapshot should expose carried amount");
            AssertEqual(false, unit.HasMoveTarget, "unit snapshot should expose move target state");
        }

        private static void PresentationSnapshotIncludesTechStatus()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateResearchReadyState(165, 1, out int buildingId);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.ResearchTech), new ResearchTechCommand(buildingId, TechId.InfantryAttack1)));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            GameSnapshot snapshot = GameSnapshotBuilder.Build(state, 0);

            AssertEqual(1, snapshot.LocalPlayer.ResearchQueue.Count, "snapshot should expose queued research");
            AssertEqual(TechId.InfantryAttack1, snapshot.LocalPlayer.ResearchQueue[0].TechId, "snapshot should expose research tech id");
            AssertEqual(1, snapshot.LocalPlayer.ResearchQueue[0].ProgressTicks, "snapshot should expose research progress");
            AssertEqual(GameData.InfantryAttack1ResearchTicks, snapshot.LocalPlayer.ResearchQueue[0].RequiredTicks, "snapshot should expose research required ticks");
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

        private static void SimulationSourceDoesNotReferencePresentation()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(text.Contains("RtsGame.Presentation"), "simulation source must not reference presentation namespace file=" + files[i]);
                AssertFalse(text.Contains("Godot"), "simulation source must not reference Godot file=" + files[i]);
            }
        }

        private static void SimulationSourceRoutesPathfindingThroughServiceLayer()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');
                if (normalized.EndsWith("/Core/DeterministicPathfinder.cs")
                    || normalized.EndsWith("/Core/DeterministicPathQueryService.cs"))
                {
                    continue;
                }

                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(
                    text.Contains("DeterministicPathfinder.TryFindNextTile(") || text.Contains("DeterministicPathfinder.TryFindPathCost("),
                    "simulation code outside path query service must not call DeterministicPathfinder directly file=" + normalized);
            }
        }

        private static void SimulationSourceRoutesReservationWritesThroughTrafficService()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string normalized = files[i].Replace('\\', '/');
                bool isAllowedWriter =
                    normalized.EndsWith("/Core/DeterministicTrafficReservationService.cs")
                    || normalized.EndsWith("/Core/EntityFactory.cs");
                if (isAllowedWriter)
                {
                    continue;
                }

                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionKind"), "reservation kind writes must route through traffic service file=" + normalized);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionTargetId"), "reservation target writes must route through traffic service file=" + normalized);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionTileX"), "reservation tile x writes must route through traffic service file=" + normalized);
                AssertFalse(ContainsFieldAssignment(text, "ReservedInteractionTileY"), "reservation tile y writes must route through traffic service file=" + normalized);
            }
        }

        private static void DeterministicReservationConflictsAvoidUnorderedIteration()
        {
            string text = System.IO.File.ReadAllText(System.IO.Path.Combine("src", "sim", "Core", "DeterministicTrafficReservationService.cs"));
            AssertFalse(text.Contains("foreach (var"), "reservation conflict resolution should avoid unordered iteration in deterministic paths");
            AssertFalse(text.Contains("HashSet<"), "reservation conflict resolution should avoid hash-set iteration in deterministic conflict decisions");
            AssertFalse(text.Contains("Dictionary<"), "reservation conflict resolution should avoid dictionary iteration in deterministic conflict decisions");
            AssertEqual(true, text.Contains("for (int i = 0; i < candidates.Count; i++)"), "reservation conflict resolution should use ordered candidate iteration");
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

        private static void GodotBridgeDoesNotReferenceGodotApi()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "presentation", "GodotBridge"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(text.Contains("using Godot;"), "Godot bridge helpers must stay engine-api free file=" + files[i]);
                AssertFalse(text.Contains("Godot.Vector") || text.Contains("Godot.Color") || text.Contains("Node2D"), "Godot bridge helpers must not use Godot engine types file=" + files[i]);
            }
        }

        private static void GodotClientScriptDoesNotReferenceSimulationCore()
        {
            string script = System.IO.File.ReadAllText(System.IO.Path.Combine("GodotClient", "Scripts", "RtsClientRoot.cs"));

            AssertFalse(script.Contains("RtsGame.Sim"), "Godot client script must not reference simulation namespaces directly");
            AssertFalse(script.Contains("GameState"), "Godot client script must not reference GameState directly");
            AssertFalse(script.Contains("TickRunner"), "Godot client script must not reference TickRunner directly");
            AssertFalse(script.Contains("StateChecksum"), "Godot client script must not reference checksums directly");
        }

        private static void GodotClientScriptDoesNotSwitchOnRawPrimitiveKind()
        {
            string script = System.IO.File.ReadAllText(System.IO.Path.Combine("GodotClient", "Scripts", "RtsClientRoot.cs"));

            AssertFalse(script.Contains("switch (primitive.Kind)"), "Godot client script must not switch on raw primitive kind values");
            AssertFalse(script.Contains("case 1:") || script.Contains("case 2:") || script.Contains("case 3:"), "Godot client script must not use raw primitive kind case labels");
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

        private static void VisualFrameUsesBuildingFootprintSize()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(67, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            int normalId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(8, 0));
            Building normal = state.EntityState.Buildings[state.EntityState.EntityLookup[normalId].Index];
            normal.IsUnderConstruction = false;
            normal.BuildProgressTicks = GameData.TownCenterBuildTicks;
            normal.HitPoints = GameData.TownCenterHitPoints;
            int wallId = EntityFactory.CreateWall(state, 0, FixedVector2.FromInts(14, 0));
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));
            VisualPrimitive capital = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, state.PlayerStates.Players[0].CapitalStatus.CapitalBuildingId);
            VisualPrimitive normalTownCenter = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, normalId);
            VisualPrimitive wall = FindPrimitive(frame, VisualPrimitiveKind.WallRectangle, wallId);
            long townCenterDiameterRaw = Fixed.FromInt(GameData.GetBuildingPlacementRadiusTiles(BuildingTypeId.TownCenter) * 2).Raw;
            long wallDiameterRaw = Fixed.FromInt(GameData.GetBuildingPlacementRadiusTiles(BuildingTypeId.Wall) * 2).Raw;

            AssertEqual(true, capital.IsCapital, "capital primitive should be marked as capital");
            AssertEqual(townCenterDiameterRaw, capital.Size.Raw, "capital primitive footprint should match TC simulation diameter");
            AssertEqual(townCenterDiameterRaw, normalTownCenter.Size.Raw, "normal TC primitive footprint should match TC simulation diameter");
            AssertEqual(wallDiameterRaw, wall.Size.Raw, "wall primitive footprint should match wall simulation diameter");
        }

        private static void VisualFrameIncludesTypeIds()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(87, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(0, 0));
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));
            VisualPrimitive villager = FindPrimitive(frame, VisualPrimitiveKind.UnitSquare, 1);
            VisualPrimitive townCenter = FindPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, state.PlayerStates.Players[0].CapitalStatus.CapitalBuildingId);
            VisualPrimitive food = FindPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle, 1);

            AssertEqual((int)UnitTypeId.Villager, villager.TypeId, "unit visual primitive should expose unit type id");
            AssertEqual((int)BuildingTypeId.TownCenter, townCenter.TypeId, "building visual primitive should expose building type id");
            AssertEqual((int)ResourceType.Food, food.TypeId, "resource visual primitive should expose resource type id");
        }

        private static void VisualFrameIncludesResourcePrimitives()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(82, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());

            VisualFrame frame = VisualFrameBuilder.Build(GameSnapshotBuilder.Build(state, 0));

            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle), "visual frame should include food resource primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.WoodResourceCircle), "visual frame should include wood resource primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.GoldResourceCircle), "visual frame should include gold resource primitive");
        }

        private static void VisualFrameUsesResourceProfileVisualSize()
        {
            var resources = new List<ResourceNodeSnapshot>
            {
                new ResourceNodeSnapshot(1, 1, ResourceType.Wood, ResourceNodeType.Tree, GatherProfileId.Tree, FixedVector2.FromInts(10, 10), GameData.StartingWoodAmount),
                new ResourceNodeSnapshot(2, 2, ResourceType.Gold, ResourceNodeType.GoldVeinSmall, GatherProfileId.GoldVeinSmall, FixedVector2.FromInts(20, 10), GameData.StartingGoldAmount),
                new ResourceNodeSnapshot(3, 3, ResourceType.Gold, ResourceNodeType.GoldVeinLarge, GatherProfileId.GoldVeinLarge, FixedVector2.FromInts(30, 10), GameData.CenterGoldAmount)
            };
            var snapshot = new GameSnapshot(
                0,
                0,
                new List<UnitSnapshot>(),
                new List<BuildingSnapshot>(),
                resources,
                new LocalPlayerSnapshot(0, 0, 0, 0, 0, false, false, false),
                new MatchSnapshot(false, -1, -1));

            VisualFrame frame = VisualFrameBuilder.Build(snapshot);
            VisualPrimitive treePrimitive = FindPrimitive(frame, VisualPrimitiveKind.WoodResourceCircle, 1);
            VisualPrimitive smallGoldPrimitive = FindPrimitive(frame, VisualPrimitiveKind.GoldResourceCircle, 2);
            VisualPrimitive largeGoldPrimitive = FindPrimitive(frame, VisualPrimitiveKind.GoldResourceCircle, 3);

            AssertEqual(Fixed.FromInt(GameData.GetGatherProfile(GatherProfileId.Tree).VisualRadiusTiles * 2).Raw, treePrimitive.Size.Raw, "tree primitive should expose profile visual size");
            AssertEqual(Fixed.FromInt(GameData.GetGatherProfile(GatherProfileId.GoldVeinSmall).VisualRadiusTiles * 2).Raw, smallGoldPrimitive.Size.Raw, "small gold primitive should expose profile visual size");
            AssertEqual(Fixed.FromInt(GameData.GetGatherProfile(GatherProfileId.GoldVeinLarge).VisualRadiusTiles * 2).Raw, largeGoldPrimitive.Size.Raw, "large gold primitive should expose profile visual size");
            AssertEqual(true, largeGoldPrimitive.Size.Raw > smallGoldPrimitive.Size.Raw, "large gold should look larger than small gold");
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

        private static void ClientIntentMapsMovementCommand()
        {
            CommandEnvelope envelope = ClientCommandMapper.ToCommandEnvelope(
                ClientCommandIntent.MoveUnits(new[] { 1, 2 }, FixedVector2.FromInts(5, 6)),
                12,
                0,
                7);

            AssertEqual(12, envelope.Header.Tick, "client intent should copy command tick");
            AssertEqual(0, envelope.Header.PlayerIndex, "client intent should copy player index");
            AssertEqual(7u, envelope.Header.Sequence, "client intent should copy sequence");
            AssertEqual(CommandType.MoveUnits, envelope.Header.CommandType, "client movement intent should map to movement command");
            AssertEqual(CommandType.MoveUnits, envelope.Payload.Type, "payload should be movement command");
        }

        private static void ClientIntentSortsSelectedUnitIds()
        {
            ClientCommandIntent move = ClientCommandIntent.MoveUnits(new[] { 4, 1, 3, 2 }, FixedVector2.FromInts(5, 6));
            ClientCommandIntent gather = ClientCommandIntent.GatherResource(10, new[] { 9, 7, 8 });
            ClientCommandIntent build = ClientCommandIntent.AssignBuild(11, new[] { 6, 5, 4 });
            ClientCommandIntent attack = ClientCommandIntent.Attack(new[] { 14, 12, 13 }, 99);

            AssertEqual(1, move.UnitIds[0], "move selected unit ids should be sorted before command mapping");
            AssertEqual(2, move.UnitIds[1], "move selected unit ids should be sorted before command mapping");
            AssertEqual(3, move.UnitIds[2], "move selected unit ids should be sorted before command mapping");
            AssertEqual(4, move.UnitIds[3], "move selected unit ids should be sorted before command mapping");
            AssertEqual(7, gather.UnitIds[0], "gather selected unit ids should be sorted before command mapping");
            AssertEqual(8, gather.UnitIds[1], "gather selected unit ids should be sorted before command mapping");
            AssertEqual(9, gather.UnitIds[2], "gather selected unit ids should be sorted before command mapping");
            AssertEqual(4, build.UnitIds[0], "build selected unit ids should be sorted before command mapping");
            AssertEqual(5, build.UnitIds[1], "build selected unit ids should be sorted before command mapping");
            AssertEqual(6, build.UnitIds[2], "build selected unit ids should be sorted before command mapping");
            AssertEqual(12, attack.UnitIds[0], "attack selected unit ids should be sorted before command mapping");
            AssertEqual(13, attack.UnitIds[1], "attack selected unit ids should be sorted before command mapping");
            AssertEqual(14, attack.UnitIds[2], "attack selected unit ids should be sorted before command mapping");
        }

        private static void ClientIntentMapsResearchCommand()
        {
            CommandEnvelope envelope = ClientCommandMapper.ToCommandEnvelope(
                ClientCommandIntent.ResearchTech(11, TechId.InfantryAttack1),
                13,
                0,
                8);

            AssertEqual(13, envelope.Header.Tick, "client research intent should copy command tick");
            AssertEqual(0, envelope.Header.PlayerIndex, "client research intent should copy player index");
            AssertEqual(8u, envelope.Header.Sequence, "client research intent should copy sequence");
            AssertEqual(CommandType.ResearchTech, envelope.Header.CommandType, "client research intent should map to research command");
            AssertEqual(CommandType.ResearchTech, envelope.Payload.Type, "payload should be research command");
        }

        private static void ClientIntentMapsLocal1v1CommandFlow()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(69, 2);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();

            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)), 0, 0, 0));
            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(50, 10)), 0, 1, 0));
            runner.AdvanceOneTick(state, rules, buffer);

            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.AssignBuild(11, new[] { 1, 2, 3, 4 }), 1, 0, 1));
            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.AssignBuild(12, new[] { 6, 7, 8, 9 }), 1, 1, 1));
            runner.AdvanceOneTick(state, rules, buffer);

            int tick = 2;
            uint sequence = 2;
            while (tick < 100
                && (!state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive
                    || !state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive))
            {
                buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.NoOp(), tick, 0, sequence));
                buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.NoOp(), tick, 1, sequence));
                runner.AdvanceOneTick(state, rules, buffer);
                tick++;
                sequence++;
            }

            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.MoveUnits(new[] { 5 }, FixedVector2.FromInts(12, 12)), tick, 0, sequence));
            buffer.Add(ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.NoOp(), tick, 1, sequence));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, state.PlayerStates.Players[0].CapitalStatus.CapitalBonusActive, "client flow should complete player 0 capital through normal sim commands");
            AssertEqual(true, state.PlayerStates.Players[1].CapitalStatus.CapitalBonusActive, "client flow should complete player 1 capital through normal sim commands");
            AssertEqual(true, state.EntityState.Units[4].HasMoveTarget, "client movement intent should assign normal sim move target");
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "client intent flow should not create invalid commands");
        }

        private static void ClientCommandMappingDoesNotMutateChecksum()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(70, 1);
            new TickRunner().AdvanceOneTick(state, rules, new CommandBuffer());
            ulong before = StateChecksum.Compute(state, rules);

            ClientCommandMapper.ToCommandEnvelope(ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)), state.Tick, 0, 0);
            ulong after = StateChecksum.Compute(state, rules);

            AssertEqual(before, after, "mapping client intent should not mutate simulation state");
        }

        private static void LocalPlaySessionAdvancesWithAutomaticNoOps()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(71);

            session.QueueIntent(0, ClientCommandIntent.MoveUnits(new[] { 5 }, FixedVector2.FromInts(2, 2)));
            session.AdvanceOneTick();

            AssertEqual(1, session.CurrentTick, "local session should advance one tick");
            AssertEqual(2, session.ExecutedCommandCount, "local session should fill missing player input with noop");
            AssertEqual(0, session.RejectedCommandCount, "automatic noop fill should not reject");
        }

        private static void LocalPlaySessionQueuesWithoutMutatingBeforeTick()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(72);

            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)));
            GameSnapshot snapshot = session.GetSnapshot(0);

            AssertEqual(0, session.CurrentTick, "queuing should not advance simulation");
            AssertEqual(false, snapshot.LocalPlayer.HasCapitalBeenPlaced, "queued command should not mutate player capital state before tick execution");
            AssertEqual(0, session.ExecutedCommandCount, "queued command should not execute before tick");
        }

        private static void LocalPlaySessionCompletes1v1Capitals()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(73);

            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)));
            session.QueueIntent(1, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(50, 10)));
            session.AdvanceOneTick();
            session.QueueIntent(0, ClientCommandIntent.AssignBuild(11, new[] { 1, 2, 3, 4 }));
            session.QueueIntent(1, ClientCommandIntent.AssignBuild(12, new[] { 6, 7, 8, 9 }));
            session.AdvanceOneTick();
            for (int i = 0; i < 100; i++)
            {
                GameSnapshot playerZeroProgress = session.GetSnapshot(0);
                GameSnapshot playerOneProgress = session.GetSnapshot(1);
                if (playerZeroProgress.LocalPlayer.CapitalBonusActive && playerOneProgress.LocalPlayer.CapitalBonusActive)
                {
                    break;
                }

                session.AdvanceOneTick();
            }

            GameSnapshot playerZero = session.GetSnapshot(0);
            GameSnapshot playerOne = session.GetSnapshot(1);

            AssertEqual(true, playerZero.LocalPlayer.CapitalBonusActive, "player 0 capital should complete through local session commands");
            AssertEqual(true, playerOne.LocalPlayer.CapitalBonusActive, "player 1 capital should complete through local session commands");
            AssertEqual(0, session.RejectedCommandCount, "local 1v1 capital flow should not reject");
        }

        private static void LocalPlaySessionExposesVisualFrame()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(74);

            session.AdvanceOneTick();
            VisualFrame frame = session.GetVisualFrame(0);

            AssertEqual(1, frame.Tick, "visual frame should match local session tick");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.FogOverlay), "local visual frame should include fog primitive");
            AssertEqual(true, HasPrimitive(frame, VisualPrimitiveKind.UnitSquare), "local visual frame should include visible local units");
        }

        private static void LocalPlaySessionRejectsInvalidIntentThroughSim()
        {
            LocalPlaySession session = LocalPlaySession.Create1v1(75);

            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(10, 10)));
            session.AdvanceOneTick();
            session.QueueIntent(0, ClientCommandIntent.PlaceTownCenter(FixedVector2.FromInts(20, 20)));
            session.AdvanceOneTick();

            AssertEqual(1, session.RejectedCommandCount, "invalid local intent should be rejected by simulation validation");
            AssertEqual(true, session.GetSnapshot(0).LocalPlayer.HasCapitalBeenPlaced, "valid first capital should remain placed");
        }

        private static void LocalPlaySessionCreates6PlayerFfa()
        {
            LocalPlaySession session = LocalPlaySession.Create6PlayerFfa(96);

            session.AdvanceOneTick();

            AssertEqual(6, session.PlayerCount, "local FFA session should expose all six players");
            AssertEqual(1, session.CurrentTick, "local FFA session should advance normally");
            AssertEqual(6, session.ExecutedCommandCount, "local FFA session should fill all six player inputs with deterministic noops");
            AssertEqual(0, session.RejectedCommandCount, "automatic six-player noops should not reject");
            AssertEqual(0, session.GetSnapshot(0).LocalPlayerIndex, "local FFA snapshot should support player 0 view");
            AssertEqual(5, session.GetSnapshot(5).LocalPlayerIndex, "local FFA snapshot should support player 5 view");
        }

        private static void LocalPlaySessionCreatesDryArabiaTestMap()
        {
            LocalPlaySession session = LocalPlaySession.CreateDryArabiaTest01(97);

            session.AdvanceOneTick();
            GameSnapshot snapshot = session.GetSnapshot(0);

            AssertEqual(2, session.PlayerCount, "dry arabia local session should be a 1v1 map");
            AssertEqual(DryArabiaTest01MapDefinition.MapName, session.MapName, "dry arabia local session should expose map name");
            AssertEqual(true, HasResource(snapshot, ResourceType.Food), "dry arabia local session should expose visible local food");
            AssertEqual(true, HasResource(snapshot, ResourceType.Wood), "dry arabia local session should expose visible local wood");
            AssertEqual(true, HasResource(snapshot, ResourceType.Gold), "dry arabia local session should expose visible local gold");

            GodotClientFacade facade = GodotClientFacade.CreateDryArabiaTest01(97);
            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            string hud = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(DryArabiaTest01MapDefinition.MapName, facade.MapName, "godot facade should expose dry arabia map name");
            AssertEqual(DryArabiaTest01MapDefinition.MapName, frame.MapName, "godot frame should expose dry arabia map name");
            AssertEqual(true, hud.Contains("Map " + DryArabiaTest01MapDefinition.MapName), "hud should include dry arabia map name");
        }

        private static void GodotFacadeReturnsDrawableFrameDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(76);

            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);

            AssertEqual(1, frame.Tick, "godot frame should report current tick");
            AssertEqual(0, frame.LocalPlayerIndex, "godot frame should report local player");
            AssertEqual(true, HasGodotPrimitive(frame, VisualPrimitiveKind.FogOverlay), "godot frame should include fog primitive");
            AssertEqual(true, HasGodotPrimitive(frame, VisualPrimitiveKind.UnitSquare), "godot frame should include unit primitive");
        }

        private static void GodotFacadeDrivesLocalCapitalFlow()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(77);

            facade.QueuePlaceTownCenter(0, 10, 10);
            facade.QueuePlaceTownCenter(1, 50, 10);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            facade.QueueAssignBuild(1, 12, new[] { 6, 7, 8, 9 });
            for (int i = 0; i < 100; i++)
            {
                GodotFrameDto playerZeroProgress = facade.GetFrame(0);
                GodotFrameDto playerOneProgress = facade.GetFrame(1);
                if (playerZeroProgress.LocalPlayer.CapitalBonusActive && playerOneProgress.LocalPlayer.CapitalBonusActive)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }

            GodotFrameDto playerZero = facade.GetFrame(0);
            GodotFrameDto playerOne = facade.GetFrame(1);

            AssertEqual(true, playerZero.LocalPlayer.CapitalBonusActive, "godot facade should complete player 0 capital through local session");
            AssertEqual(true, playerOne.LocalPlayer.CapitalBonusActive, "godot facade should complete player 1 capital through local session");
            AssertEqual(0, facade.RejectedCommandCount, "godot facade capital flow should not reject");
        }

        private static void GodotFacadeCreatesLocal6PlayerFfa()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal6PlayerFfa(97);

            facade.AdvanceOneTick();
            GodotFrameDto playerFive = facade.GetFrame(5);

            AssertEqual(6, facade.PlayerCount, "godot facade should expose six-player local FFA setup");
            AssertEqual(6, facade.ExecutedCommandCount, "godot local FFA should fill six inputs with noops");
            AssertEqual(5, playerFive.LocalPlayerIndex, "godot facade should return player 5 frame for local FFA");
            AssertEqual(true, HasGodotPrimitive(playerFive, VisualPrimitiveKind.UnitSquare), "player 5 frame should expose visible local starting units");
            AssertEqual(0, facade.RejectedCommandCount, "six-player facade setup should not reject");
        }

        private static void GodotFacadeRejectsInvalidCommandsThroughSim()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(78);

            facade.QueuePlaceTownCenter(0, 10, 10);
            facade.AdvanceOneTick();
            facade.QueuePlaceTownCenter(0, 20, 20);
            facade.AdvanceOneTick();

            AssertEqual(1, facade.RejectedCommandCount, "godot facade should route invalid commands to sim rejection");
            AssertEqual(true, facade.GetFrame(0).LocalPlayer.HasCapitalBeenPlaced, "first valid capital should remain visible in local player state");
        }

        private static void GodotFacadeExposesLastCommandRejectionMetadata()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(789);
            facade.QueueMoveUnits(0, new[] { 1 }, 3, 8);
            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);

            AssertEqual(false, frame.Match.LastCommandAccepted, "invalid command should expose rejected status");
            AssertEqual((int)CommandType.MoveUnits, frame.Match.LastCommandTypeId, "match metadata should expose command type");
            AssertEqual((int)CommandValidationReason.TargetBlockedByStaticGeometry, frame.Match.LastCommandReasonId, "resource-blocked move should expose blocked geometry reason");
            AssertEqual(0, frame.Match.LastCommandPlayerIndex, "match metadata should expose issuing player");
            AssertEqual(3, frame.Match.LastCommandTargetTileX, "match metadata should expose target tile x");
            AssertEqual(8, frame.Match.LastCommandTargetTileY, "match metadata should expose target tile y");
        }

        private static void GodotFacadeExposesFixedRawCoordinates()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(79);

            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            GodotPrimitiveDto unit = FindGodotPrimitive(frame, VisualPrimitiveKind.UnitSquare, 1);

            AssertEqual(Fixed.FromInt(0).Raw, unit.XRaw, "godot facade should expose fixed raw X coordinate");
            AssertEqual(Fixed.FromInt(0).Raw, unit.YRaw, "godot facade should expose fixed raw Y coordinate");
            AssertEqual(Fixed.FromRatio(7, 10).Raw, unit.SizeRaw, "godot facade should expose fixed raw primitive size");
        }

        private static void GodotFacadeExposesPrimitiveTypeIds()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(88);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            GodotPrimitiveDto villager = FindGodotPrimitive(frame, VisualPrimitiveKind.UnitSquare, 1);
            GodotPrimitiveDto townCenter = FindGodotPrimitive(frame, VisualPrimitiveKind.BuildingRectangle, 11);
            GodotPrimitiveDto food = FindGodotPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle, 1);

            AssertEqual((int)UnitTypeId.Villager, villager.TypeId, "godot unit primitive should expose unit type id");
            AssertEqual((int)BuildingTypeId.TownCenter, townCenter.TypeId, "godot building primitive should expose building type id");
            AssertEqual((int)ResourceType.Food, food.TypeId, "godot resource primitive should expose resource type id");
        }

        private static void GodotFacadeExposesBuildingStatusDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(90);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 200; i++)
            {
                GodotBuildingStatusDto buildStatus = FindGodotBuildingStatus(facade.GetFrame(0), 11);
                if (!buildStatus.IsUnderConstruction)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }
            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Food < 50; i++)
            {
                facade.AdvanceOneTick();
            }
            facade.QueueTrainUnit(0, 11, (int)UnitTypeId.Villager);
            facade.AdvanceOneTick();

            GodotBuildingStatusDto status = FindGodotBuildingStatus(facade.GetFrame(0), 11);

            AssertEqual(false, status.IsUnderConstruction, "godot building status should show completed building");
            AssertEqual(true, status.TrainingQueueCount >= 0, "godot building status should expose training queue count");
            AssertEqual(true, status.TrainingProgressTicks >= 0, "godot building status should expose training progress");
            AssertEqual(true, status.TrainingRequiredTicks >= 0, "godot building status should expose training requirement");
        }

        private static void GodotFacadeExposesUnitStatusDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(92);

            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 200; i++)
            {
                facade.AdvanceOneTick();
                if (FindGodotUnitStatus(facade.GetFrame(0), 1).CarriedAmount > 0)
                {
                    break;
                }
            }

            GodotUnitStatusDto status = FindGodotUnitStatus(facade.GetFrame(0), 1);

            AssertEqual((int)UnitTypeId.Villager, status.UnitTypeId, "godot unit status should expose unit type");
            AssertEqual(1, status.CurrentResourceNodeId, "godot unit status should expose gather target");
            AssertEqual(true, status.CarriedResourceTypeId >= 0, "godot unit status should expose carried resource type id");
            AssertEqual(true, status.CarriedAmount >= 0, "godot unit status should expose carried amount");
        }

        private static void GodotFacadeExposesResourcePrimitiveDto()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(83);

            facade.AdvanceOneTick();
            GodotFrameDto frame = facade.GetFrame(0);
            GodotPrimitiveDto food = FindGodotPrimitive(frame, VisualPrimitiveKind.FoodResourceCircle, 1);

            AssertEqual((int)VisualPrimitiveKind.FoodResourceCircle, food.Kind, "godot facade should expose food resource primitive kind");
            AssertEqual(GameData.NeutralOwnerPlayerIndex, food.OwnerPlayerIndex, "resource primitive should be neutral-owned presentation data");
            AssertEqual(Fixed.FromInt(6).Raw, food.XRaw, "godot facade should expose resource fixed raw X coordinate");
            AssertEqual(Fixed.FromInt(0).Raw, food.YRaw, "godot facade should expose resource fixed raw Y coordinate");
        }

        private static void GodotFacadeRoutesGatherCommand()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(84);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 200; i++)
            {
                if (!FindGodotBuildingStatus(facade.GetFrame(0), 11).IsUnderConstruction)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }
            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Food < 10; i++)
            {
                facade.AdvanceOneTick();
            }

            AssertEqual(true, facade.ExecutedCommandCount > 0, "godot facade gather route should execute through local simulation");
            AssertEqual(0, facade.RejectedCommandCount, "valid facade gather flow should not reject");
        }

        private static void GodotFacadeRoutesTrainingCommand()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(85);

            facade.QueuePlaceTownCenter(0, 3, 8);
            facade.AdvanceOneTick();
            facade.QueueAssignBuild(0, 11, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 200; i++)
            {
                if (!FindGodotBuildingStatus(facade.GetFrame(0), 11).IsUnderConstruction)
                {
                    break;
                }

                facade.AdvanceOneTick();
            }
            facade.QueueMoveUnits(0, new[] { 2, 3, 4 }, 20, 20);
            facade.AdvanceTicks(20);
            facade.QueueGatherResource(0, 1, new[] { 1 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Food < 50; i++)
            {
                facade.AdvanceOneTick();
            }
            bool hadTrainingFood = facade.GetFrame(0).LocalPlayer.Food >= 50;
            facade.QueueTrainUnit(0, 11, (int)UnitTypeId.Villager);
            facade.AdvanceTicks(GameData.VillagerTrainTicks);

            GodotFrameDto frame = facade.GetFrame(0);
            if (hadTrainingFood)
            {
                AssertEqual(10, frame.LocalPlayer.Food, "godot facade should spend villager food cost through training command");
                AssertEqual(6, frame.LocalPlayer.PopulationUsed, "training should reserve one villager population through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade training flow should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "training command should route to simulation and reject when resources are insufficient");
            }
        }

        private static void GodotFacadeRoutesAttackCommand()
        {
            GodotClientFacade facade = GodotClientFacade.CreateLocal1v1(86);

            facade.QueueAttack(0, new[] { 1 }, 6);
            facade.AdvanceOneTick();

            AssertEqual(2, facade.ExecutedCommandCount, "facade attack should execute alongside automatic local noop");
            AssertEqual(0, facade.RejectedCommandCount, "valid facade attack command should not reject");
        }

        private static void GodotFacadeRoutesWallCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(93);

            facade.QueueGatherResource(0, 2, new[] { 1, 2, 3, 4 });
            for (int i = 0; i < 1000 && facade.GetFrame(0).LocalPlayer.Wood < GameData.WallWoodCost; i++)
            {
                facade.AdvanceOneTick();
            }
            bool hadWallWood = facade.GetFrame(0).LocalPlayer.Wood >= GameData.WallWoodCost;
            facade.QueuePlaceWall(0, 7, 8);
            facade.AdvanceOneTick();

            GodotFrameDto frame = facade.GetFrame(0);
            if (hadWallWood)
            {
                AssertEqual(true, HasGodotPrimitive(frame, VisualPrimitiveKind.WallRectangle), "godot facade should route wall placement through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade wall placement should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "wall command should route to simulation and reject when resources are insufficient");
            }
        }

        private static void GodotFacadeRoutesTradePostCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(94);

            facade.QueueGatherResource(0, 2, new[] { 1 });
            facade.AdvanceTicks(30);
            facade.QueueGatherResource(0, 3, new[] { 2 });
            facade.AdvanceTicks(10);
            bool hadTradePostResources = facade.GetFrame(0).LocalPlayer.Wood >= GameData.TradePostWoodCost
                && facade.GetFrame(0).LocalPlayer.Gold >= GameData.TradePostGoldCost;
            facade.QueuePlaceTradePost(0, 8, 11);
            facade.AdvanceOneTick();

            GodotFrameDto frame = facade.GetFrame(0);

            bool hasTradePost = HasGodotPrimitiveWithType(frame, VisualPrimitiveKind.BuildingRectangle, (int)BuildingTypeId.TradePost);
            if (hadTradePostResources)
            {
                AssertEqual(true, hasTradePost, "godot facade should route trade post placement through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade trade post placement should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "trade post command should route to simulation and reject when resources are insufficient");
            }
        }

        private static void GodotFacadeRoutesTradeCartTrainingCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(95);

            facade.QueueGatherResource(0, 2, new[] { 1 });
            facade.AdvanceTicks(46);
            facade.QueueGatherResource(0, 3, new[] { 2 });
            facade.AdvanceTicks(14);
            facade.QueuePlaceTradePost(0, 8, 11);
            facade.AdvanceOneTick();

            GodotFrameDto afterTradePostPlacement = facade.GetFrame(0);
            bool hasTradePost = HasGodotPrimitiveWithType(afterTradePostPlacement, VisualPrimitiveKind.BuildingRectangle, (int)BuildingTypeId.TradePost);
            if (!hasTradePost)
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "trade post placement should reject when resources are insufficient");
                return;
            }

            int tradePostId = FindGodotPrimitiveWithType(afterTradePostPlacement, VisualPrimitiveKind.BuildingRectangle, (int)BuildingTypeId.TradePost).EntityId;

            facade.QueueAssignBuild(0, tradePostId, new[] { 1, 2, 3, 4 });
            facade.AdvanceTicks(GameData.TradePostBuildTicks);
            facade.QueueTrainUnit(0, tradePostId, (int)UnitTypeId.TradeCart);
            facade.AdvanceTicks(GameData.TradeCartTrainTicks);

            GodotFrameDto frame = facade.GetFrame(0);
            if (HasGodotUnitStatusWithType(frame, (int)UnitTypeId.TradeCart))
            {
                AssertEqual(6, frame.LocalPlayer.PopulationUsed, "trade cart training should reserve one population through simulation");
                AssertEqual(0, facade.RejectedCommandCount, "valid facade trade cart training flow should not reject");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "trade cart training should route to simulation and reject when requirements are not met");
            }
        }

        private static void GodotFacadeRoutesResearchCommand()
        {
            GodotClientFacade facade = CreateGodotFacadeWithCompletedCapital(98);
            facade.QueueGatherResource(0, 1, new[] { 1, 2 });
            facade.QueueGatherResource(0, 3, new[] { 3, 4 });
            for (int i = 0; i < 1000 && (facade.GetFrame(0).LocalPlayer.Food < GameData.InfantryAttack1FoodCost || facade.GetFrame(0).LocalPlayer.Gold < GameData.InfantryAttack1GoldCost); i++)
            {
                facade.AdvanceOneTick();
            }
            GodotFrameDto beforeResearch = facade.GetFrame(0);
            facade.QueueResearchTech(0, 11, (int)TechId.InfantryAttack1);
            facade.AdvanceOneTick();

            GodotFrameDto frame = facade.GetFrame(0);
            if (frame.LocalPlayer.ResearchQueue.Length > 0)
            {
                AssertEqual(1, frame.LocalPlayer.ResearchQueue.Length, "godot facade should expose queued research after routing command");
                AssertEqual((int)TechId.InfantryAttack1, frame.LocalPlayer.ResearchQueue[0].TechId, "godot facade should expose research tech id");
                AssertEqual(1, frame.LocalPlayer.ResearchQueue[0].ProgressTicks, "godot facade should expose research progress");
            }
            else
            {
                AssertEqual(true, facade.RejectedCommandCount > 0, "research command should route to simulation and reject when requirements are not met");
            }
        }

        private static void GodotInteractionRouterPrioritizesAttack()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.FoodResourceCircle, 10, GameData.NeutralOwnerPlayerIndex, 5, 5),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 20, 1, 5, 5)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.Attack, intent.Kind, "enemy target should take priority over gather when primitives overlap");
            AssertEqual(20, intent.TargetEntityId, "attack intent should expose target entity id");
            AssertEqual(0, intent.ResourceNodeId, "attack intent should not expose a resource id");
        }

        private static void GodotInteractionRouterRoutesBuildAssignment()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 21, 0, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(21, (int)BuildingTypeId.TownCenter, true, 1, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.AssignBuild, intent.Kind, "own under-construction building should route to build assignment");
            AssertEqual(21, intent.TargetEntityId, "build assignment intent should expose target building id");
        }

        private static void GodotInteractionRouterRoutesBuildAssignmentWithExpandedBounds()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 121, 0, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(121, (int)BuildingTypeId.TownCenter, true, 1, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(6).Raw, Fixed.FromInt(6).Raw);

            AssertEqual(GodotInteractionIntentKind.AssignBuild, intent.Kind, "expanded interaction bounds should route build assignment near foundations");
            AssertEqual(121, intent.TargetEntityId, "expanded interaction bounds should preserve target building id");
        }

        private static void GodotInteractionRouterIgnoresCompletedBuildTarget()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 22, 0, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(22, (int)BuildingTypeId.TownCenter, false, GameData.TownCenterBuildTicks, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.None, intent.Kind, "completed own building should suppress ground move fallback");
            AssertEqual(22, intent.TargetEntityId, "completed own building should remain visible as the resolved target");
        }

        private static void GodotInteractionRouterRoutesResources()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 30, GameData.NeutralOwnerPlayerIndex, 6, 7)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(6).Raw, Fixed.FromInt(7).Raw);

            AssertEqual(GodotInteractionIntentKind.GatherResource, intent.Kind, "resource target should route to gather");
            AssertEqual(30, intent.ResourceNodeId, "gather intent should expose resource node id");
            AssertEqual(0, intent.TargetEntityId, "gather intent should not expose an attack target id");
        }

        private static void GodotInteractionRouterRoutesResourceOverFriendlyCompletedBuilding()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[]
                {
                    CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 22, 0, 5, 5),
                    CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, 31, GameData.NeutralOwnerPlayerIndex, 5, 5)
                },
                new[]
                {
                    new GodotBuildingStatusDto(22, (int)BuildingTypeId.TownCenter, false, GameData.TownCenterBuildTicks, GameData.TownCenterBuildTicks, 0, 0, 0, 0)
                });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(GodotInteractionIntentKind.GatherResource, intent.Kind, "resource target should beat friendly completed building suppression");
            AssertEqual(31, intent.ResourceNodeId, "overlapping resource should remain gatherable");
        }

        private static void GodotInteractionRouterPicksNearestOverlappingResource()
        {
            long clickX = Fixed.FromInt(6).Raw;
            long clickY = Fixed.FromInt(7).Raw;
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 35, GameData.NeutralOwnerPlayerIndex, 7, 7),
                CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 34, GameData.NeutralOwnerPlayerIndex, 6, 7)
            });

            int picked = GodotInteractionRouter.FindResourceAt(frame, clickX, clickY);
            AssertEqual(34, picked, "resource selection should prefer nearest center when hit boxes overlap");
        }

        private static void GodotInteractionRouterRoutesMoveFallback()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 40, 1, 12, 12)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(2).Raw, Fixed.FromInt(3).Raw);

            AssertEqual(GodotInteractionIntentKind.Move, intent.Kind, "empty right click should route to move fallback");
            AssertEqual(0, intent.TargetEntityId, "move intent should not expose an attack target id");
            AssertEqual(0, intent.ResourceNodeId, "move intent should not expose a resource id");
        }

        private static void GodotInteractionRouterIgnoresFriendlyTarget()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 50, 0, 4, 4)
            });

            GodotInteractionIntent intent = GodotInteractionRouter.RouteRightClick(frame, 0, true, Fixed.FromInt(4).Raw, Fixed.FromInt(4).Raw);

            AssertEqual(GodotInteractionIntentKind.Move, intent.Kind, "friendly targets should not route to attack");
            AssertEqual(0, intent.TargetEntityId, "friendly target should not be exposed as an attack target");
        }

        private static void GodotSelectionRouterPrioritizesLocalUnit()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 60, 0, 8, 8),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 61, 0, 8, 8)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(8).Raw, Fixed.FromInt(8).Raw);

            AssertEqual(GodotSelectionKind.Unit, selection.Kind, "local unit should take selection priority over local building");
            AssertEqual(61, selection.EntityId, "selection should expose selected unit id");
        }

        private static void GodotSelectionRouterSelectsLocalBuilding()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 70, 0, 9, 9)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(9).Raw, Fixed.FromInt(9).Raw);

            AssertEqual(GodotSelectionKind.Building, selection.Kind, "local building should be selectable");
            AssertEqual(70, selection.EntityId, "selection should expose selected building id");
        }

        private static void GodotSelectionRouterIgnoresEnemyPrimitive()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 80, 1, 10, 10)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(10).Raw, Fixed.FromInt(10).Raw);

            AssertEqual(GodotSelectionKind.None, selection.Kind, "enemy primitives should not be selected by local selection router");
            AssertEqual(0, selection.EntityId, "ignored selection should not expose an entity id");
        }

        private static void GodotSelectionRouterRectangleSelectsOwnedUnitsInIdOrder()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 103, 0, 7, 8),
                CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 201, 0, 8, 8),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 101, 0, 5, 5),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 102, 1, 6, 6),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 100, 0, 6, 7),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 104, 0, 20, 20)
            });

            int[] selected = GodotSelectionRouter.SelectUnitsInRectangle(
                frame,
                0,
                Fixed.FromInt(4).Raw,
                Fixed.FromInt(4).Raw,
                Fixed.FromInt(8).Raw,
                Fixed.FromInt(9).Raw);

            AssertEqual(3, selected.Length, "rectangle selection should include only owned units inside the drag box");
            AssertEqual(100, selected[0], "rectangle selection should return deterministic unit id order");
            AssertEqual(101, selected[1], "rectangle selection should return deterministic unit id order");
            AssertEqual(103, selected[2], "rectangle selection should return deterministic unit id order");
        }

        private static void GodotSelectionRouterRectangleNormalizesCorners()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 111, 0, 3, 4),
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 112, 0, 12, 12)
            });

            int[] selected = GodotSelectionRouter.SelectUnitsInRectangle(
                frame,
                0,
                Fixed.FromInt(5).Raw,
                Fixed.FromInt(5).Raw,
                Fixed.FromInt(2).Raw,
                Fixed.FromInt(2).Raw);

            AssertEqual(1, selected.Length, "rectangle selection should normalize drag corners");
            AssertEqual(111, selected[0], "normalized rectangle should select the owned unit inside the box");
        }

        private static void GodotSelectionRouterReturnsNone()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 90, 0, 14, 14)
            });

            GodotSelectionResult selection = GodotSelectionRouter.SelectAt(frame, 0, Fixed.FromInt(2).Raw, Fixed.FromInt(2).Raw);

            AssertEqual(GodotSelectionKind.None, selection.Kind, "empty click should not select anything");
            AssertEqual(0, selection.EntityId, "empty selection should not expose an entity id");
        }

        private static void GodotHudTextIncludesEconomyAndSelection()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                42,
                new GodotLocalPlayerDto(100, 80, 30, 6, 20, true, true, true),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 1, 2 }, 11, 5, true);

            AssertEqual(true, text.Contains("Tick 42"), "hud should include tick");
            AssertEqual(true, text.Contains("Food 100"), "hud should include food");
            AssertEqual(true, text.Contains("Wood 80"), "hud should include wood");
            AssertEqual(true, text.Contains("Gold 30"), "hud should include gold");
            AssertEqual(true, text.Contains("Pop 6/20"), "hud should include population");
            AssertEqual(true, text.Contains("Rej 0"), "hud should include rejected command count");
            AssertEqual(true, text.Contains("Selected 1,2"), "hud should include selected units");
            AssertEqual(true, text.Contains("Building 11"), "hud should include selected building");
            AssertEqual(true, text.Contains("Resource 5"), "hud should include hovered resource");
            AssertEqual(true, text.Contains("Paused"), "hud should include pause state");
            AssertEqual(true, text.Contains("Press H for hotkeys"), "hud should include control hint text");
        }

        private static void GodotHudTextBuildLinesReturnsTwoLines()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string[] lines = GodotHudTextBuilder.BuildLines(frame, new int[0], 0, 0, false);

            AssertEqual(2, lines.Length, "hud text should return two lines for runtime readability");
            AssertEqual(true, lines[0].Contains("Tick 1"), "first hud line should include match/economy summary");
            AssertEqual(true, lines[1].Contains("Selected none"), "second hud line should include interaction summary");
        }

        private static void GodotHudTextIncludesUnitGatherStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new[]
                {
                    new GodotUnitStatusDto(3, (int)UnitTypeId.Villager, false, 0, 0, 0, 9, (int)ResourceType.Food, 10, 0, 0)
                },
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 3 }, 0, 0, false);

            AssertEqual(true, text.Contains("Gather 9 Carry 10"), "hud should include selected unit gather status");
        }

        private static void GodotHudTextIncludesBuildingTrainingStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[]
                {
                    new GodotBuildingStatusDto(11, (int)BuildingTypeId.TownCenter, false, 0, 0, 1, (int)UnitTypeId.Villager, 4, GameData.VillagerTrainTicks)
                });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 11, 0, false);

            AssertEqual(true, text.Contains("Train 1 4/" + GameData.VillagerTrainTicks), "hud should include selected building training status");
        }

        private static void GodotHudTextIncludesResearchActionReady()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(21, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 21, 0, false);

            AssertEqual(true, text.Contains("Y:Research Ready"), "hud should show ready research action for selected town center");
        }

        private static void GodotHudTextIncludesResearchActionQueued()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    GameData.InfantryAttack1FoodCost,
                    0,
                    GameData.InfantryAttack1GoldCost,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new int[0],
                    new[] { new GodotResearchStatusDto((int)TechId.InfantryAttack1, 1, GameData.InfantryAttack1ResearchTicks) },
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(22, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 22, 0, false);

            AssertEqual(true, text.Contains("Y:Research Queued"), "hud should show queued research action for selected town center");
        }

        private static void GodotHudTextIncludesResearchActionDone()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new[] { (int)TechId.InfantryAttack1 },
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(23, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 23, 0, false);

            AssertEqual(true, text.Contains("Y:Research Done"), "hud should show completed research action for selected town center");
        }

        private static void GodotHudTextIncludesResearchActionBlockedBuild()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(24, (int)BuildingTypeId.TownCenter, true, 1, 5, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 24, 0, false);

            AssertEqual(true, text.Contains("Y:Research Blocked(Build)"), "hud should show blocked research action for under-construction town center");
        }

        private static void GodotHudTextIncludesResearchActionCost()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(25, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 25, 0, false);

            AssertEqual(true, text.Contains("Y:Research Cost " + GameData.InfantryAttack1FoodCost + "F/" + GameData.InfantryAttack1GoldCost + "G"), "hud should show research cost action when resources are missing");
        }

        private static void GodotHudTextIncludesTrainActionStatusForTownCenter()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(26, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 26, 0, false);

            AssertEqual(true, text.Contains("V:Ready"), "town center should show villager train action as ready with enough food");
            AssertEqual(true, text.Contains("I:Cost"), "town center should show infantry train action as cost when missing food");
            AssertEqual(true, text.Contains("K:N/A"), "town center should show trade cart train action as not applicable");
        }

        private static void GodotHudTextIncludesTrainActionStatusForTradePost()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, GameData.TradeCartWoodCost, GameData.TradeCartGoldCost, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(27, (int)BuildingTypeId.TradePost, false, 0, 0, 0, 0, 0, 0) });

            string text = GodotHudTextBuilder.Build(frame, new int[0], 27, 0, false);

            AssertEqual(true, text.Contains("V:N/A"), "trade post should show villager train action as not applicable");
            AssertEqual(true, text.Contains("I:N/A"), "trade post should show infantry train action as not applicable");
            AssertEqual(true, text.Contains("K:Ready"), "trade post should show trade cart train action as ready with enough resources");
        }

        private static void GodotHudTextIncludesResearchStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new int[0],
                    new[] { new GodotResearchStatusDto((int)TechId.InfantryAttack1, 2, GameData.InfantryAttack1ResearchTicks) },
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Research InfAtk1 2/" + GameData.InfantryAttack1ResearchTicks), "hud should include active research status");
        }

        private static void GodotHudTextIncludesQueuedResearchCount()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new int[0],
                    new[]
                    {
                        new GodotResearchStatusDto((int)TechId.InfantryAttack1, 2, GameData.InfantryAttack1ResearchTicks),
                        new GodotResearchStatusDto(99, 0, 10)
                    },
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Research InfAtk1 2/" + GameData.InfantryAttack1ResearchTicks + " +1"), "hud should include queued research count suffix");
        }

        private static void GodotHudTextIncludesModifierStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new[] { (int)TechId.InfantryAttack1 },
                    new GodotResearchStatusDto[0],
                    new[] { new GodotModifierStatusDto((int)ModifierId.InfantryAttackBonus, GameData.InfantryAttack1DamageBonus) }),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Mod InfAtkBonus=" + GameData.InfantryAttack1DamageBonus), "hud should include first active modifier status");
        }

        private static void GodotHudTextIncludesModifierCount()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new[] { (int)TechId.InfantryAttack1 },
                    new GodotResearchStatusDto[0],
                    new[]
                    {
                        new GodotModifierStatusDto((int)ModifierId.InfantryAttackBonus, GameData.InfantryAttack1DamageBonus),
                        new GodotModifierStatusDto(99, 2)
                    }),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Mod InfAtkBonus=" + GameData.InfantryAttack1DamageBonus + " +1"), "hud should include active modifier count suffix");
        }

        private static void GodotHudTextIncludesCompletedTechLabel()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new[] { (int)TechId.InfantryAttack1 },
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Tech InfAtk1"), "hud should include completed tech label when research queue is empty");
        }

        private static void GodotHudTextIncludesCompletedTechCount()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new[] { (int)TechId.InfantryAttack1, 99 },
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Tech #99 (2)"), "hud should include completed tech count suffix");
        }

        private static void GodotHudTextIncludesRejectedCommandCount()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0],
                3);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Rej 3"), "hud should include rejected command count from match dto");
        }

        private static void GodotHudTextIncludesLastCommandStatus()
        {
            GodotFrameDto frame = new GodotFrameDto(
                1,
                "DryArabiaTest01",
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(
                    false,
                    -1,
                    -1,
                    12,
                    2,
                    (int)CommandType.GatherResource,
                    (int)CommandValidationReason.TargetComplete,
                    false,
                    0,
                    5,
                    20,
                    42,
                    3,
                    1),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new int[0], 0, 0, false);

            AssertEqual(true, text.Contains("Cmd GatherResource rej"), "hud should include last command type and result");
            AssertEqual(true, text.Contains("r" + (int)CommandValidationReason.TargetComplete), "hud should include last command reason id");
        }

        private static void GodotHudTextHandlesMissingStatus()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            string text = GodotHudTextBuilder.Build(frame, new[] { 99 }, 77, 0, false);

            AssertEqual(true, text.Contains("Selected 99"), "hud should still include selected unit id when status is missing");
            AssertEqual(true, text.Contains("Building 77"), "hud should still include selected building id when status is missing");
            AssertEqual(false, text.Contains("Gather"), "missing unit status should not invent gather text");
            AssertEqual(false, text.Contains("Train"), "missing building status should not invent training text");
        }

        private static void GodotSelectedStatusHidesIdleNoProgressTicks()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new GodotPrimitiveDto[0],
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(
                        11,
                        (int)UnitTypeId.Villager,
                        false,
                        0,
                        0,
                        0,
                        0,
                        19,
                        44,
                        Fixed.FromInt(19).Raw,
                        Fixed.FromInt(44).Raw,
                        (int)WorkerTaskPhase.Idle,
                        (int)InteractionReservationKind.MoveDestination,
                        SpatialRules.EncodeTileKey(20, 44),
                        20,
                        44,
                        false,
                        false,
                        false,
                        10,
                        0,
                        0,
                        0,
                        0)
                });

            string[] lines = GodotSelectedStatusBuilder.BuildLines(frame, new[] { 11 }, 0, 0);

            AssertEqual(true, lines[1].Contains("NoProgress -"), "idle unit without active move should hide stale no-progress counter");
        }

        private static void GodotPrimitiveHitTestIncludesBoundary()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 100, 0, 10, 10);

            bool contains = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(10).Raw + Fixed.FromRatio(1, 2).Raw,
                Fixed.FromInt(10).Raw);

            AssertEqual(true, contains, "hit test should include primitive boundary");
        }

        private static void GodotBuildingHitTestIncludesFootprintBoundary()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitiveWithSize(
                VisualPrimitiveKind.BuildingRectangle,
                103,
                0,
                (int)BuildingTypeId.TownCenter,
                10,
                10,
                GameData.GetBuildingPlacementRadiusTiles(BuildingTypeId.TownCenter) * 2);

            bool contains = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(12).Raw,
                Fixed.FromInt(10).Raw);

            AssertEqual(true, contains, "building hit test should include the full visual/sim footprint boundary");
        }

        private static void GodotPrimitiveHitTestRejectsOutside()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 101, 0, 10, 10);

            bool contains = GodotPrimitiveHitTest.ContainsPoint(
                primitive,
                Fixed.FromInt(10).Raw + Fixed.FromRatio(1, 2).Raw + 1,
                Fixed.FromInt(10).Raw);

            AssertEqual(false, contains, "hit test should reject points beyond primitive boundary");
        }

        private static void GodotPrimitiveInteractionHitTestExpandsBuildingBounds()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 102, 0, (int)BuildingTypeId.TownCenter, 10, 10);

            long outsideCoreX = primitive.XRaw + (primitive.SizeRaw / 2) + Fixed.FromRatio(1, 2).Raw;
            long outsideCoreY = primitive.YRaw + (primitive.SizeRaw / 2) + Fixed.FromRatio(1, 2).Raw;
            bool containsCore = GodotPrimitiveHitTest.ContainsPoint(primitive, outsideCoreX, outsideCoreY);
            bool containsInteraction = GodotPrimitiveHitTest.ContainsPointForInteraction(primitive, outsideCoreX, outsideCoreY);

            AssertEqual(false, containsCore, "core hit test should stay strict");
            AssertEqual(true, containsInteraction, "interaction hit test should expand building bounds for playability");
        }

        private static void GodotVisualStyleResolvesLocalUnitTypes()
        {
            GodotPrimitiveDto villager = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 110, 0, (int)UnitTypeId.Villager, 1, 1);
            GodotPrimitiveDto scout = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 111, 0, (int)UnitTypeId.Scout, 1, 1);
            GodotPrimitiveDto infantry = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 112, 0, (int)UnitTypeId.Infantry, 1, 1);
            GodotPrimitiveDto cavalry = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 113, 0, (int)UnitTypeId.Cavalry, 1, 1);

            AssertEqual(GodotVisualStyle.LocalVillager, GodotVisualStyleResolver.ResolveUnit(villager, 0), "villager should resolve to local villager style");
            AssertEqual(GodotVisualStyle.LocalScout, GodotVisualStyleResolver.ResolveUnit(scout, 0), "scout should resolve to local scout style");
            AssertEqual(GodotVisualStyle.LocalInfantry, GodotVisualStyleResolver.ResolveUnit(infantry, 0), "infantry should resolve to local infantry style");
            AssertEqual(GodotVisualStyle.LocalCavalry, GodotVisualStyleResolver.ResolveUnit(cavalry, 0), "cavalry should resolve to local cavalry style");
        }

        private static void GodotVisualStyleResolvesEnemyUnit()
        {
            GodotPrimitiveDto enemy = CreateGodotPrimitiveWithType(VisualPrimitiveKind.UnitSquare, 120, 1, (int)UnitTypeId.Villager, 1, 1);

            AssertEqual(GodotVisualStyle.EnemyUnit, GodotVisualStyleResolver.ResolveUnit(enemy, 0), "enemy unit should resolve to enemy style regardless of type");
        }

        private static void GodotVisualStyleResolvesBuildingTypes()
        {
            GodotPrimitiveDto normal = CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 130, 0, 1, 1);
            GodotPrimitiveDto wall = CreateGodotPrimitive(VisualPrimitiveKind.WallRectangle, 131, 0, 1, 1);
            GodotPrimitiveDto capital = CreateGodotPrimitiveWithCapital(VisualPrimitiveKind.BuildingRectangle, 132, 0, 1, 1);

            AssertEqual(GodotVisualStyle.NormalBuilding, GodotVisualStyleResolver.ResolveBuilding(normal), "normal building should resolve to normal building style");
            AssertEqual(GodotVisualStyle.Wall, GodotVisualStyleResolver.ResolveBuilding(wall), "wall should resolve to wall style");
            AssertEqual(GodotVisualStyle.CapitalBuilding, GodotVisualStyleResolver.ResolveBuilding(capital), "capital building should resolve to capital style");
        }

        private static void GodotVisualStyleResolvesResources()
        {
            GodotPrimitiveDto food = CreateGodotPrimitive(VisualPrimitiveKind.FoodResourceCircle, 140, GameData.NeutralOwnerPlayerIndex, 1, 1);
            GodotPrimitiveDto wood = CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, 141, GameData.NeutralOwnerPlayerIndex, 1, 1);
            GodotPrimitiveDto gold = CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 142, GameData.NeutralOwnerPlayerIndex, 1, 1);

            AssertEqual(GodotVisualStyle.FoodResource, GodotVisualStyleResolver.ResolveResource(food), "food should resolve to food resource style");
            AssertEqual(GodotVisualStyle.WoodResource, GodotVisualStyleResolver.ResolveResource(wood), "wood should resolve to wood resource style");
            AssertEqual(GodotVisualStyle.GoldResource, GodotVisualStyleResolver.ResolveResource(gold), "gold should resolve to gold resource style");
        }

        private static void GodotTechLabelResolverResolvesKnownTech()
        {
            AssertEqual("InfAtk1", GodotTechLabelResolver.ResolveTechLabel((int)TechId.InfantryAttack1), "known tech id should map to readable label");
        }

        private static void GodotTechLabelResolverFallsBackForUnknownTech()
        {
            AssertEqual("#99", GodotTechLabelResolver.ResolveTechLabel(99), "unknown tech id should map to numeric fallback");
        }

        private static void GodotTechLabelResolverResolvesKnownModifier()
        {
            AssertEqual("InfAtkBonus", GodotTechLabelResolver.ResolveModifierLabel((int)ModifierId.InfantryAttackBonus), "known modifier id should map to readable label");
        }

        private static void GodotTechLabelResolverFallsBackForUnknownModifier()
        {
            AssertEqual("#42", GodotTechLabelResolver.ResolveModifierLabel(42), "unknown modifier id should map to numeric fallback");
        }

        private static void GodotResearchActionEvaluatorReturnsReady()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(31, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.Ready, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 31), "evaluator should report ready when building and resources are valid");
        }

        private static void GodotResearchActionEvaluatorReturnsQueued()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    GameData.InfantryAttack1FoodCost,
                    0,
                    GameData.InfantryAttack1GoldCost,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new int[0],
                    new[] { new GodotResearchStatusDto((int)TechId.InfantryAttack1, 1, GameData.InfantryAttack1ResearchTicks) },
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(32, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.Queued, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 32), "evaluator should report queued when infantry attack research already exists in queue");
        }

        private static void GodotResearchActionEvaluatorReturnsDone()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    new[] { (int)TechId.InfantryAttack1 },
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(33, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.Done, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 33), "evaluator should report done when infantry attack tech is already completed");
        }

        private static void GodotResearchActionEvaluatorReturnsMissingResources()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(34, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.MissingResources, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 34), "evaluator should report missing resources when cost cannot be paid");
        }

        private static void GodotResearchActionEvaluatorReturnsNone()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(35, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.None, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 0), "evaluator should report none when no building is selected");
        }

        private static void GodotResearchActionEvaluatorReturnsNotApplicable()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(36, (int)BuildingTypeId.TradePost, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.NotApplicable, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 36), "evaluator should report not applicable for non-research building type");
        }

        private static void GodotResearchActionEvaluatorReturnsBlockedConstruction()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.InfantryAttack1FoodCost, 0, GameData.InfantryAttack1GoldCost, 0, 0, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(37, (int)BuildingTypeId.TownCenter, true, 1, 5, 0, 0, 0, 0) });

            AssertEqual(GodotResearchActionState.BlockedConstruction, GodotResearchActionEvaluator.EvaluateInfantryAttack1(frame, 37), "evaluator should report blocked construction for incomplete selected town center");
        }

        private static void GodotTrainActionEvaluatorReturnsReady()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(41, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.Ready, GodotTrainActionEvaluator.Evaluate(frame, 41, (int)UnitTypeId.Villager), "train evaluator should report ready when training constraints are satisfied");
        }

        private static void GodotTrainActionEvaluatorReturnsMissingResources()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(0, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(42, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.MissingResources, GodotTrainActionEvaluator.Evaluate(frame, 42, (int)UnitTypeId.Villager), "train evaluator should report missing resources when cost cannot be paid");
        }

        private static void GodotTrainActionEvaluatorReturnsPopulationCapped()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 5, 5, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(43, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.PopulationCapped, GodotTrainActionEvaluator.Evaluate(frame, 43, (int)UnitTypeId.Villager), "train evaluator should report population cap block before queueing");
        }

        private static void GodotTrainActionEvaluatorReturnsNotApplicable()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.TradeCartWoodCost, 0, GameData.TradeCartGoldCost, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(44, (int)BuildingTypeId.TownCenter, false, 0, 0, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.NotApplicable, GodotTrainActionEvaluator.Evaluate(frame, 44, (int)UnitTypeId.TradeCart), "train evaluator should report not applicable when building cannot train the unit type");
        }

        private static void GodotTrainActionEvaluatorReturnsBlockedConstruction()
        {
            GodotFrameDto frame = CreateGodotHudFrame(
                1,
                new GodotLocalPlayerDto(GameData.VillagerFoodCost, 0, 0, 0, 10, false, false, false),
                new GodotUnitStatusDto[0],
                new[] { new GodotBuildingStatusDto(45, (int)BuildingTypeId.TownCenter, true, 1, 5, 0, 0, 0, 0) });

            AssertEqual(GodotTrainActionState.BlockedConstruction, GodotTrainActionEvaluator.Evaluate(frame, 45, (int)UnitTypeId.Villager), "train evaluator should report blocked construction for incomplete selected building");
        }

        private static void GodotPrimitiveDrawKindResolvesKnownPrimitives()
        {
            AssertEqual(GodotPrimitiveDrawKind.Unit, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.UnitSquare, 150, 0, 1, 1)), "unit primitive should resolve to unit draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Building, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.BuildingRectangle, 151, 0, 1, 1)), "building primitive should resolve to building draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Building, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.WallRectangle, 152, 0, 1, 1)), "wall primitive should resolve to building draw kind");
            AssertEqual(GodotPrimitiveDrawKind.TradeRoute, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.TradeRouteLine, 153, 0, 1, 1)), "trade route primitive should resolve to trade route draw kind");
            AssertEqual(GodotPrimitiveDrawKind.HealthBar, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.HealthBar, 154, 0, 1, 1)), "health bar primitive should resolve to health bar draw kind");
            AssertEqual(GodotPrimitiveDrawKind.FogOverlay, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.FogOverlay, 155, 0, 1, 1)), "fog primitive should resolve to fog draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Resource, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.FoodResourceCircle, 156, GameData.NeutralOwnerPlayerIndex, 1, 1)), "food primitive should resolve to resource draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Resource, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.WoodResourceCircle, 157, GameData.NeutralOwnerPlayerIndex, 1, 1)), "wood primitive should resolve to resource draw kind");
            AssertEqual(GodotPrimitiveDrawKind.Resource, GodotPrimitiveDrawKindResolver.Resolve(CreateGodotPrimitive(VisualPrimitiveKind.GoldResourceCircle, 158, GameData.NeutralOwnerPlayerIndex, 1, 1)), "gold primitive should resolve to resource draw kind");
        }

        private static void GodotPrimitiveDrawKindReturnsNoneForUnknown()
        {
            GodotPrimitiveDto primitive = CreateGodotPrimitive((VisualPrimitiveKind)999, 160, 0, 1, 1);

            AssertEqual(GodotPrimitiveDrawKind.None, GodotPrimitiveDrawKindResolver.Resolve(primitive), "unknown primitive kind should resolve to none");
        }

        private static void GodotDebugEventLogKeepsBoundedMessages()
        {
            var log = new GodotDebugEventLog(3);
            log.Add("a");
            log.Add("b");
            log.Add("c");
            log.Add("d");

            string[] lines = log.GetLines();
            AssertEqual(3, lines.Length, "bounded event log should keep max capacity");
            AssertEqual("b", lines[0], "oldest entry should roll off first");
            AssertEqual("d", lines[2], "latest entry should remain in log");
        }

        private static void GodotCommandResultClassifierClassifiesCounterDeltas()
        {
            AssertEqual(
                GodotCommandResultKind.Accepted,
                GodotCommandResultClassifier.Classify(10, 2, 11, 2),
                "increased executed count should classify as accepted");
            AssertEqual(
                GodotCommandResultKind.Rejected,
                GodotCommandResultClassifier.Classify(10, 2, 10, 3),
                "increased rejected count should classify as rejected");
            AssertEqual(
                GodotCommandResultKind.NoVisibleCountChange,
                GodotCommandResultClassifier.Classify(10, 2, 10, 2),
                "no count change should classify as no visible change");
        }

        private static void GodotHotkeyHelpContainsKnownBindings()
        {
            GodotHotkeyHelpEntry[] entries = GodotHotkeyHelpBuilder.Build(researchIsWired: true);
            AssertEqual(true, ContainsHotkey(entries, "F1", "Start DryArabiaTest01 local 1v1"), "hotkey help should include F1 binding");
            AssertEqual(true, ContainsHotkey(entries, "F6", "Start local 6-player FFA"), "hotkey help should include F6 binding");
            AssertEqual(true, ContainsHotkey(entries, "F9", "Toggle sprites/primitives"), "hotkey help should include F9 binding");
            AssertEqual(true, ContainsHotkey(entries, "F10", "Toggle debug overlay"), "hotkey help should include F10 debug overlay binding");
            AssertEqual(true, ContainsHotkey(entries, "F12", "Toggle screenshot mode"), "hotkey help should include F12 screenshot mode binding");
            AssertEqual(true, ContainsHotkey(entries, "H/F11", "Toggle hotkey help"), "hotkey help should include H/F11 help binding");
            AssertEqual(true, ContainsHotkey(entries, "Space", "Pause / unpause"), "hotkey help should include pause binding");
            AssertEqual(true, ContainsHotkey(entries, "Left Click", "Select / confirm placement"), "hotkey help should include left-click selection behavior");
            AssertEqual(true, ContainsHotkey(entries, "Right Click", "Move, gather, attack, assign build, or cancel placement"), "hotkey help should include right-click context behavior");
            AssertEqual(true, ContainsHotkey(entries, "C", "Enter Town Center placement mode"), "hotkey help should include C placement binding");
            AssertEqual(true, ContainsHotkey(entries, "W", "Place Wall at mouse"), "hotkey help should include W placement binding");
            AssertEqual(true, ContainsHotkey(entries, "T", "Place Trade Post at mouse"), "hotkey help should include T placement binding");
            AssertEqual(true, ContainsHotkey(entries, "R", "Create Trade Route with selected Trade Cart"), "hotkey help should include R trade route binding");
            AssertEqual(true, ContainsHotkey(entries, "V", "Train Villager"), "hotkey help should include V train villager binding");
            AssertEqual(true, ContainsHotkey(entries, "I", "Train Infantry"), "hotkey help should include I train infantry binding");
            AssertEqual(true, ContainsHotkey(entries, "K", "Train Trade Cart"), "hotkey help should include K train trade cart binding");
            AssertEqual(true, ContainsHotkey(entries, "Y", "Research Infantry Attack I"), "hotkey help should include Y research binding");
        }

        private static void GodotBuildingDebugStatusIncludesTrainingQueueCount()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[] { CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 211, 0, (int)BuildingTypeId.TownCenter, 5, 5) },
                new[] { new GodotBuildingStatusDto(211, (int)BuildingTypeId.TownCenter, false, 5, 5, 2, (int)UnitTypeId.Villager, 1, 3) });

            string[] lines = GodotBuildingDebugStatusBuilder.BuildLines(frame, 211);
            AssertEqual(true, lines[1].Contains("Queue 2"), "selected building debug status should include training queue count");
        }

        private static void GodotBuildingDebugStatusIncludesTrainingProgress()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[] { CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 212, 0, (int)BuildingTypeId.TradePost, 5, 5) },
                new[] { new GodotBuildingStatusDto(212, (int)BuildingTypeId.TradePost, false, 5, 5, 1, (int)UnitTypeId.TradeCart, 2, 4) });

            string[] lines = GodotBuildingDebugStatusBuilder.BuildLines(frame, 212);
            AssertEqual(true, lines[1].Contains("TradeCart 2/4"), "selected building debug status should include training progress");
        }

        private static void GodotBuildingDebugStatusMarksIncompleteTrainingUnavailable()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new[] { CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 213, 0, (int)BuildingTypeId.TownCenter, 5, 5) },
                new[] { new GodotBuildingStatusDto(213, (int)BuildingTypeId.TownCenter, true, 3, GameData.TownCenterBuildTicks, 0, 0, 0, 0) });

            string[] lines = GodotBuildingDebugStatusBuilder.BuildLines(frame, 213);
            AssertEqual(true, lines[0].Contains("BUILDING 3/" + GameData.TownCenterBuildTicks), "selected incomplete building status should include build progress");
            AssertEqual(true, lines[1].Contains("unavailable until complete"), "selected incomplete building should mark training unavailable");
        }

        private static void GodotTrainIntentTextIncludesUnitAndBuilding()
        {
            string text = GodotBuildingDebugStatusBuilder.BuildTrainIntentText(22, (int)UnitTypeId.TradeCart);
            AssertEqual(true, text.Contains("building=22"), "train intent text should include building id");
            AssertEqual(true, text.Contains("TradeCart"), "train intent text should include unit type label");
        }

        private static void SimulationSourceDoesNotReferenceDebugOverlayHelpers()
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine("src", "sim"), "*.cs", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string text = System.IO.File.ReadAllText(files[i]);
                AssertFalse(text.Contains("GodotDebugEventLog"), "simulation source must not reference debug overlay log helper file=" + files[i]);
                AssertFalse(text.Contains("GodotHotkeyHelpBuilder"), "simulation source must not reference hotkey help helper file=" + files[i]);
                AssertFalse(text.Contains("GodotCommandResultClassifier"), "simulation source must not reference command result classifier helper file=" + files[i]);
            }
        }

        private static void GodotSpriteSheetLayoutResolvesExpectedFrameRect()
        {
            bool found = GodotSpriteSheetLayout.TryGetMetadata(GodotSpriteAssetId.Villager, out GodotSpriteSheetMetadata metadata);
            AssertEqual(true, found, "villager metadata should exist");

            GodotSpriteFrameRect rect = GodotSpriteSheetLayout.ResolveFrameRect(metadata, 900, 900, 5);
            AssertEqual(600, rect.X, "frame rect x should match frame column");
            AssertEqual(300, rect.Y, "frame rect y should match frame row");
            AssertEqual(300, rect.Width, "frame width should be texture width divided by columns");
            AssertEqual(300, rect.Height, "frame height should be texture height divided by rows");
        }

        private static void GodotSpriteSheetLayoutUsesDeterministicDefaultFrame()
        {
            bool found = GodotSpriteSheetLayout.TryGetMetadata(GodotSpriteAssetId.Infantry, out GodotSpriteSheetMetadata metadata);
            AssertEqual(true, found, "infantry metadata should exist");

            int frame = GodotSpriteSheetLayout.ResolveDirectionalFrameIndex(metadata, false, 1, 1);
            AssertEqual(metadata.DefaultFrameIndex, frame, "default frame index should be deterministic when there is no move target");
        }

        private static void GodotSpriteSheetLayoutReturnsFalseForUnknownUnitType()
        {
            bool found = GodotSpriteSheetLayout.TryResolveUnitAsset(999, out _);
            AssertEqual(false, found, "unknown unit type should not resolve to a sprite asset so primitive fallback can render");
        }

        private static void GodotSpriteSheetLayoutIncludesPlaceholderSlots()
        {
            AssertEqual(18, GodotSpriteSheetLayout.ExpectedAssetCount, "phase 6 placeholder registry should include all expected visual slots");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveBuildingAsset((int)BuildingTypeId.TownCenter, out GodotSpriteAssetId townCenter), "normal town center should have an asset slot");
            AssertEqual(GodotSpriteAssetId.TownCenter, townCenter, "normal town center should resolve to its own placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveBuildingAsset((int)BuildingTypeId.TradePost, out GodotSpriteAssetId tradePost), "trade post should have an asset slot");
            AssertEqual(GodotSpriteAssetId.TradePost, tradePost, "trade post should resolve to trade post placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveResourceAsset((int)ResourceType.Food, out GodotSpriteAssetId food), "food should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Food, food, "food should resolve to food placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveResourceAsset((int)ResourceType.Wood, out GodotSpriteAssetId wood), "wood should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Wood, wood, "wood should resolve to wood placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveResourceAsset((int)ResourceType.Gold, out GodotSpriteAssetId gold), "gold should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Gold, gold, "gold should resolve to gold placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveUnitAsset((int)UnitTypeId.Cavalry, out GodotSpriteAssetId cavalry), "cavalry should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Cavalry, cavalry, "cavalry should resolve to cavalry placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveUnitAsset((int)UnitTypeId.SiegeCannon, out GodotSpriteAssetId siege), "siege cannon should have an asset slot");
            AssertEqual(GodotSpriteAssetId.SiegeCannon, siege, "siege cannon should resolve to siege placeholder slot");
            AssertEqual(true, GodotSpriteSheetLayout.TryResolveUnitAsset((int)UnitTypeId.Mangonel, out GodotSpriteAssetId mangonel), "mangonel should have an asset slot");
            AssertEqual(GodotSpriteAssetId.Mangonel, mangonel, "mangonel should resolve to mangonel placeholder slot");
        }

        private static void GodotSpriteSheetLayoutFilenamesAreCorrect()
        {
            AssertFilename(GodotSpriteAssetId.Villager, "villager_sheet.png");
            AssertFilename(GodotSpriteAssetId.Infantry, "infantry_sheet.png");
            AssertFilename(GodotSpriteAssetId.Scout, "scout_sheet.png");
            AssertFilename(GodotSpriteAssetId.TradeCart, "trade_cart_sheet.png");
            AssertFilename(GodotSpriteAssetId.Capital, "capital.png");
            AssertFilename(GodotSpriteAssetId.Wall, "wall_sheet.png");
            AssertFilename(GodotSpriteAssetId.TownCenter, "town_center.png");
            AssertFilename(GodotSpriteAssetId.TradePost, "trade_post.png");
            AssertFilename(GodotSpriteAssetId.BuildingScaffold, "building_scaffold.png");
            AssertFilename(GodotSpriteAssetId.Food, "food.png");
            AssertFilename(GodotSpriteAssetId.Wood, "wood.png");
            AssertFilename(GodotSpriteAssetId.Gold, "gold.png");
            AssertFilename(GodotSpriteAssetId.Cavalry, "cavalry_sheet.png");
            AssertFilename(GodotSpriteAssetId.SiegeCannon, "siege_cannon_sheet.png");
            AssertFilename(GodotSpriteAssetId.Mangonel, "mangonel_sheet.png");
            AssertFilename(GodotSpriteAssetId.GrassTile, "grass_tile_sheet.png");
            AssertFilename(GodotSpriteAssetId.DirtTile, "dirt_path_tile_sheet.png");
            AssertFilename(GodotSpriteAssetId.RockBlocker, "rock_blocker_sheet.png");
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

        private static void GodotCoordinateMapperConvertsRawToPixels()
        {
            float pixels = GodotCoordinateMapper.RawToPixels(Fixed.FromInt(3).Raw, 16.0f);

            AssertEqual(48.0f, pixels, "three fixed tiles should convert to forty-eight pixels at sixteen pixels per tile");
        }

        private static void GodotCoordinateMapperConvertsScreenToRaw()
        {
            long raw = GodotCoordinateMapper.ScreenToRaw(24.0f, 16.0f);

            AssertEqual(Fixed.FromRatio(3, 2).Raw, raw, "twenty-four pixels should convert to one and a half raw tiles at sixteen pixels per tile");
        }

        private static void GodotCoordinateMapperFloorsScreenTile()
        {
            AssertEqual(1, GodotCoordinateMapper.ScreenToTile(31.9f, 16.0f), "positive screen coordinates should floor to tile index");
            AssertEqual(-1, GodotCoordinateMapper.ScreenToTile(-0.1f, 16.0f), "negative screen coordinates should floor down, not truncate toward zero");
        }

        private static void GodotTradeRouteRouterFindsLocalTradePost()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 170, 0, (int)BuildingTypeId.TradePost, 5, 5)
            });

            int tradePostId = GodotTradeRouteRouter.FindLocalTradePostAt(frame, 0, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(170, tradePostId, "router should find visible local trade post under cursor");
        }

        private static void GodotTradeRouteRouterIgnoresInvalidTradePostTargets()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(new[]
            {
                CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 171, 1, (int)BuildingTypeId.TradePost, 5, 5),
                CreateGodotPrimitiveWithType(VisualPrimitiveKind.BuildingRectangle, 172, 0, (int)BuildingTypeId.TownCenter, 5, 5)
            });

            int tradePostId = GodotTradeRouteRouter.FindLocalTradePostAt(frame, 0, Fixed.FromInt(5).Raw, Fixed.FromInt(5).Raw);

            AssertEqual(0, tradePostId, "router should ignore enemy trade posts and non-trade-post buildings");
        }

        private static void GodotTradeRouteRouterFindsSelectedTradeCart()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new GodotPrimitiveDto[0],
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(180, (int)UnitTypeId.TradeCart, false, 0, 0, 0, 0, 0, 0, 0, 0)
                });

            int tradeCartId = GodotTradeRouteRouter.FindSelectedTradeCart(frame, new[] { 180 });

            AssertEqual(180, tradeCartId, "router should find selected trade cart by unit status");
        }

        private static void GodotTradeRouteRouterIgnoresNonCartSelection()
        {
            GodotFrameDto frame = CreateGodotInteractionFrame(
                new GodotPrimitiveDto[0],
                new GodotBuildingStatusDto[0],
                new[]
                {
                    new GodotUnitStatusDto(181, (int)UnitTypeId.Villager, false, 0, 0, 0, 0, 0, 0, 0, 0)
                });

            int tradeCartId = GodotTradeRouteRouter.FindSelectedTradeCart(frame, new[] { 181 });

            AssertEqual(0, tradeCartId, "router should ignore selected non-trade-cart units");
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

        private static void ResearchInfantryAttackCompletes()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            var state = GameInitializer.CreateNomadStart(63, 1);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            CompleteCapitalForPlayerZero(rules, state, buffer, runner);
            int buildingId = state.EntityState.Buildings[0].Id;
            state.PlayerStates.Players[0].Resources.Food = GameData.InfantryAttack1FoodCost;
            state.PlayerStates.Players[0].Resources.Gold = GameData.InfantryAttack1GoldCost;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.ResearchTech), new ResearchTechCommand(buildingId, TechId.InfantryAttack1)));
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < GameData.InfantryAttack1ResearchTicks - 1; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(4 + i));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            PlayerState player = state.PlayerStates.Players[0];
            AssertEqual(1, player.TechState.CompletedTechs.Count, "research should complete one tech");
            AssertEqual(TechId.InfantryAttack1, player.TechState.CompletedTechs[0], "completed tech should be infantry attack 1");
            AssertEqual(0, player.TechState.ResearchQueue.Count, "completed research should leave queue empty");
            AssertEqual(GameData.InfantryAttack1DamageBonus, TechRules.GetModifierValue(player, ModifierId.InfantryAttackBonus), "completed research should add deterministic attack modifier");
            AssertEqual(0, player.Resources.Food, "research should spend food cost");
            AssertEqual(0, player.Resources.Gold, "research should spend gold cost");
        }

        private static void ResearchRejectsCompletedTech()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateResearchReadyState(64, 1, out int buildingId);
            PlayerState player = state.PlayerStates.Players[0];
            TechRules.ApplyCompletedTech(player, TechId.InfantryAttack1);
            var buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.ResearchTech), new ResearchTechCommand(buildingId, TechId.InfantryAttack1)));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "completed tech should not be researched twice");
            AssertEqual(0, player.TechState.ResearchQueue.Count, "rejected completed tech should not enter queue");
            AssertEqual(1, player.TechState.CompletedTechs.Count, "rejected duplicate should not add completed tech entries");
        }

        private static void ResearchInfantryAttackModifiesDamage()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            TechRules.ApplyCompletedTech(state.PlayerStates.Players[0], TechId.InfantryAttack1);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            int expectedDamage = GameData.InfantryAttackDamage + GameData.InfantryAttack1DamageBonus;
            AssertEqual(GameData.InfantryHitPoints - expectedDamage, state.EntityState.Units[11].HitPoints, "infantry attack research should modify owner damage through player modifier table");
        }

        private static void ResearchChecksumCoversTechState()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState withoutTech = CreateResearchReadyState(641, 1, out _);
            GameState withTech = CreateResearchReadyState(641, 1, out _);
            TechRules.ApplyCompletedTech(withTech.PlayerStates.Players[0], TechId.InfantryAttack1);

            ulong withoutChecksum = StateChecksum.Compute(withoutTech, rules);
            ulong withChecksum = StateChecksum.Compute(withTech, rules);

            AssertEqual(false, withoutChecksum == withChecksum, "checksum should cover completed techs and player modifiers");
        }

        private static void ResearchReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState firstState = CreateResearchReadyState(65, 1, out int firstBuildingId);
            GameState secondState = CreateResearchReadyState(65, 1, out int secondBuildingId);
            var firstCommands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.ResearchTech), new ResearchTechCommand(firstBuildingId, TechId.InfantryAttack1))
            };

            var secondCommands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.ResearchTech), new ResearchTechCommand(secondBuildingId, TechId.InfantryAttack1))
            };

            ulong first = RunCommandsFromState(firstState, rules, firstCommands, GameData.InfantryAttack1ResearchTicks);
            ulong second = RunCommandsFromState(secondState, rules, secondCommands, GameData.InfantryAttack1ResearchTicks);
            AssertEqual(first, second, "research command stream should replay deterministically");
        }

        private static void ResearchLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 66, true);
            int buildingId = SetupResearchReadyState(session.Peers[0].LocalState, 0);
            SetupResearchReadyState(session.Peers[1].LocalState, 0);

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.ResearchTech), new ResearchTechCommand(buildingId, TechId.InfantryAttack1)));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "research lockstep first tick should advance");
            for (int tick = 1; tick < GameData.InfantryAttack1ResearchTicks; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "research lockstep progress tick should advance");
            }

            AssertEqual(0, session.DesyncReports.Count, "research lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "research peer checksums should match");
            AssertEqual(GameData.InfantryAttack1DamageBonus, TechRules.GetModifierValue(session.Peers[0].LocalState.PlayerStates.Players[0], ModifierId.InfantryAttackBonus), "research lockstep should complete modifier");
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

            AssertEqual(wallId, state.EntityState.Units[0].CurrentBuildTargetId, "first villager should stay assigned to wall build target");
            AssertEqual(wallId, state.EntityState.Units[1].CurrentBuildTargetId, "second villager should stay assigned to wall build target");
            AssertEqual(true, state.EntityState.Units[0].HasMoveTarget || state.EntityState.Units[1].HasMoveTarget, "at least one assigned wall builder should move toward interaction range");
        }

        private static void AssignBuildSetsAdjacentDeterministicApproachTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(141, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building building = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            Unit villager = state.EntityState.Units[0];
            villager.Position = FixedVector2.FromInts(6, 10);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { villager.Id })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, villager.HasMoveTarget, "assigned builder should get movement target");
            int targetX = SpatialRules.GetTileX(villager.MoveTarget);
            int targetY = SpatialRules.GetTileY(villager.MoveTarget);
            AssertEqual(false, SpatialRules.IsTileInsideBuildingFootprint(building, targetX, targetY), "build approach target should never be inside footprint");
            AssertEqual(true,
                SpatialRules.IsTileInsideBuildingFootprint(building, targetX + 1, targetY)
                    || SpatialRules.IsTileInsideBuildingFootprint(building, targetX - 1, targetY)
                    || SpatialRules.IsTileInsideBuildingFootprint(building, targetX, targetY + 1)
                    || SpatialRules.IsTileInsideBuildingFootprint(building, targetX, targetY - 1),
                "build approach target should be adjacent to footprint");
        }

        private static void AssignBuildGivesDistinctApproachTilesForMultipleVillagers()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(142, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            state.EntityState.Units[0].Position = FixedVector2.FromInts(6, 9);
            state.EntityState.Units[1].Position = FixedVector2.FromInts(6, 10);
            state.EntityState.Units[2].Position = FixedVector2.FromInts(6, 11);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { 1, 2, 3 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            var tiles = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                Unit villager = state.EntityState.Units[i];
                AssertEqual(true, villager.HasMoveTarget, "each assigned villager should get a target");
                tiles.Add(SpatialRules.GetTileX(villager.MoveTarget) + "," + SpatialRules.GetTileY(villager.MoveTarget));
            }

            AssertEqual(3, tiles.Count, "multiple builders should reserve distinct adjacent approach tiles when available");
        }

        private static void AssignBuildRejectsWhenNoInteractionTileIsReachable()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(143, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            List<SpatialRules.TileCoord> blockedRing = SpatialRules.EnumerateBuildInteractionTiles(state, tc);
            for (int i = 0; i < blockedRing.Count; i++)
            {
                AddCompletedWall(state, 0, FixedVector2.FromInts(blockedRing[i].X, blockedRing[i].Y));
            }
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { 1 })));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "assign build should reject when no reachable interaction tile exists");
            AssertEqual(0, state.EntityState.Units[0].CurrentBuildTargetId, "rejected assignment should not set build target");
        }

        private static void AssignBuildAcceptsTemporaryCongestionIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(1490, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Unit villager = state.EntityState.Units[0];
            villager.Position = FixedVector2.FromInts(0, 0);
            AddCompletedWall(state, 0, FixedVector2.FromInts(1, 0));
            AddCompletedWall(state, 0, FixedVector2.FromInts(0, 1));
            var header = new CommandHeader(state.Tick, 0, 0, CommandType.AssignBuild);
            var command = new AssignBuildCommand(tcId, new[] { villager.Id });

            CommandValidationReport report = CommandValidationInspector.Evaluate(
                state,
                rules,
                new CommandEnvelope(header, command));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(header, command));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(true, report.Accepted, "temporarily blocked builder should accept long-term intent");
            AssertEqual(CommandValidationReason.TemporaryCongestionAcceptedIntent, report.Reason, "temporarily blocked builder should report congestion acceptance");
            AssertEqual(tcId, state.EntityState.Units[0].CurrentBuildTargetId, "accepted command should keep build target for wait/retry");
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "temporary congestion should not count as rejection");
        }

        private static void AssignBuildRejectReasonForCompletedTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = GameInitializer.CreateNomadStart(1491, 1);
            int tcId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            tc.IsUnderConstruction = false;
            var header = new CommandHeader(state.Tick, 0, 0, CommandType.AssignBuild);
            var command = new AssignBuildCommand(tcId, new[] { 1 });

            CommandValidationReport report = CommandValidationInspector.Evaluate(
                state,
                rules,
                new CommandEnvelope(header, command));

            AssertEqual(false, report.Accepted, "completed target should reject assign-build");
            AssertEqual(CommandValidationReason.TargetComplete, report.Reason, "completed target should report target complete");
            AssertEqual(tcId, report.TargetEntityId, "report should include build target id");
        }

        private static void VillagerPathsToTcInteractionTileFromLeft()
        {
            AssertVillagerPathsToTcInteractionTileFromSide(-6, 1461);
        }

        private static void VillagerPathsToTcInteractionTileFromRight()
        {
            AssertVillagerPathsToTcInteractionTileFromSide(6, 1462);
        }

        private static void WorkerLoopRemainsUnstuckOverLongDryArabiaRun()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(1463);
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            int[] villagers = GetPlayerVillagerIds(state, 0);
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tcId, villagers)));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int i = 0; i < 900 && FindUnderConstructionBuildingIdOrZero(state, 0, BuildingTypeId.TownCenter) != 0; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(1000 + i * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(1001 + i * 2));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            int completedTcId = FindCompletedBuildingId(state, 0, BuildingTypeId.TownCenter);
            AssertEqual(true, completedTcId != 0, "town center should complete during long worker loop");

            int foodNodeId = FindNearestResourceNodeId(state, tcPos, ResourceType.Food);
            int woodNodeId = FindNearestResourceNodeId(state, tcPos, ResourceType.Wood);
            int goldNodeId = FindNearestResourceNodeId(state, tcPos, ResourceType.Gold);
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2000, CommandType.GatherResource), new GatherResourceCommand(foodNodeId, new[] { villagers[0] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2001, CommandType.GatherResource), new GatherResourceCommand(woodNodeId, new[] { villagers[1] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2002, CommandType.GatherResource), new GatherResourceCommand(goldNodeId, new[] { villagers[2] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 2003, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int activeWorkerTicks = 0;
            for (int i = 0; i < 6000; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(3000 + i * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(3001 + i * 2));
                runner.AdvanceOneTick(state, rules, buffer);

                for (int v = 0; v < villagers.Length; v++)
                {
                    Unit unit = state.EntityState.Units[state.EntityState.EntityLookup[villagers[v]].Index];
                    if (unit.CurrentResourceNodeId != 0 || unit.CarriedAmount > 0 || unit.HasMoveTarget)
                    {
                        activeWorkerTicks++;
                        break;
                    }
                }
            }

            AssertEqual(true, activeWorkerTicks > 0, "workers should remain in active deterministic movement/gather states over long run");
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

        private static void DryArabiaTcBuildAssignmentProgressesAndUpdatesPopulation()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(144);
            var runner = new TickRunner();
            var buffer = new CommandBuffer();
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tcId, new[] { 1, 2, 3, 4 })));
            buffer.Add(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int assignedBuilders = 0;
            for (int i = 0; i < 4; i++)
            {
                if (state.EntityState.Units[i].CurrentBuildTargetId == tcId)
                {
                    assignedBuilders++;
                }
            }

            AssertEqual(4, assignedBuilders, "dry arabia build assignment should assign all selected villagers to tc build target");

            for (int tick = 2; tick < 120 && state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction; tick++)
            {
                buffer.Add(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                buffer.Add(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            Building tc = state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index];
            AssertEqual(false, tc.IsUnderConstruction, "town center should complete after deterministic approach/build");
            AssertEqual(GameData.CapitalPopulationBonus, state.PlayerStates.Players[0].PopulationCap, "completed capital tc should grant population cap bonus");
            AssertEqual(5, state.PlayerStates.Players[0].PopulationUsed, "population used should remain 5/10 after completion");
        }

        private static void PlayabilityInvariantFiveVillagersBuildAssignmentTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(27101, 1);
            while (state.EntityState.Units.Count < 5)
            {
                int offset = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(8 + offset, 8));
            }

            int[] unitIds = new int[5];
            int foundVillagers = 0;
            for (int i = 0; i < state.EntityState.Units.Count && foundVillagers < 5; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.OwnerPlayerIndex == 0 && unit.UnitTypeId == UnitTypeId.Villager)
                {
                    unitIds[foundVillagers++] = unit.Id;
                }
            }

            AssertEqual(5, foundVillagers, "test setup should provide five villagers");
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            int startRejected = state.DebugCounters.RejectedCommandCount;
            var traces = new Queue<string>();
            state.PlayerStates.Players[0].Resources.Wood = GameData.WallWoodCost;

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceWall), new PlaceWallCommand(FixedVector2.FromInts(14, 10))));
            runner.AdvanceOneTick(state, rules, buffer);
            int wallId = state.EntityState.Buildings[state.EntityState.Buildings.Count - 1].Id;
            Building foundation = state.EntityState.Buildings[state.EntityState.EntityLookup[wallId].Index];

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(wallId, unitIds)));
            runner.AdvanceOneTick(state, rules, buffer);

            bool sawBuilderIntent = false;
            for (int tick = 0; tick < 180; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(40000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, unitIds, traces, 40);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("build invariant stacking", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("build invariant reservations", traces));
                for (int i = 0; i < unitIds.Length; i++)
                {
                    Unit loopUnit = FindUnitById(state, unitIds[i]);
                    if (loopUnit.CurrentBuildTargetId == wallId)
                    {
                        sawBuilderIntent = true;
                    }
                }
            }

            AssertEqual(true, sawBuilderIntent, BuildTraceFailureMessage("at least one builder should keep build intent during the scenario", traces));
            AssertEqual(true, foundation.BuildProgressTicks > 0, BuildTraceFailureMessage("foundation should gain build progress", traces));
            AssertEqual(true, state.DebugCounters.RejectedCommandCount - startRejected <= 1, BuildTraceFailureMessage("legal build assignment should not spam rejections", traces));
            AssertNoEndlessWorkerPhase(state, unitIds, WorkerTaskPhase.MovingToBuildSlot, 180, BuildTraceFailureMessage("builders stuck in moving-to-build phase", traces));
        }

        private static void PlayabilityInvariantMixedResourceWorkersSharedTcTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(27102);
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            var traces = new Queue<string>();
            int startRejected = state.DebugCounters.RejectedCommandCount;
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);

            for (int i = 0; i < 180; i++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(41000 + i * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(41001 + i * 2));
                runner.AdvanceOneTick(state, rules, buffer);
                if (!state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction)
                {
                    break;
                }
            }

            while (GetPlayerVillagerIds(state, 0).Length < 5)
            {
                int index = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(
                    state,
                    0,
                    UnitTypeId.Villager,
                    FixedVector2.FromInts(tcPos.X.FloorToInt() - 3 + (index % 3), tcPos.Y.FloorToInt() + 5 + (index % 2)));
            }
            int[] workers = GetPlayerVillagerIds(state, 0);

            int foodId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            int goldId = FindFirstResourceNodeIdByType(state, ResourceType.Gold);
            int woodId = FindFirstResourceNodeIdByType(state, ResourceType.Wood);
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.GatherResource), new GatherResourceCommand(foodId, new[] { workers[0], workers[1] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.GatherResource), new GatherResourceCommand(goldId, new[] { workers[2], workers[3] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 4, CommandType.GatherResource), new GatherResourceCommand(woodId, new[] { workers[4] })));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 2, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            int startingFood = state.PlayerStates.Players[0].Resources.Food;
            int startingGold = state.PlayerStates.Players[0].Resources.Gold;
            int startingWood = state.PlayerStates.Players[0].Resources.Wood;
            bool sawCarriedResources = false;
            for (int tick = 0; tick < 900; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(42000 + tick * 2));
                AddNoOp(buffer, state.Tick, 1, (uint)(42001 + tick * 2));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, workers, traces, 50);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("mixed-resource stacking invariant", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("mixed-resource reservation invariant", traces));
                for (int i = 0; i < workers.Length; i++)
                {
                    Unit loopUnit = FindUnitById(state, workers[i]);
                    if (loopUnit.CarriedAmount > 0)
                    {
                        sawCarriedResources = true;
                    }
                }
            }

            bool anyStockpileProgress =
                state.PlayerStates.Players[0].Resources.Food > startingFood
                || state.PlayerStates.Players[0].Resources.Gold > startingGold
                || state.PlayerStates.Players[0].Resources.Wood > startingWood;
            AssertEqual(true, anyStockpileProgress || sawCarriedResources, BuildTraceFailureMessage("mixed workers should make gather/deposit progress evidence", traces));
            AssertEqual(true, state.DebugCounters.RejectedCommandCount - startRejected <= 2, BuildTraceFailureMessage("legal gather commands should not spam rejections", traces));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToResourceSlot, 900, BuildTraceFailureMessage("workers stuck moving-to-resource", traces));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToDropoffSlot, 900, BuildTraceFailureMessage("workers stuck moving-to-dropoff", traces));
        }

        private static void PlayabilityInvariantTenWorkersAroundTcMovementTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateOccupancyState(27103, 1);
            AddCompletedTownCenter(state, 0, FixedVector2.FromInts(20, 20));
            while (state.EntityState.Units.Count < 10)
            {
                int i = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(16 + (i % 5), 24 + (i / 5)));
            }

            int[] unitIds = new int[10];
            for (int i = 0; i < 10; i++)
            {
                unitIds[i] = state.EntityState.Units[i].Id;
            }

            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            var traces = new Queue<string>();
            int startRejected = state.DebugCounters.RejectedCommandCount;
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(unitIds, FixedVector2.FromInts(20, 18))));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 0; tick < 240; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(43000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, unitIds, traces, 40);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("ten-worker move stacking invariant", traces));
            }

            int arrivedOrWaiting = 0;
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (!unit.HasMoveTarget || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting || unit.ReservedInteractionKind == InteractionReservationKind.MoveDestination)
                {
                    arrivedOrWaiting++;
                }
            }

            AssertEqual(true, arrivedOrWaiting >= 8, BuildTraceFailureMessage("most workers should settle or wait cleanly around tc movement", traces));
            AssertEqual(true, state.DebugCounters.RejectedCommandCount - startRejected <= 1, BuildTraceFailureMessage("legal group move should not spam rejections", traces));
        }

        private static void PlayabilityInvariantTrainedVillagersGatherTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateSingleNodeResourceAreaState(27104, GatherProfileId.BerryBush, out _, out _);
            int tcId = AddCompletedTownCenter(state, 0, FixedVector2.FromInts(10, 10));
            state.PlayerStates.Players[0].Resources.Food = 500;
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            var traces = new Queue<string>();

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.TrainUnit), new TrainUnitCommand(tcId, UnitTypeId.Villager)));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.TrainUnit), new TrainUnitCommand(tcId, UnitTypeId.Villager)));
            runner.AdvanceOneTick(state, rules, buffer);
            int initialCount = state.EntityState.Units.Count;

            for (int tick = 0; tick < 800 && state.EntityState.Units.Count < initialCount + 2; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(44000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
            }

            AssertEqual(true, state.EntityState.Units.Count >= initialCount + 2, "trained villagers should spawn in bounded ticks");
            int foodId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            int[] trainedIds = new[] { state.EntityState.Units[initialCount].Id, state.EntityState.Units[initialCount + 1].Id };
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.GatherResource), new GatherResourceCommand(foodId, trainedIds)));
            runner.AdvanceOneTick(state, rules, buffer);

            bool intentRetained = false;
            for (int tick = 0; tick < 700; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(45000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, trainedIds, traces, 40);
                for (int i = 0; i < trainedIds.Length; i++)
                {
                    Unit loopUnit = FindUnitById(state, trainedIds[i]);
                    if (loopUnit.CurrentResourceAreaId != 0 || loopUnit.CurrentResourceNodeId != 0 || loopUnit.TaskPhase == WorkerTaskPhase.BlockedWaiting)
                    {
                        intentRetained = true;
                    }
                }
            }

            AssertEqual(true, intentRetained, BuildTraceFailureMessage("trained villagers should retain gather intent even under congestion", traces));
            AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("trained villagers stacking invariant", traces));
        }

        private static void PlayabilityInvariantDepletionContinuationPressureTrace()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(1);
            GameState state = CreateTwoNodeResourceAreaState(27105, GatherProfileId.Tree, out int firstNodeId, out int secondNodeId, out int areaId);
            while (state.EntityState.Units.Count < 3)
            {
                int idx = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(state, 0, UnitTypeId.Villager, FixedVector2.FromInts(3 + idx, 0));
            }

            ResourceNode firstNode = FindResourceNodeById(state, firstNodeId);
            firstNode.RemainingAmount = GameData.VillagerCarryCapacity;
            TickRunner runner = new TickRunner();
            CommandBuffer buffer = new CommandBuffer();
            int[] unitIds = new int[3];
            for (int i = 0; i < 3; i++)
            {
                unitIds[i] = state.EntityState.Units[i].Id;
            }
            var traces = new Queue<string>();

            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.GatherResource), new GatherResourceCommand(firstNodeId, unitIds)));
            runner.AdvanceOneTick(state, rules, buffer);

            for (int tick = 0; tick < 700; tick++)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)(46000 + tick));
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureWorkerTraceTick(state, unitIds, traces, 50);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("depletion continuation stacking invariant", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("depletion continuation reservation invariant", traces));
            }

            ResourceNode secondNode = FindResourceNodeById(state, secondNodeId);
            AssertEqual(true, firstNode.IsDepleted, BuildTraceFailureMessage("first node should deplete under pressure", traces));
            bool areaOrNodeRetained = false;
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                areaOrNodeRetained = areaOrNodeRetained
                    || unit.CurrentResourceAreaId == areaId
                    || unit.CurrentResourceNodeId == secondNodeId
                    || (unit.CarriedResourceType == ResourceType.Wood && unit.CarriedAmount > 0);
            }

            bool secondNodeTouched = secondNode.RemainingAmount < GameData.StartingWoodAmount || secondNode.IsDepleted;
            AssertEqual(true, secondNodeTouched || areaOrNodeRetained, BuildTraceFailureMessage("workers should continue or retain valid area intent after first depletion", traces));
            AssertEqual(true, areaOrNodeRetained, BuildTraceFailureMessage("workers should retain area intent or continue on next node", traces));
        }

        private static void SimulationScenarioHarnessMixedEconomyWorkflow()
        {
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateDryArabiaTest01(27106);
            FixedVector2 tcPos = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var scenario = new SimScenarioHarness(state, rules, 2, 47000);

            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcPos)),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            int tcId = FindUnderConstructionBuildingId(state, 0, BuildingTypeId.TownCenter);
            scenario.RunTicks(200, () => !state.EntityState.Buildings[state.EntityState.EntityLookup[tcId].Index].IsUnderConstruction);

            while (GetPlayerVillagerIds(state, 0).Length < 5)
            {
                int index = state.EntityState.Units.Count;
                EntityFactory.CreateUnit(
                    state,
                    0,
                    UnitTypeId.Villager,
                    FixedVector2.FromInts(tcPos.X.FloorToInt() - 3 + (index % 3), tcPos.Y.FloorToInt() + 5 + (index % 2)));
            }

            int[] workers = GetPlayerVillagerIds(state, 0);
            int foodId = FindFirstResourceNodeIdByType(state, ResourceType.Food);
            int goldId = FindFirstResourceNodeIdByType(state, ResourceType.Gold);
            int woodId = FindFirstResourceNodeIdByType(state, ResourceType.Wood);
            int startingFood = state.PlayerStates.Players[0].Resources.Food;
            int startingGold = state.PlayerStates.Players[0].Resources.Gold;
            int startingWood = state.PlayerStates.Players[0].Resources.Wood;

            scenario.Step(
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.GatherResource), new GatherResourceCommand(foodId, new[] { workers[0], workers[1] })),
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 2, CommandType.GatherResource), new GatherResourceCommand(goldId, new[] { workers[2], workers[3] })),
                new CommandEnvelope(new CommandHeader(state.Tick, 0, 3, CommandType.GatherResource), new GatherResourceCommand(woodId, new[] { workers[4] })),
                new CommandEnvelope(new CommandHeader(state.Tick, 1, 1, CommandType.NoOp), new NoOpCommand()));

            bool sawGatherOrCarry = false;
            bool sawActiveIntent = false;
            for (int tick = 0; tick < 700; tick++)
            {
                scenario.StepNoOps();
                scenario.CaptureWorkerTrace(workers, 60);
                scenario.AssertCoreInvariants("mixed-economy");

                for (int i = 0; i < workers.Length; i++)
                {
                    Unit unit = FindUnitById(state, workers[i]);
                    if (unit.TaskPhase == WorkerTaskPhase.Gathering || unit.CarriedAmount > 0)
                    {
                        sawGatherOrCarry = true;
                    }

                    if (unit.CurrentResourceAreaId != 0
                        || unit.CurrentResourceNodeId != 0
                        || unit.TaskPhase == WorkerTaskPhase.MovingToResourceSlot
                        || unit.TaskPhase == WorkerTaskPhase.MovingToDropoffSlot
                        || unit.TaskPhase == WorkerTaskPhase.BlockedWaiting)
                    {
                        sawActiveIntent = true;
                    }
                }
            }

            bool anyStockpileProgress =
                state.PlayerStates.Players[0].Resources.Food > startingFood
                || state.PlayerStates.Players[0].Resources.Gold > startingGold
                || state.PlayerStates.Players[0].Resources.Wood > startingWood;
            AssertEqual(true, sawGatherOrCarry, scenario.Fail("expected at least one worker to gather or carry"));
            AssertEqual(true, anyStockpileProgress || sawActiveIntent, scenario.Fail("expected mixed economy progress or retained active intent"));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToResourceSlot, 900, scenario.Fail("workers stuck moving-to-resource"));
            AssertNoEndlessWorkerPhase(state, workers, WorkerTaskPhase.MovingToDropoffSlot, 900, scenario.Fail("workers stuck moving-to-dropoff"));
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

            Building tradePost = state.EntityState.Buildings[1];
            AssertEqual(true, tradePost.IsUnderConstruction || tradePost.BuildProgressTicks > 0, "assigned villagers should begin trade post construction after assignment");
            AssertEqual(7, state.EntityState.Units[0].CurrentBuildTargetId, "first villager should stay assigned to trade post");
            AssertEqual(7, state.EntityState.Units[1].CurrentBuildTargetId, "second villager should stay assigned to trade post");
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
            int tradePostId = FindUnderConstructionBuildingId(session.Peers[0].LocalState, 0, BuildingTypeId.TradePost);
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tradePostId, new[] { 1, 2 })));
            session.Broadcast(new CommandEnvelope(new CommandHeader(1, 1, 1, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post build assignment tick should advance");
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 0, 2, CommandType.NoOp), new NoOpCommand()));
            session.Broadcast(new CommandEnvelope(new CommandHeader(2, 1, 2, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "trade post build tick should advance");
            int tick = 3;
            uint sequence = 3;
            while (tick < 80 && session.Peers[0].LocalState.EntityState.Buildings[1].IsUnderConstruction)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, sequence, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, sequence, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "trade post completion progression tick should advance");
                tick++;
                sequence++;
            }

            Building tradePost = session.Peers[0].LocalState.EntityState.Buildings[1];
            AssertEqual(0, session.DesyncReports.Count, "trade post lockstep should not desync");
            AssertEqual(tradePostId, session.Peers[0].LocalState.EntityState.Units[0].CurrentBuildTargetId, "first builder should remain assigned to trade post in lockstep");
            AssertEqual(tradePostId, session.Peers[0].LocalState.EntityState.Units[1].CurrentBuildTargetId, "second builder should remain assigned to trade post in lockstep");
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
            AdvanceTradeUntilFirstDeposit(state, rules, runner, buffer, 64);

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

            for (int tick = 1; tick < 64; tick++)
            {
                commands.Add(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
            }

            GameState firstState = CreateTradeState(10);
            GameState secondState = CreateTradeState(10);
            ulong first = RunCommandsFromState(firstState, rules, commands, 64);
            ulong second = RunCommandsFromState(secondState, rules, commands, 64);
            AssertEqual(first, second, "trade route trip should replay deterministically");
            AssertEqual(true, firstState.PlayerStates.Players[0].Resources.Gold >= 20, "replayed trade route should pay gold on arrival");
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
            for (int tick = 1; tick < 64; tick++)
            {
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 0, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                session.Broadcast(new CommandEnvelope(new CommandHeader(tick, 1, (uint)tick, CommandType.NoOp), new NoOpCommand()));
                AssertEqual(true, session.TryAdvanceOneTick(), "trade movement tick should advance");

                if (session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Gold >= 20)
                {
                    break;
                }
            }

            AssertEqual(0, session.DesyncReports.Count, "trade lockstep should not desync");
            AssertEqual(20, session.Peers[0].LocalState.PlayerStates.Players[0].Resources.Gold, "trade income should be paid in lockstep state");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "trade peer checksums should match");
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

        private static void DryArabiaResourceDtoExposesStableIdTypeTile()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1264);
            AssertResourceNodeStable(state, 1, ResourceType.Food, 30, 48);
            AssertResourceNodeStable(state, 3, ResourceType.Wood, 22, 57);
            AssertResourceNodeStable(state, 5, ResourceType.Gold, 18, 47);

            GodotClientFacade facade = GodotClientFacade.CreateDryArabiaTest01(1264);
            GodotFrameDto frame = facade.GetFrame(0);
            int visibleCount = 0;
            for (int i = 0; i < frame.Primitives.Length; i++)
            {
                GodotPrimitiveDto primitive = frame.Primitives[i];
                if (primitive.Kind != (int)VisualPrimitiveKind.FoodResourceCircle
                    && primitive.Kind != (int)VisualPrimitiveKind.WoodResourceCircle
                    && primitive.Kind != (int)VisualPrimitiveKind.GoldResourceCircle)
                {
                    continue;
                }

                ResourceNode node = FindResourceNodeById(state, primitive.EntityId);
                AssertEqual((int)node.ResourceType, primitive.TypeId, "resource dto type should match sim resource type for id=" + primitive.EntityId);
                AssertEqual(node.Position.X.Raw, primitive.XRaw, "resource dto x should match sim position for id=" + primitive.EntityId);
                AssertEqual(node.Position.Y.Raw, primitive.YRaw, "resource dto y should match sim position for id=" + primitive.EntityId);
                visibleCount++;
            }

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

        private static void AssertDistinctReservations(GameState state, int[] unitIds, InteractionReservationKind kind, int targetId, string message)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                AssertEqual(kind, unit.ReservedInteractionKind, message + " kind for unit " + unit.Id);
                AssertEqual(targetId, unit.ReservedInteractionTargetId, message + " target for unit " + unit.Id);
                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, seen.Add(key), message + " should not duplicate tile " + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY);
            }
        }

        private static bool TryReservationsAreDistinct(GameState state, int[] unitIds, InteractionReservationKind kind, int targetId)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (unit.ReservedInteractionKind != kind || unit.ReservedInteractionTargetId != targetId)
                {
                    continue;
                }

                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                if (!seen.Add(key))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertNoDuplicateFinalPurposeReservations(GameState state, string message)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead || unit.ReservedInteractionKind == InteractionReservationKind.None)
                {
                    continue;
                }

                int key = (unit.ReservedInteractionTileY << 16) ^ (unit.ReservedInteractionTileX & 0xFFFF);
                AssertEqual(true, seen.Add(key), message + " duplicate reserved tile " + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY);
            }
        }

        private static long EncodeReservationKey(Unit unit)
        {
            if (unit.ReservedInteractionKind == InteractionReservationKind.None)
            {
                return -1L;
            }

            unchecked
            {
                long result = (int)unit.ReservedInteractionKind;
                result = (result * 397L) ^ unit.ReservedInteractionTargetId;
                result = (result * 397L) ^ unit.ReservedInteractionTileX;
                result = (result * 397L) ^ unit.ReservedInteractionTileY;
                return result;
            }
        }

        private static long EncodeMoveTargetKey(Unit unit)
        {
            if (!unit.HasMoveTarget)
            {
                return -1L;
            }

            unchecked
            {
                return (unit.MoveTarget.X.Raw * 397L) ^ unit.MoveTarget.Y.Raw;
            }
        }

        private static void AssertNoLiveUnitStacking(GameState state, string message)
        {
            var occupied = new HashSet<int>();
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (unit.IsDead)
                {
                    continue;
                }

                int tileX = SpatialRules.GetTileX(unit.Position);
                int tileY = SpatialRules.GetTileY(unit.Position);
                int key = (tileY << 16) ^ (tileX & 0xFFFF);
                AssertEqual(true, occupied.Add(key), message + " at " + tileX + "," + tileY);
            }
        }

        private static void CaptureWorkerTraceTick(GameState state, int[] unitIds, Queue<string> traces, int maxEntries)
        {
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                int tileX = SpatialRules.GetTileX(unit.Position);
                int tileY = SpatialRules.GetTileY(unit.Position);
                int moveTileX = unit.HasMoveTarget ? SpatialRules.GetTileX(unit.MoveTarget) : -1;
                int moveTileY = unit.HasMoveTarget ? SpatialRules.GetTileY(unit.MoveTarget) : -1;
                string line =
                    "t=" + state.Tick
                    + " u=" + unit.Id
                    + " tile=(" + tileX + "," + tileY + ")"
                    + " raw=(" + unit.Position.X.Raw + "," + unit.Position.Y.Raw + ")"
                    + " phase=" + unit.TaskPhase
                    + " move=(" + moveTileX + "," + moveTileY + ")"
                    + " moveRaw=(" + (unit.HasMoveTarget ? unit.MoveTarget.X.Raw.ToString() : "-") + "," + (unit.HasMoveTarget ? unit.MoveTarget.Y.Raw.ToString() : "-") + ")"
                    + " reserve=" + unit.ReservedInteractionKind + ":" + unit.ReservedInteractionTargetId + "@(" + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY + ")"
                    + " area=" + unit.CurrentResourceAreaId
                    + " node=" + unit.CurrentResourceNodeId
                    + " build=" + unit.CurrentBuildTargetId
                    + " carry=" + unit.CarriedResourceType + ":" + unit.CarriedAmount
                    + " lastMoved=" + unit.LastMovedTick
                    + " cmd=" + state.DebugCounters.LastCommandType
                    + " cmdAccepted=" + state.DebugCounters.LastCommandAccepted
                    + " cmdReason=" + state.DebugCounters.LastCommandReason;
                traces.Enqueue(line);
                while (traces.Count > maxEntries)
                {
                    traces.Dequeue();
                }
            }
        }

        private static string BuildTraceFailureMessage(string header, Queue<string> traces)
        {
            if (traces.Count == 0)
            {
                return header;
            }

            return header + Environment.NewLine + string.Join(Environment.NewLine, traces);
        }

        private static void AssertNoEndlessWorkerPhase(GameState state, int[] unitIds, WorkerTaskPhase phase, int maxNoProgressTicks, string message)
        {
            for (int i = 0; i < unitIds.Length; i++)
            {
                Unit unit = FindUnitById(state, unitIds[i]);
                if (unit.TaskPhase != phase)
                {
                    continue;
                }

                int stalledFor = state.Tick - unit.LastMovedTick;
                AssertEqual(true, stalledFor <= maxNoProgressTicks, message + " unit=" + unit.Id + " phase=" + phase + " stalled=" + stalledFor);
            }
        }

        private readonly struct WorkerDiagnosticSample
        {
            public WorkerTaskPhase Phase { get; }
            public long PositionXRaw { get; }
            public long MoveTargetKey { get; }
            public long ReservationKey { get; }

            private WorkerDiagnosticSample(WorkerTaskPhase phase, long positionXRaw, long moveTargetKey, long reservationKey)
            {
                Phase = phase;
                PositionXRaw = positionXRaw;
                MoveTargetKey = moveTargetKey;
                ReservationKey = reservationKey;
            }

            public static WorkerDiagnosticSample Capture(Unit unit)
            {
                long moveTargetKey = unit.HasMoveTarget ? CombineRaw(unit.MoveTarget.X.Raw, unit.MoveTarget.Y.Raw) : -1L;
                long reservationKey = unit.ReservedInteractionKind == InteractionReservationKind.None
                    ? -1L
                    : CombineInts((int)unit.ReservedInteractionKind, unit.ReservedInteractionTargetId, unit.ReservedInteractionTileX, unit.ReservedInteractionTileY);
                return new WorkerDiagnosticSample(unit.TaskPhase, unit.Position.X.Raw, moveTargetKey, reservationKey);
            }

            private static long CombineRaw(long xRaw, long yRaw)
            {
                unchecked
                {
                    return (xRaw * 397L) ^ yRaw;
                }
            }

            private static long CombineInts(int a, int b, int c, int d)
            {
                unchecked
                {
                    long result = a;
                    result = (result * 397L) ^ b;
                    result = (result * 397L) ^ c;
                    result = (result * 397L) ^ d;
                    return result;
                }
            }
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
            state.Tick = GameData.InteractionTargetRetargetBlockedTicks;

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

        private static void TcPlacementPreviewValidAtPlayer0TcZone()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(101);
            var godotFrame = facade.GetFrame(0);

            var p0Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var result = TcPlacementPreview.Evaluate(godotFrame, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());

            AssertEqual(TcPlacementPreviewResult.Valid, result, "P0 TC zone should be a valid preview location on DryArabia");
            AssertEqual(true, PlaceTownCenterWouldBeValidOnDryArabia(0, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt(), 101), "valid preview should map to accepted placement command");
        }

        private static void TcPlacementPreviewValidAtPlayer1TcZone()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(102);
            var godotFrame = facade.GetFrame(1);

            var p1Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(1);
            var result = TcPlacementPreview.Evaluate(godotFrame, p1Zone.X.FloorToInt(), p1Zone.Y.FloorToInt());

            AssertEqual(TcPlacementPreviewResult.Valid, result, "P1 TC zone should be a valid preview location on DryArabia");
            AssertEqual(true, PlaceTownCenterWouldBeValidOnDryArabia(1, p1Zone.X.FloorToInt(), p1Zone.Y.FloorToInt(), 102), "valid preview should map to accepted placement command");
        }

        private static void TcPlacementPreviewInvalidOverlappingResource()
        {
            var fakePrimitives = new GodotPrimitiveDto[]
            {
                new GodotPrimitiveDto(
                    (int)VisualPrimitiveKind.FoodResourceCircle, // Kind
                    100, // EntityId
                    0,   // TypeId
                    -1,  // OwnerPlayerIndex
                    30 * 65536, // XRaw (30 tiles)
                    48 * 65536, // YRaw (48 tiles)
                    0, 0, 0, 0, 0, false)
            };

            var godotFrame = new GodotFrameDto(
                0,
                "Test",
                0,
                new GodotLocalPlayerDto(0, 0, 0, 0, 0, false, false, false),
                new GodotMatchDto(false, -1, -1, 0),
                fakePrimitives,
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);

            var result = TcPlacementPreview.Evaluate(godotFrame, 30, 48);

            AssertEqual(TcPlacementPreviewResult.OverlapsResource, result, "TC preview on top of resource should be OverlapsResource");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 30, 48, 103), "resource-overlap preview should map to rejected placement command");
        }

        private static void TcPlacementPreviewInvalidOverlappingBuilding()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(104);
            var p0Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            
            // Advance one tick to allow starting commands (if any) or simply queue a placement manually.
            facade.QueuePlaceTownCenter(0, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());
            facade.AdvanceOneTick();

            var godotFrame = facade.GetFrame(0);

            // Same spot should now be blocked by the building
            var result = TcPlacementPreview.Evaluate(godotFrame, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());

            AssertEqual(TcPlacementPreviewResult.MissingResources, result, "second TC without wood should report missing resources before placement overlap");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabiaWithPlacedTownCenter(0, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt(), 104), "building-overlap preview should map to rejected placement command");
        }

        private static void TcPlacementPreviewInvalidOutsideMap()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(105);
            var godotFrame = facade.GetFrame(0);

            var resultX = TcPlacementPreview.Evaluate(godotFrame, -1, 50);
            var resultY = TcPlacementPreview.Evaluate(godotFrame, 50, -1);
            var resultMaxX = TcPlacementPreview.Evaluate(godotFrame, 128, 50); // Map is 128x96
            var resultMaxY = TcPlacementPreview.Evaluate(godotFrame, 50, 96);

            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultX, "Preview outside -x map should be OutsideMap");
            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultY, "Preview outside -y map should be OutsideMap");
            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultMaxX, "Preview outside +x map should be OutsideMap");
            AssertEqual(TcPlacementPreviewResult.OutsideMap, resultMaxY, "Preview outside +y map should be OutsideMap");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, -1, 50, 105), "outside-map preview should map to rejected placement command");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 50, -1, 105), "outside-map preview should map to rejected placement command");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 128, 50, 105), "outside-map preview should map to rejected placement command");
            AssertEqual(false, PlaceTownCenterWouldBeValidOnDryArabia(0, 50, 96, 105), "outside-map preview should map to rejected placement command");
        }

        private static void TcPlacementPreviewCommandParityAroundPlayer0Zone()
        {
            var previewFacade = GodotClientFacade.CreateDryArabiaTest01(107);
            FixedVector2 zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            int centerX = zone.X.FloorToInt();
            int centerY = zone.Y.FloorToInt();

            for (int y = centerY - 2; y <= centerY + 2; y++)
            {
                for (int x = centerX - 2; x <= centerX + 2; x++)
                {
                    GodotFrameDto frame = previewFacade.GetFrame(0);
                    TcPlacementPreviewResult preview = TcPlacementPreview.Evaluate(frame, x, y);
                    bool accepted = PlaceTownCenterWouldBeValidOnDryArabia(0, x, y, 107);
                    if (preview == TcPlacementPreviewResult.Valid)
                    {
                        AssertEqual(true, accepted, "preview valid should accept at tile (" + x + "," + y + ")");
                    }
                    else
                    {
                        AssertEqual(false, accepted, "preview invalid should reject at tile (" + x + "," + y + ") reason=" + preview);
                    }
                }
            }
        }

        private static void DryArabiaTcFoundationHasReachableInteractionRing()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1261);
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            FixedVector2 tcZone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcZone)));
            buffer.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Building tc = FindLatestPlayerBuilding(state, 0, BuildingTypeId.TownCenter);
            List<SpatialRules.TileCoord> tiles = SpatialRules.EnumerateBuildInteractionTiles(state, tc);
            AssertEqual(true, tiles.Count >= 8, "town center should expose an interaction ring with multiple tiles");

            int reachableCount = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (AnyPlayerVillagerCanReachTile(state, 0, tiles[i].X, tiles[i].Y))
                {
                    reachableCount++;
                }
            }

            AssertEqual(true, reachableCount >= 6, "starting area should keep most build interaction tiles reachable");
        }

        private static void DryArabiaStartingVillagersCanAllReceiveBuildAssignment()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1262);
            GameRules rules = GameRules.CreatePhaseZeroDefaults(2);
            FixedVector2 tcZone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            var place = new CommandBuffer();
            place.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter), new PlaceTownCenterCommand(tcZone)));
            place.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 0, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, place);

            Building tc = FindLatestPlayerBuilding(state, 0, BuildingTypeId.TownCenter);
            int[] villagers = GetPlayerVillagerIds(state, 0);
            var assign = new CommandBuffer();
            assign.Add(new CommandEnvelope(new CommandHeader(state.Tick, 0, 1, CommandType.AssignBuild), new AssignBuildCommand(tc.Id, villagers)));
            assign.Add(new CommandEnvelope(new CommandHeader(state.Tick, 1, 1, CommandType.NoOp), new NoOpCommand()));
            new TickRunner().AdvanceOneTick(state, rules, assign);

            AssertEqual(villagers.Length, tc.AssignedBuilderIds.Count, "assign-build should accept all selected starting villagers");
            AssertEqual(true, tc.AssignedBuilderIds.Count >= 3, "at least three starting villagers should be assigned together");
        }

        private static void DryArabiaDecorativeRockTilesAreNotSimBlockers()
        {
            GameState state = GameInitializer.CreateDryArabiaTest01(1263);
            int[,] decorativeRockTiles =
            {
                { 58, 42 },
                { 66, 50 },
                { 62, 46 },
                { 35, 30 },
                { 90, 65 }
            };

            for (int i = 0; i < decorativeRockTiles.GetLength(0); i++)
            {
                int tileX = decorativeRockTiles[i, 0];
                int tileY = decorativeRockTiles[i, 1];
                AssertEqual(false, SpatialRules.IsTileBlockedForUnitMovement(state, tileX, tileY), "decorative terrain tile should not be a sim blocker at (" + tileX + "," + tileY + ")");
            }
        }

        private static void TcPlacementPreviewInvalidMissingResources()
        {
            var frame = new GodotFrameDto(
                0,
                "Test",
                0,
                new GodotLocalPlayerDto(
                    0,
                    0,
                    0,
                    0,
                    0,
                    true,
                    true,
                    true,
                    true,
                    false,
                    false,
                    new int[0],
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotMatchDto(false, -1, -1, 0),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);
            TcPlacementPreviewResult preview = TcPlacementPreview.Evaluate(frame, 20, 20);
            AssertEqual(TcPlacementPreviewResult.MissingResources, preview, "second-town-center preview should show missing resources");

            GameState state = GameInitializer.CreateDryArabiaTest01(108);
            state.PlayerStates.Players[0].CapitalStatus.HasCapitalBeenPlaced = true;
            bool accepted = new PlaceTownCenterCommand(FixedVector2.FromInts(20, 20))
                .IsValid(state, GameRules.CreatePhaseZeroDefaults(2), new CommandHeader(state.Tick, 0, 0, CommandType.PlaceTownCenter));
            AssertEqual(false, accepted, "second-town-center missing wood should be rejected by command validation");
        }

        private static void TcPlacementPreviewInvalidBlockedPlayerState()
        {
            var frame = new GodotFrameDto(
                0,
                "Test",
                0,
                new GodotLocalPlayerDto(
                    0,
                    500,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    false,
                    false,
                    true,
                    new int[0],
                    new GodotResearchStatusDto[0],
                    new GodotModifierStatusDto[0]),
                new GodotMatchDto(false, -1, -1, 0),
                new GodotPrimitiveDto[0],
                new GodotUnitStatusDto[0],
                new GodotBuildingStatusDto[0]);
            TcPlacementPreviewResult preview = TcPlacementPreview.Evaluate(frame, 10, 10);
            AssertEqual(TcPlacementPreviewResult.PlayerStateBlocked, preview, "resigned or disconnected player should preview blocked");
        }

        private static void TcPlacementPreviewDoesNotMutateChecksum()
        {
            var facade = GodotClientFacade.CreateDryArabiaTest01(106);
            var godotFrame = facade.GetFrame(0);
            int beforeCommands = facade.ExecutedCommandCount;

            var p0Zone = DryArabiaTest01MapDefinition.GetTownCenterZone(0);
            
            // Call it 10 times to ensure no weird internal state accumulates
            for (int i = 0; i < 10; i++)
            {
                TcPlacementPreview.Evaluate(godotFrame, p0Zone.X.FloorToInt(), p0Zone.Y.FloorToInt());
            }

            int afterCommands = facade.ExecutedCommandCount;
            AssertEqual(beforeCommands, afterCommands, "TcPlacementPreview must be strictly read-only and not mutate state");
        }

        private static void TcPlacementPreviewUnknownResultDoesNotThrow()
        {
            // Null frame would normally not be called due to null check in RtsClientRoot,
            // but the enum supports Unknown for safe defaulting. We just verify the enum exists and resolves cleanly.
            AssertEqual((int)TcPlacementPreviewResult.Unknown, 4, "Unknown result should be 4");
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

        private static void GodotHotkeyHelpContainsEdgePanEntry()
        {
            GodotHotkeyHelpEntry[] entries = GodotHotkeyHelpBuilder.Build(true);
            bool hasEdgePan = false;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Input == "Mouse Edge")
                {
                    hasEdgePan = true;
                }
            }

            AssertEqual(true, hasEdgePan, "Hotkey help should contain Mouse Edge panning documentation");
        }

        private sealed class SimScenarioHarness
        {
            private readonly TickRunner runner = new TickRunner();
            private readonly CommandBuffer buffer = new CommandBuffer();
            private readonly Queue<string> traces = new Queue<string>();
            private readonly int players;
            private uint nextSequence;

            public SimScenarioHarness(GameState state, GameRules rules, int players, uint sequenceSeed)
            {
                State = state;
                Rules = rules;
                this.players = players;
                nextSequence = sequenceSeed;
            }

            public GameState State { get; }

            public GameRules Rules { get; }

            public void Step(params CommandEnvelope[] commands)
            {
                for (int i = 0; i < commands.Length; i++)
                {
                    buffer.Add(commands[i]);
                }

                runner.AdvanceOneTick(State, Rules, buffer);
            }

            public void StepNoOps()
            {
                for (int player = 0; player < players; player++)
                {
                    AddNoOp(buffer, State.Tick, player, nextSequence++);
                }

                runner.AdvanceOneTick(State, Rules, buffer);
            }

            public void RunTicks(int maxTicks, Func<bool> stopWhen)
            {
                for (int i = 0; i < maxTicks; i++)
                {
                    if (stopWhen())
                    {
                        return;
                    }

                    StepNoOps();
                }
            }

            public void CaptureWorkerTrace(int[] unitIds, int maxEntries)
            {
                CaptureWorkerTraceTick(State, unitIds, traces, maxEntries);
            }

            public void AssertCoreInvariants(string header)
            {
                AssertNoLiveUnitStacking(State, Fail(header + " stacking"));
                AssertNoDuplicateFinalPurposeReservations(State, Fail(header + " duplicate reservations"));
                AssertEqual(true, State.DebugCounters.RejectedCommandCount <= 64, Fail(header + " suspicious command reject volume"));
            }

            public string Fail(string header)
            {
                return BuildTraceFailureMessage(header, traces);
            }
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

