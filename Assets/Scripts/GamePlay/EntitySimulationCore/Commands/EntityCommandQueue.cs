using System;
using System.Collections.Generic;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 服务端按输入 Tick 排序、去重并限制容量的纯 C# 命令队列。
    /// </summary>
    public sealed class EntityCommandQueue<TCommand>
    {
        private readonly SortedDictionary<uint, TCommand> _commands = new();
        private readonly int _capacity;

        #region 属性
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
            if (tickDistance < -maxPastTicks || tickDistance > maxFutureTicks) return false;
            if (tick <= LastProcessedTick || _commands.ContainsKey(tick)) return false;
            if (_commands.Count >= _capacity) return false;
            _commands.Add(tick, command);
            return true;
        }

        /// <summary>
        /// 只消费当前服务端 Tick 已经到达的最早命令。
        /// </summary>
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

        [Obsolete("Use TryDequeueExecutable with the server tick.")]
        public bool TryDequeue(out uint tick, out TCommand command)
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
