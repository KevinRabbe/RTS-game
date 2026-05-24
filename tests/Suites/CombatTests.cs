using System;
using System.Collections.Generic;
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

namespace RtsGame.Tests
{
    public static partial class Program
    {
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

        private static void AttackOutOfRangePreservesIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = CreateAdjacentCombatState();
            Unit attacker = state.EntityState.Units[10];
            Unit target = state.EntityState.Units[11];
            target.Position = FixedVector2.FromInts(10, 0);
            int targetHitPointsBefore = target.HitPoints;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { attacker.Id }, target.Id)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(targetHitPointsBefore, target.HitPoints, "out-of-range attack should not apply damage");
            AssertEqual(target.Id, attacker.AttackTargetId, "out-of-range attack should preserve explicit attack target intent");
        }

        private static void DeadTargetClearsAttackIntentOnNextTick()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = CreateAdjacentCombatState();
            Unit attacker = state.EntityState.Units[10];
            Unit target = state.EntityState.Units[11];
            target.HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { attacker.Id }, target.Id)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(target.Id), "target should be removed during cleanup after lethal damage");

            AddNoOp(buffer, 1, 0, 1);
            AddNoOp(buffer, 1, 1, 1);
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(0, attacker.AttackTargetId, "attacker should clear explicit target when target id is no longer valid");
        }

        private static void AttackRejectsNonCombatUnit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int tradeCartId = EntityFactory.CreateUnit(state, 0, UnitTypeId.TradeCart, FixedVector2.FromInts(0, 0));
            int enemyInfantryId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { tradeCartId }, enemyInfantryId)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "non-combat unit attack should reject");
            AssertEqual(GameData.InfantryHitPoints, FindUnitById(state, enemyInfantryId).HitPoints, "rejected non-combat attack should not damage enemy");
        }

        private static void AttackOutOfRangeQueuesAttackSlotMovement()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int attackerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            int targetId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(7, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { attackerId }, targetId)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit attacker = FindUnitById(state, attackerId);
            AssertEqual(targetId, attacker.AttackTargetId, "attacker should retain explicit target");
            AssertEqual(true, attacker.HasMoveTarget, "out-of-range attacker should receive move target to attack slot");
            AssertEqual(WorkerTaskPhase.MovingToAttackSlot, attacker.TaskPhase, "attacker should use attack-slot movement phase");
            AssertEqual(InteractionReservationKind.AttackSlot, attacker.ReservedInteractionKind, "attacker should reserve deterministic attack slot");
            AssertEqual(targetId, attacker.ReservedInteractionTargetId, "attack slot reservation should be tied to target id");
        }

        private static void AttackGroupUsesDistinctAttackSlots()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int attackerA = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            int attackerB = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 1));
            int targetId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(8, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { attackerA, attackerB }, targetId)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit first = FindUnitById(state, attackerA);
            Unit second = FindUnitById(state, attackerB);
            AssertEqual(InteractionReservationKind.AttackSlot, first.ReservedInteractionKind, "first attacker should reserve attack slot");
            AssertEqual(InteractionReservationKind.AttackSlot, second.ReservedInteractionKind, "second attacker should reserve attack slot");
            bool sameSlot = first.ReservedInteractionTileX == second.ReservedInteractionTileX
                && first.ReservedInteractionTileY == second.ReservedInteractionTileY;
            AssertEqual(false, sameSlot, "group attackers should not reserve identical attack slot");
        }

        private static void AttackOverflowAttackersWaitWithoutStacking()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int targetId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(18, 18));
            var attackers = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                attackers.Add(EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(i % 3, i / 3)));
            }

            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(attackers, targetId)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            int reservedCount = 0;
            int waitingCount = 0;
            for (int i = 0; i < attackers.Count; i++)
            {
                Unit attacker = FindUnitById(state, attackers[i]);
                if (attacker.ReservedInteractionKind == InteractionReservationKind.AttackSlot)
                {
                    reservedCount++;
                }
                else if (attacker.TaskPhase == WorkerTaskPhase.BlockedWaiting && !attacker.HasMoveTarget)
                {
                    waitingCount++;
                }
            }

            AssertEqual(true, reservedCount <= 8, "single-tile unit target should expose bounded ring slots");
            AssertEqual(true, waitingCount > 0, "overflow attackers should wait instead of forcing stacks");
        }

        private static void CombatPressureTenAttackersVsOneTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int targetId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(24, 24));
            int[] attackers = CreateInfantryLine(state, 0, 10, 20, 22);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            var traces = new Queue<string>();

            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(attackers, targetId)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            bool targetRemoved = false;
            for (int tick = 0; tick < 420; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureCombatTraceTick(state, attackers, traces, 120);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("10v1 stacking invariant", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("10v1 reservation invariant", traces));
                AssertNoEndlessWorkerPhase(state, attackers, WorkerTaskPhase.MovingToAttackSlot, GameData.NoProgressTimeoutTicks * 2, BuildTraceFailureMessage("10v1 attack-slot no-progress invariant", traces));
                if (!state.EntityState.EntityLookup.ContainsKey(targetId))
                {
                    targetRemoved = true;
                    break;
                }

                AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            }

            AssertEqual(true, targetRemoved, BuildTraceFailureMessage("10v1 target should die under focused attack", traces));
            AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
            AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            runner.AdvanceOneTick(state, rules, buffer);
            for (int i = 0; i < attackers.Length; i++)
            {
                Unit attacker = FindUnitById(state, attackers[i]);
                AssertEqual(0, attacker.AttackTargetId, "attackers should clear target intent once target is gone");
            }
        }

        private static void CombatPressureFiftyVsFiftyMeleeStaysStable()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int[] sideA = CreateInfantryLine(state, 0, 50, 8, 20);
            int[] sideB = CreateInfantryLine(state, 1, 50, 80, 20);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            var traces = new Queue<string>();

            QueueNearestEnemyAttackCommands(state, buffer, 0, sideA, sideB, 0);
            QueueNearestEnemyAttackCommands(state, buffer, 1, sideB, sideA, 0);

            int startA = CountAliveUnitsForPlayer(state, 0);
            int startB = CountAliveUnitsForPlayer(state, 1);
            int maxPathCalls = 0;
            int maxReservationRetarget = 0;
            for (int tick = 0; tick < 700; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int[] liveSideA = FilterAliveEntityIds(state, sideA);
                int[] liveSideB = FilterAliveEntityIds(state, sideB);
                CaptureCombatTraceTick(state, liveSideA, traces, 160);
                CaptureCombatTraceTick(state, liveSideB, traces, 160);
                AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("50v50 stacking invariant", traces));
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("50v50 reservation invariant", traces));
                AssertNoEndlessWorkerPhase(state, liveSideA, WorkerTaskPhase.MovingToAttackSlot, GameData.NoProgressTimeoutTicks * 2, BuildTraceFailureMessage("50v50 sideA attack-slot no-progress invariant", traces));
                AssertNoEndlessWorkerPhase(state, liveSideB, WorkerTaskPhase.MovingToAttackSlot, GameData.NoProgressTimeoutTicks * 2, BuildTraceFailureMessage("50v50 sideB attack-slot no-progress invariant", traces));
                maxPathCalls = Math.Max(maxPathCalls, state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls);
                maxReservationRetarget = Math.Max(maxReservationRetarget, state.DebugCounters.ReservationRetargetCount);

                AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            }

            int endA = CountAliveUnitsForPlayer(state, 0);
            int endB = CountAliveUnitsForPlayer(state, 1);
            AssertEqual(true, endA < startA || endB < startB, BuildTraceFailureMessage("50v50 should produce combat casualties", traces));
            AssertEqual(true, maxPathCalls <= GameData.PathQueryBudgetPerTick * 2, "50v50 path query budget should stay bounded max=" + maxPathCalls);
            AssertEqual(true, maxReservationRetarget <= GameData.ReservationRetargetBudgetPerTick * 2, "50v50 reservation retarget should stay bounded max=" + maxReservationRetarget);
        }

        private static void CombatPressureOneHundredFiftyVsOneHundredFiftyMeleeStaysStable()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int[] sideA = CreateInfantryGrid(state, 0, 150, 6, 10, 12);
            int[] sideB = CreateInfantryGrid(state, 1, 150, 48, 10, 12);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            var traces = new Queue<string>();

            QueueNearestEnemyAttackCommands(state, buffer, 0, sideA, sideB, 0);
            QueueNearestEnemyAttackCommands(state, buffer, 1, sideB, sideA, 0);

            int maxPathCalls = 0;
            int maxReservationRetarget = 0;
            for (int tick = 0; tick < 240; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int[] liveSideA = FilterAliveEntityIds(state, sideA);
                int[] liveSideB = FilterAliveEntityIds(state, sideB);
                if (tick % 6 == 0)
                {
                    CaptureCombatTraceTick(state, liveSideA, traces, 220);
                    CaptureCombatTraceTick(state, liveSideB, traces, 220);
                    AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("150v150 stacking invariant", traces));
                    AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("150v150 reservation invariant", traces));
                }

                AssertNoEndlessWorkerPhase(state, liveSideA, WorkerTaskPhase.MovingToAttackSlot, GameData.NoProgressTimeoutTicks * 2, "150v150 sideA attack-slot no-progress invariant");
                AssertNoEndlessWorkerPhase(state, liveSideB, WorkerTaskPhase.MovingToAttackSlot, GameData.NoProgressTimeoutTicks * 2, "150v150 sideB attack-slot no-progress invariant");
                maxPathCalls = Math.Max(maxPathCalls, state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls);
                maxReservationRetarget = Math.Max(maxReservationRetarget, state.DebugCounters.ReservationRetargetCount);

                AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            }

            AssertEqual(true, maxPathCalls <= GameData.PathQueryBudgetPerTick * 3, "150v150 path query budget should stay bounded max=" + maxPathCalls);
            AssertEqual(true, maxReservationRetarget <= GameData.ReservationRetargetBudgetPerTick * 3, "150v150 reservation retarget should stay bounded max=" + maxReservationRetarget);
        }

        private static void CombatPressureHotspotThreeAttackersVsDefenderObjectiveStaysStable()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(4);
            var state = GameInitializer.CreateNomadStart(4, 1);
            int objectiveId = EntityFactory.CreateTownCenter(state, 0, FixedVector2.FromInts(32, 28));
            Building objective = FindBuildingById(state, objectiveId);
            objective.IsUnderConstruction = false;
            objective.HitPoints = GameData.TownCenterHitPoints + GameData.CapitalHitPointBonus;
            int[] defenders = CreateInfantryGrid(state, 0, 40, 28, 24, 10);

            int[] attacker1 = CreateInfantryGrid(state, 1, 40, 16, 14, 10);
            int[] attacker2 = CreateInfantryGrid(state, 2, 40, 16, 36, 10);
            int[] attacker3 = CreateInfantryGrid(state, 3, 40, 44, 24, 10);
            var allAttackers = Concat(attacker1, attacker2, attacker3);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            var traces = new Queue<string>();

            QueueNearestEnemyAttackCommands(state, buffer, 1, attacker1, defenders, 0);
            QueueNearestEnemyAttackCommands(state, buffer, 2, attacker2, defenders, 0);
            QueueNearestEnemyAttackCommands(state, buffer, 3, attacker3, defenders, 0);
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.NoOp), new NoOpCommand()));

            int maxPathCalls = 0;
            int maxReservationRetarget = 0;
            for (int tick = 0; tick < 500; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                int[] liveAttackers = FilterAliveEntityIds(state, allAttackers);
                if (tick % 6 == 0)
                {
                    CaptureCombatTraceTick(state, liveAttackers, traces, 260);
                    AssertNoLiveUnitStacking(state, BuildTraceFailureMessage("hotspot stacking invariant", traces));
                    AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("hotspot reservation invariant", traces));
                }

                AssertNoEndlessWorkerPhase(state, liveAttackers, WorkerTaskPhase.MovingToAttackSlot, GameData.NoProgressTimeoutTicks * 2, "hotspot attack-slot no-progress invariant");
                maxPathCalls = Math.Max(maxPathCalls, state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls);
                maxReservationRetarget = Math.Max(maxReservationRetarget, state.DebugCounters.ReservationRetargetCount);

                AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 2, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 3, (uint)state.Tick);
            }

            AssertEqual(true, maxPathCalls <= GameData.PathQueryBudgetPerTick * 3, "hotspot path query budget should stay bounded max=" + maxPathCalls);
            AssertEqual(true, maxReservationRetarget <= GameData.ReservationRetargetBudgetPerTick * 3, "hotspot reservation retarget should stay bounded max=" + maxReservationRetarget);
            AssertEqual(true, CountDamagedUnitsForPlayer(state, 0) > 0, "hotspot defenders should take pressure damage");
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

        private static void AttackMoveAcceptsCombatUnit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { 11 }, FixedVector2.FromInts(4, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit attacker = state.EntityState.Units[10];
            AssertEqual(0, state.DebugCounters.RejectedCommandCount, "combat unit attack-move should be accepted");
            AssertEqual(true, attacker.HasAttackMoveTarget, "attack-move should set persistent attack-move intent");
            AssertEqual(4, SpatialRules.GetTileX(attacker.AttackMoveTarget), "attack-move destination x should be stored");
            AssertEqual(0, SpatialRules.GetTileY(attacker.AttackMoveTarget), "attack-move destination y should be stored");
            AssertEqual(true, attacker.HasMoveTarget, "attack-move should issue movement toward destination in 9C.1");
        }

        private static void AttackMoveRejectsNonCombatUnit()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var state = GameInitializer.CreateNomadStart(2, 1);
            int tradeCartId = EntityFactory.CreateUnit(state, 0, UnitTypeId.TradeCart, FixedVector2.FromInts(0, 0));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { tradeCartId }, FixedVector2.FromInts(6, 6))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit cart = FindUnitById(state, tradeCartId);
            AssertEqual(1, state.DebugCounters.RejectedCommandCount, "non-combat attack-move should reject");
            AssertEqual(false, cart.HasAttackMoveTarget, "rejected attack-move should not set intent state");
        }

        private static void AttackMoveClearsExplicitAttackIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            state.EntityState.Units[10].AttackTargetId = 12;
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { 11 }, FixedVector2.FromInts(5, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit attacker = state.EntityState.Units[10];
            AssertEqual(0, attacker.AttackTargetId, "attack-move should clear explicit attack target intent");
            AssertEqual(true, attacker.HasAttackMoveTarget, "attack-move intent should be active after command");
        }

        private static void MoveCommandClearsAttackMoveIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            Unit attacker = state.EntityState.Units[10];
            attacker.HasAttackMoveTarget = true;
            attacker.AttackMoveTarget = FixedVector2.FromInts(9, 9);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.MoveUnits), new MoveUnitsCommand(new[] { 11 }, FixedVector2.FromInts(2, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Units[10].HasAttackMoveTarget, "explicit move should clear attack-move intent");
        }

        private static void ExplicitAttackClearsAttackMoveIntent()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            Unit attacker = state.EntityState.Units[10];
            attacker.HasAttackMoveTarget = true;
            attacker.AttackMoveTarget = FixedVector2.FromInts(8, 8);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.Attack), new AttackCommand(new[] { 11 }, 12)));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.Units[10].HasAttackMoveTarget, "explicit attack should clear attack-move intent");
            AssertEqual(12, state.EntityState.Units[10].AttackTargetId, "explicit attack target should still be assigned");
        }

        private static void AttackMoveChecksumCoversIntentState()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateAdjacentCombatState();
            ulong before = StateChecksum.Compute(state, rules);
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { 11 }, FixedVector2.FromInts(6, 0))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);
            ulong after = StateChecksum.Compute(state, rules);

            AssertEqual(true, before != after, "attack-move intent state should be checksum-covered");
        }

        private static void AttackMoveReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { 11 }, FixedVector2.FromInts(7, 1))),
                new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand())
            };

            ulong first = RunCommandsFromState(CreateAdjacentCombatState(), rules, commands, 1);
            ulong second = RunCommandsFromState(CreateAdjacentCombatState(), rules, commands, 1);
            AssertEqual(first, second, "attack-move command stream should replay deterministically");
        }

        private static void AttackMoveLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 713, true);
            EntityFactory.CreateUnit(session.Peers[0].LocalState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(session.Peers[0].LocalState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(session.Peers[1].LocalState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(session.Peers[1].LocalState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { 11 }, FixedVector2.FromInts(7, 1))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            AssertEqual(true, session.TryAdvanceOneTick(), "attack-move lockstep tick should advance");
            AssertEqual(0, session.DesyncReports.Count, "attack-move lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "attack-move lockstep checksums should match");
        }

        private static void AttackMoveAcquiresNearbyEnemy()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(901, 2);
            int attackerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            int enemyId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(31, 30));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { attackerId }, FixedVector2.FromInts(40, 30))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit attacker = FindUnitById(state, attackerId);
            AssertEqual(enemyId, attacker.AttackTargetId, "attack-move should acquire nearby enemy target on bounded indexed query");
            AssertEqual(true, attacker.HasAttackMoveTarget, "attack-move destination intent should remain active during temporary combat target");
        }

        private static void AttackMoveWithoutNearbyEnemyKeepsMoving()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(902, 2);
            int attackerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            int enemyId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(45, 45));
            var buffer = new CommandBuffer();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { attackerId }, FixedVector2.FromInts(40, 30))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            Unit attacker = FindUnitById(state, attackerId);
            AssertEqual(0, attacker.AttackTargetId, "attack-move should not acquire distant enemy outside search radius");
            AssertEqual(true, attacker.HasMoveTarget, "attack-move should continue moving when no target is nearby");
            AssertEqual(true, state.EntityState.EntityLookup.ContainsKey(enemyId), "distant enemy should still exist");
        }

        private static void AttackMoveAcquisitionRespectsCadence()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(903, 2);
            int attackerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            int enemyId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(45, 45));
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { attackerId }, FixedVector2.FromInts(40, 30))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            Unit attacker = FindUnitById(state, attackerId);
            AssertEqual(0, attacker.AttackTargetId, "initial far enemy should not be acquired");

            Unit enemy = FindUnitById(state, enemyId);
            enemy.Position = FixedVector2.FromInts(31, 30);

            for (int tick = state.Tick; tick < GameData.AttackMoveAcquireCadenceTicks; tick++)
            {
                AddNoOp(buffer, tick, 0, (uint)tick);
                AddNoOp(buffer, tick, 1, (uint)tick);
                runner.AdvanceOneTick(state, rules, buffer);
                attacker = FindUnitById(state, attackerId);
                AssertEqual(0, attacker.AttackTargetId, "acquisition should wait for cadence window");
            }

            AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
            AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            runner.AdvanceOneTick(state, rules, buffer);
            attacker = FindUnitById(state, attackerId);
            AssertEqual(enemyId, attacker.AttackTargetId, "attack-move should acquire once cadence window opens");
        }

        private static void AttackMoveResumesMovementAfterTargetDies()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(905, 2);
            int attackerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            int enemyId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(31, 30));
            FindUnitById(state, enemyId).HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { attackerId }, FixedVector2.FromInts(40, 30))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);

            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(enemyId), "first acquired enemy should die in setup tick");

            AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
            AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            runner.AdvanceOneTick(state, rules, buffer);

            Unit attacker = FindUnitById(state, attackerId);
            AssertEqual(0, attacker.AttackTargetId, "attack-move attacker should clear dead acquired target");
            AssertEqual(true, attacker.HasAttackMoveTarget, "attack-move intent should remain active after temporary target dies");
            AssertEqual(true, attacker.HasMoveTarget, "attack-move attacker should resume travel after temporary target clears");
            AssertEqual(WorkerTaskPhase.MovingToCommandMove, attacker.TaskPhase, "attack-move attacker should return to command-move phase after target clears");
        }

        private static void AttackMoveCompletesAtDestinationWithoutTarget()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(906, 2);
            int attackerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            Unit attacker = FindUnitById(state, attackerId);
            attacker.HasAttackMoveTarget = true;
            attacker.AttackMoveTarget = FixedVector2.FromInts(30, 30);
            attacker.AttackTargetId = 0;
            attacker.HasMoveTarget = false;
            attacker.TaskPhase = WorkerTaskPhase.MovingToCommandMove;
            var buffer = new CommandBuffer();
            AddNoOp(buffer, 0, 0, 0);
            AddNoOp(buffer, 0, 1, 0);

            new TickRunner().AdvanceOneTick(state, rules, buffer);

            attacker = FindUnitById(state, attackerId);
            AssertEqual(false, attacker.HasAttackMoveTarget, "attack-move intent should clear once destination is already reached");
            AssertEqual(false, attacker.HasMoveTarget, "completed attack-move should not keep a movement target");
            AssertEqual(WorkerTaskPhase.Idle, attacker.TaskPhase, "completed attack-move should settle to idle");
        }

        private static void AttackMoveResumeStillRespectsAcquireCadence()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = CreateOccupancyState(907, 2);
            int attackerId = EntityFactory.CreateUnit(state, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            int enemyAId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(31, 30));
            int enemyBId = EntityFactory.CreateUnit(state, 1, UnitTypeId.Infantry, FixedVector2.FromInts(31, 31));
            FindUnitById(state, enemyAId).HitPoints = GameData.InfantryAttackDamage;
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { attackerId }, FixedVector2.FromInts(42, 30))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            runner.AdvanceOneTick(state, rules, buffer);
            AssertEqual(false, state.EntityState.EntityLookup.ContainsKey(enemyAId), "first enemy should die to establish resume cadence test");

            AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
            AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            runner.AdvanceOneTick(state, rules, buffer);
            Unit attacker = FindUnitById(state, attackerId);
            AssertEqual(0, attacker.AttackTargetId, "dead target should clear before any reacquire");

            while (state.Tick < GameData.AttackMoveAcquireCadenceTicks)
            {
                AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
                runner.AdvanceOneTick(state, rules, buffer);
                attacker = FindUnitById(state, attackerId);
                AssertEqual(0, attacker.AttackTargetId, "reacquire should wait for acquisition cadence after resume");
            }

            AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
            AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            runner.AdvanceOneTick(state, rules, buffer);
            attacker = FindUnitById(state, attackerId);
            AssertEqual(enemyBId, attacker.AttackTargetId, "attack-move should reacquire nearby enemy once cadence opens after resume");
        }

        private static void AttackMoveAcquisitionReplayDeterminism()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState firstState = CreateOccupancyState(904, 2);
            GameState secondState = CreateOccupancyState(904, 2);
            int firstAttackerId = EntityFactory.CreateUnit(firstState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            int firstEnemyId = EntityFactory.CreateUnit(firstState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(31, 30));
            int secondAttackerId = EntityFactory.CreateUnit(secondState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(30, 30));
            int secondEnemyId = EntityFactory.CreateUnit(secondState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(31, 30));
            AssertEqual(firstEnemyId, secondEnemyId, "replay acquisition setup should produce stable enemy ids");
            var commands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { firstAttackerId }, FixedVector2.FromInts(40, 30))),
                new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand())
            };
            var mirroredCommands = new[]
            {
                new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { secondAttackerId }, FixedVector2.FromInts(40, 30))),
                new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand())
            };

            ulong first = RunCommandsFromState(firstState, rules, commands, 1);
            ulong second = RunCommandsFromState(secondState, rules, mirroredCommands, 1);
            AssertEqual(first, second, "attack-move acquisition should replay deterministically");
        }

        private static void AttackMoveAcquisitionLockstep()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            var session = new LockstepSession(rules, 714, true);
            EntityFactory.CreateUnit(session.Peers[0].LocalState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(session.Peers[0].LocalState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));
            EntityFactory.CreateUnit(session.Peers[1].LocalState, 0, UnitTypeId.Infantry, FixedVector2.FromInts(0, 0));
            EntityFactory.CreateUnit(session.Peers[1].LocalState, 1, UnitTypeId.Infantry, FixedVector2.FromInts(1, 0));

            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(new[] { 11 }, FixedVector2.FromInts(8, 0))));
            session.Broadcast(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));
            AssertEqual(true, session.TryAdvanceOneTick(), "attack-move acquisition lockstep tick should advance");

            AssertEqual(0, session.DesyncReports.Count, "attack-move acquisition lockstep should not desync");
            AssertEqual(session.Peers[0].LocalState.LastChecksum, session.Peers[1].LocalState.LastChecksum, "attack-move acquisition lockstep checksums should match");
        }

        private static void AttackMovePressureTenUnitsThroughEnemyGroupStaysStable()
        {
            var rules = GameRules.CreatePhaseZeroDefaults(2);
            GameState state = GameInitializer.CreateNomadStart(2, 1);
            int[] attackers = CreateInfantryLine(state, 0, 10, 0, 10);
            int[] enemies = CreateInfantryLine(state, 1, 6, 6, 10);
            var buffer = new CommandBuffer();
            var runner = new TickRunner();
            var traces = new Queue<string>();
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 0, 0, CommandType.AttackMove), new AttackMoveCommand(attackers, FixedVector2.FromInts(20, 10))));
            buffer.Add(new CommandEnvelope(new CommandHeader(0, 1, 0, CommandType.NoOp), new NoOpCommand()));

            bool acquired = false;
            for (int tick = 0; tick < 160; tick++)
            {
                runner.AdvanceOneTick(state, rules, buffer);
                CaptureCombatTraceTick(state, attackers, traces, 160);
                AssertNoDuplicateFinalPurposeReservations(state, BuildTraceFailureMessage("attack-move pressure reservation invariant", traces));
                for (int i = 0; i < attackers.Length; i++)
                {
                    if (!state.EntityState.EntityLookup.TryGetValue(attackers[i], out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
                    {
                        continue;
                    }

                    Unit attacker = state.EntityState.Units[entityRef.Index];
                    if (!attacker.IsDead && attacker.AttackTargetId != 0)
                    {
                        acquired = true;
                        break;
                    }
                }

                AddNoOp(buffer, state.Tick, 0, (uint)state.Tick);
                AddNoOp(buffer, state.Tick, 1, (uint)state.Tick);
            }

            AssertEqual(true, acquired, BuildTraceFailureMessage("attack-move pressure should acquire enemies under contact", traces));
            AssertEqual(true, CountAliveUnitsForPlayer(state, 1) < enemies.Length || CountDamagedUnitsForPlayer(state, 1) > 0, BuildTraceFailureMessage("attack-move pressure should apply combat pressure", traces));
        }

        private static int[] CreateInfantryLine(GameState state, int ownerPlayerIndex, int count, int startX, int y)
        {
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = EntityFactory.CreateUnit(state, ownerPlayerIndex, UnitTypeId.Infantry, FixedVector2.FromInts(startX + i, y));
            }

            return ids;
        }

        private static int[] CreateInfantryGrid(GameState state, int ownerPlayerIndex, int count, int startX, int startY, int width)
        {
            var ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                int x = startX + (i % width);
                int y = startY + (i / width);
                ids[i] = EntityFactory.CreateUnit(state, ownerPlayerIndex, UnitTypeId.Infantry, FixedVector2.FromInts(x, y));
            }

            return ids;
        }

        private static int[] Concat(int[] first, int[] second, int[] third)
        {
            var merged = new int[first.Length + second.Length + third.Length];
            Array.Copy(first, 0, merged, 0, first.Length);
            Array.Copy(second, 0, merged, first.Length, second.Length);
            Array.Copy(third, 0, merged, first.Length + second.Length, third.Length);
            return merged;
        }

        private static int[] FilterAliveEntityIds(GameState state, int[] ids)
        {
            var alive = new List<int>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                if (state.EntityState.EntityLookup.ContainsKey(ids[i]))
                {
                    alive.Add(ids[i]);
                }
            }

            return alive.ToArray();
        }

        private static void QueueNearestEnemyAttackCommands(GameState state, CommandBuffer buffer, int playerIndex, int[] attackers, int[] enemyPool, int tick)
        {
            for (int i = 0; i < attackers.Length; i++)
            {
                Unit attacker = FindUnitById(state, attackers[i]);
                int targetId = FindNearestAliveEnemy(state, attacker.Position, enemyPool);
                if (targetId == 0)
                {
                    continue;
                }

                buffer.Add(new CommandEnvelope(
                    new CommandHeader(tick, playerIndex, (uint)(i + 1), CommandType.Attack),
                    new AttackCommand(new[] { attackers[i] }, targetId)));
            }
        }

        private static int FindNearestAliveEnemy(GameState state, FixedVector2 from, int[] enemyPool)
        {
            int bestId = 0;
            long bestDistanceSquared = long.MaxValue;
            for (int i = 0; i < enemyPool.Length; i++)
            {
                int enemyId = enemyPool[i];
                if (!state.EntityState.EntityLookup.TryGetValue(enemyId, out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
                {
                    continue;
                }

                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
                {
                    continue;
                }

                Unit enemy = state.EntityState.Units[entityRef.Index];
                if (enemy.Id != enemyId || enemy.IsDead)
                {
                    continue;
                }

                long distanceSquared = (enemy.Position - from).LengthSquaredRaw();
                if (distanceSquared < bestDistanceSquared || (distanceSquared == bestDistanceSquared && enemyId < bestId))
                {
                    bestDistanceSquared = distanceSquared;
                    bestId = enemyId;
                }
            }

            return bestId;
        }

        private static int CountAliveUnitsForPlayer(GameState state, int playerIndex)
        {
            int count = 0;
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!unit.IsDead && unit.OwnerPlayerIndex == playerIndex)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDamagedUnitsForPlayer(GameState state, int playerIndex)
        {
            int count = 0;
            for (int i = 0; i < state.EntityState.Units.Count; i++)
            {
                Unit unit = state.EntityState.Units[i];
                if (!unit.IsDead && unit.OwnerPlayerIndex == playerIndex && unit.HitPoints < GameData.InfantryHitPoints)
                {
                    count++;
                }
            }

            return count;
        }

        private static void CaptureCombatTraceTick(GameState state, int[] unitIds, Queue<string> traces, int maxEntries)
        {
            for (int i = 0; i < unitIds.Length; i++)
            {
                if (!state.EntityState.EntityLookup.TryGetValue(unitIds[i], out EntityRef entityRef) || entityRef.Kind != EntityKind.Unit)
                {
                    continue;
                }

                if (entityRef.Index < 0 || entityRef.Index >= state.EntityState.Units.Count)
                {
                    continue;
                }

                Unit unit = state.EntityState.Units[entityRef.Index];
                if (unit.Id != unitIds[i] || unit.IsDead)
                {
                    continue;
                }

                int tileX = SpatialRules.GetTileX(unit.Position);
                int tileY = SpatialRules.GetTileY(unit.Position);
                int noProgressTicks = unit.LastMovedTick < 0 ? 0 : state.Tick - unit.LastMovedTick;
                string line =
                    "t=" + state.Tick
                    + " u=" + unit.Id
                    + " p=" + unit.OwnerPlayerIndex
                    + " tile=(" + tileX + "," + tileY + ")"
                    + " hp=" + unit.HitPoints
                    + " atkTarget=" + unit.AttackTargetId
                    + " phase=" + unit.TaskPhase
                    + " reserve=" + unit.ReservedInteractionKind + ":" + unit.ReservedInteractionTargetId + "@(" + unit.ReservedInteractionTileX + "," + unit.ReservedInteractionTileY + ")"
                    + " cooldown=" + unit.AttackCooldownTicksRemaining
                    + " noProgress=" + noProgressTicks
                    + " pathCalls=" + (state.DebugCounters.PathFindNextCalls + state.DebugCounters.PathFindCostCalls)
                    + " retargets=" + state.DebugCounters.ReservationRetargetCount;
                traces.Enqueue(line);
                while (traces.Count > maxEntries)
                {
                    traces.Dequeue();
                }
            }
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

    }
}
