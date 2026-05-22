namespace RtsGame.Tests
{
    public readonly struct TestCase
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
