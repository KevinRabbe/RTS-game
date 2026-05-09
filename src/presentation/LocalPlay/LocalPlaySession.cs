using System;
using RtsGame.Presentation.ClientInput;
using RtsGame.Presentation.Snapshots;
using RtsGame.Presentation.Visuals;
using RtsGame.Sim.Commands;
using RtsGame.Sim.Core;

namespace RtsGame.Presentation.LocalPlay
{
    public sealed class LocalPlaySession
    {
        private readonly TickRunner _runner;
        private readonly CommandBuffer _commandBuffer;
        private readonly GameState _state;
        private readonly uint[] _nextSequenceByPlayer;
        private readonly bool[] _hasInputForCurrentTick;

        public GameRules Rules { get; }

        public int CurrentTick
        {
            get { return _state.Tick; }
        }

        public int PlayerCount
        {
            get { return Rules.MaxPlayers; }
        }

        public int ExecutedCommandCount
        {
            get { return _state.DebugCounters.ExecutedCommandCount; }
        }

        public int RejectedCommandCount
        {
            get { return _state.DebugCounters.RejectedCommandCount; }
        }

        public ulong LastChecksum
        {
            get { return _state.LastChecksum; }
        }

        public bool IsMatchFinished
        {
            get { return _state.MatchResultState.IsFinished; }
        }

        public int WinnerPlayerIndex
        {
            get { return _state.MatchResultState.WinnerPlayerIndex; }
        }

        public LocalPlaySession(GameRules rules, ulong matchSeed)
        {
            if (rules.MaxPlayers <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rules), "Local play requires at least one player.");
            }

            Rules = rules;
            _runner = new TickRunner();
            _commandBuffer = new CommandBuffer();
            _state = GameInitializer.CreateNomadStart(matchSeed, rules.MaxPlayers);
            _nextSequenceByPlayer = new uint[rules.MaxPlayers];
            _hasInputForCurrentTick = new bool[rules.MaxPlayers];
        }

        public static LocalPlaySession Create1v1(ulong matchSeed)
        {
            return Create(matchSeed, 2);
        }

        public static LocalPlaySession Create6PlayerFfa(ulong matchSeed)
        {
            return Create(matchSeed, 6);
        }

        public static LocalPlaySession Create(ulong matchSeed, int playerCount)
        {
            if (playerCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Local play requires at least one player.");
            }

            return new LocalPlaySession(GameRules.CreatePhaseZeroDefaults(playerCount), matchSeed);
        }

        public void QueueIntent(int playerIndex, ClientCommandIntent intent)
        {
            ValidatePlayerIndex(playerIndex);
            CommandEnvelope command = ClientCommandMapper.ToCommandEnvelope(
                intent,
                _state.Tick,
                playerIndex,
                _nextSequenceByPlayer[playerIndex]);

            _nextSequenceByPlayer[playerIndex]++;
            _hasInputForCurrentTick[playerIndex] = true;
            _commandBuffer.Add(command);
        }

        public void AdvanceOneTick()
        {
            AddNoOpsForMissingPlayers();
            _runner.AdvanceOneTick(_state, Rules, _commandBuffer);
            ClearCurrentTickInputFlags();
        }

        public void AdvanceTicks(int tickCount)
        {
            if (tickCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tickCount), "Tick count must be non-negative.");
            }

            for (int i = 0; i < tickCount; i++)
            {
                AdvanceOneTick();
            }
        }

        public GameSnapshot GetSnapshot(int localPlayerIndex)
        {
            return GameSnapshotBuilder.Build(_state, localPlayerIndex);
        }

        public VisualFrame GetVisualFrame(int localPlayerIndex)
        {
            return VisualFrameBuilder.Build(GetSnapshot(localPlayerIndex));
        }

        private void AddNoOpsForMissingPlayers()
        {
            for (int player = 0; player < Rules.MaxPlayers; player++)
            {
                if (_hasInputForCurrentTick[player])
                {
                    continue;
                }

                CommandEnvelope command = new CommandEnvelope(
                    new CommandHeader(_state.Tick, player, _nextSequenceByPlayer[player], CommandType.NoOp),
                    new NoOpCommand());
                _nextSequenceByPlayer[player]++;
                _commandBuffer.Add(command);
            }
        }

        private void ClearCurrentTickInputFlags()
        {
            for (int i = 0; i < _hasInputForCurrentTick.Length; i++)
            {
                _hasInputForCurrentTick[i] = false;
            }
        }

        private void ValidatePlayerIndex(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Rules.MaxPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex), "Player index must exist in the local session.");
            }
        }
    }
}
