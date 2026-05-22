using RtsGame.Net.Lockstep;
using RtsGame.Sim.Checksums;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;
using RtsGame.Sim.Data;
using RtsGame.Sim.Determinism;
using RtsGame.Sim.Replay;

namespace RtsGame.Tests
{
    public static partial class Program
    {
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

    }
}
