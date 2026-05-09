namespace RtsGame.Sim.Data
{
    public sealed class MatchResultState
    {
        public bool IsFinished { get; set; }
        public int WinnerPlayerIndex { get; set; }
        public int FinishedTick { get; set; }

        public MatchResultState()
        {
            IsFinished = false;
            WinnerPlayerIndex = -1;
            FinishedTick = -1;
        }
    }
}
