using System.Collections.Generic;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Stress
{
    public interface IStressScenario
    {
        string ScenarioName { get; }
        int ScenarioVersion { get; }
        int PlayerCount { get; }
        int Ticks { get; }
        void PrepareInitialState(GameState state);
        IReadOnlyList<CommandEnvelope> GetCommandsForTick(int tick);
    }
}
