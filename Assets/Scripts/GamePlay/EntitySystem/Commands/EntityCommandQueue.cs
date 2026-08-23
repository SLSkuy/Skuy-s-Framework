using System;
using System.Collections.Generic;

namespace GamePlay.EntitySystem
{
    public sealed class EntityCommandQueue<TCommand>
    {
        private readonly SortedDictionary<uint, TCommand> _commands = new();
        private readonly int _capacity;

        #region Properties
        public int Count => _commands.Count;
        public uint LastProcessedTick { get; private set; }
        #endregion

        public EntityCommandQueue(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public bool Enqueue(uint tick, uint currentTick, uint maxPastTicks, uint maxFutureTicks, in TCommand command)
        {
            long tickDistance = (long)tick - currentTick;
            if (tickDistance < -maxPastTicks || tickDistance > maxFutureTicks || tick <= LastProcessedTick) return false;
            if (_commands.ContainsKey(tick))
            {
                _commands[tick] = command;
                return true;
            }

            if (_commands.Count >= _capacity) return false;
            _commands.Add(tick, command);
            return true;
        }

        public bool TryDequeueExecutable(uint currentTick, out uint tick, out TCommand command)
        {
            if (_commands.Count == 0)
            {
                tick = 0;
                command = default;
                return false;
            }

            using IEnumerator<KeyValuePair<uint, TCommand>> enumerator = _commands.GetEnumerator();
            enumerator.MoveNext();
            KeyValuePair<uint, TCommand> item = enumerator.Current;
            if (item.Key > currentTick)
            {
                tick = 0;
                command = default;
                return false;
            }

            _commands.Remove(item.Key);
            LastProcessedTick = item.Key;
            tick = item.Key;
            command = item.Value;
            return true;
        }

        public void Clear()
        {
            _commands.Clear();
            LastProcessedTick = 0;
        }
    }
}
