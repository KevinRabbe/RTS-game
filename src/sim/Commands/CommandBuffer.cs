using System.Collections.Generic;
using RtsGame.Sim.Determinism;

namespace RtsGame.Sim.Commands
{
    public sealed class CommandBuffer
    {
        private readonly Dictionary<int, List<CommandEnvelope>> _commandsByTick = new Dictionary<int, List<CommandEnvelope>>();

        public void Add(CommandEnvelope command)
        {
            int tick = command.Header.Tick;
            if (!_commandsByTick.TryGetValue(tick, out List<CommandEnvelope>? commands))
            {
                commands = new List<CommandEnvelope>();
                _commandsByTick.Add(tick, commands);
            }

            commands.Add(command);
        }

        public List<CommandEnvelope> GetCommandsForTick(int tick)
        {
            if (!_commandsByTick.TryGetValue(tick, out List<CommandEnvelope>? commands))
            {
                return new List<CommandEnvelope>();
            }

            return StableSort.Sorted(commands, CompareCommands);
        }

        public bool HasAllRequiredPlayerInputs(int tick, int playerCount)
        {
            if (!_commandsByTick.TryGetValue(tick, out List<CommandEnvelope>? commands))
            {
                return false;
            }

            var seen = new bool[playerCount];
            foreach (CommandEnvelope command in commands)
            {
                int playerIndex = command.Header.PlayerIndex;
                if (playerIndex >= 0 && playerIndex < playerCount)
                {
                    seen[playerIndex] = true;
                }
            }

            for (int i = 0; i < seen.Length; i++)
            {
                if (!seen[i])
                {
                    return false;
                }
            }

            return true;
        }

        public void ClearBeforeTick(int tick)
        {
            var ticksToRemove = new List<int>();
            foreach (int commandTick in _commandsByTick.Keys)
            {
                if (commandTick < tick)
                {
                    ticksToRemove.Add(commandTick);
                }
            }

            ticksToRemove.Sort();
            foreach (int commandTick in ticksToRemove)
            {
                _commandsByTick.Remove(commandTick);
            }
        }

        private static int CompareCommands(CommandEnvelope left, CommandEnvelope right)
        {
            int result = left.Header.Tick.CompareTo(right.Header.Tick);
            if (result != 0) return result;

            result = left.Header.PlayerIndex.CompareTo(right.Header.PlayerIndex);
            if (result != 0) return result;

            result = left.Header.Sequence.CompareTo(right.Header.Sequence);
            if (result != 0) return result;

            return left.Header.CommandType.CompareTo(right.Header.CommandType);
        }
    }
}
