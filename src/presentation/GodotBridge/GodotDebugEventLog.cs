using System;
using System.Collections.Generic;

namespace RtsGame.Presentation.GodotBridge
{
    public sealed class GodotDebugEventLog
    {
        private readonly int _capacity;
        private readonly Queue<string> _events;

        public GodotDebugEventLog(int capacity)
        {
            _capacity = Math.Max(1, capacity);
            _events = new Queue<string>(_capacity);
        }

        public void Add(string message)
        {
            if (_events.Count >= _capacity)
            {
                _events.Dequeue();
            }

            _events.Enqueue(message);
        }

        public string[] GetLines()
        {
            return _events.ToArray();
        }
    }
}
