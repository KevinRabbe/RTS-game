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

    }
}
