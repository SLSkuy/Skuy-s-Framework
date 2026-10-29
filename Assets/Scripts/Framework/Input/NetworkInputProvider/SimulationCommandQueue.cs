using System;

namespace Framework
{
    /// <summary>
    /// 按客户端输入 Tick 索引的环形命令缓冲；Tick 0 表示空槽，未命中时返回默认空命令。
    /// </summary>
    public class SimulationCommandQueue<TCommand>
    {
        private readonly TCommand[] _commands;
        private readonly uint[] _storedTicks;
        private readonly int _capacity;

        #region 属性
        public int Capacity => _capacity;
        public int Count { get; private set; }
        public bool IsEmpty => Count == 0;
        public bool IsFull => Count == _capacity;
        #endregion

        public SimulationCommandQueue(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _commands = new TCommand[capacity];
            _storedTicks = new uint[capacity];
        }

        /// <summary>
        /// 写入指定 Tick 的命令
        /// 如果该 Tick 已经存在，则覆盖原命令
        /// 如果对应槽位被其他 Tick 占用，则写入失败
        /// </summary>
        /// <param name="tick">命令对应的客户端 Tick</param>
        /// <param name="command">命令</param>
        public bool TryEnqueue(uint tick, in TCommand command)
        {
            if (tick == 0)
            {
                return false;
            }

            int index = GetIndex(tick);
            uint storedTick = _storedTicks[index];

            // 同一个 Tick，允许覆盖。
            if (storedTick == tick)
            {
                _commands[index] = command;
                return true;
            }

            // 槽位中存在其他 Tick，不能覆盖。
            if (storedTick != 0)
            {
                return false;
            }

            _commands[index] = command;
            _storedTicks[index] = tick;
            Count++;

            return true;
        }

        /// <summary>
        /// 查询并移除指定 Tick 的命令
        /// </summary>
        public bool TryDequeue(uint tick, out TCommand command)
        {
            if (tick == 0)
            {
                command = default;
                return false;
            }

            int index = GetIndex(tick);

            if (_storedTicks[index] != tick)
            {
                command = default;
                return false;
            }

            command = _commands[index];

            ClearSlot(index);

            return true;
        }

        /// <summary>
        /// 移除指定 Tick 的命令。
        /// </summary>
        public bool Remove(uint tick)
        {
            if (tick == 0)
            {
                return false;
            }

            int index = GetIndex(tick);

            if (_storedTicks[index] != tick)
            {
                return false;
            }

            ClearSlot(index);

            return true;
        }

        /// <summary>
        /// 判断指定 Tick 是否存在。
        /// </summary>
        public bool Contains(uint tick)
        {
            if (tick == 0)
            {
                return false;
            }

            return _storedTicks[GetIndex(tick)] == tick;
        }

        /// <summary>
        /// 清空整个缓冲区。
        /// </summary>
        public void Clear()
        {
            Array.Clear(_commands, 0, _commands.Length);
            Array.Clear(_storedTicks, 0, _storedTicks.Length);

            Count = 0;
        }

        /// <summary>
        /// 清空已占用的槽位并更新数量。
        /// </summary>
        private void ClearSlot(int index)
        {
            _commands[index] = default;
            _storedTicks[index] = 0;
            Count--;
        }

        /// <summary>
        /// 获取 Tick 对应的环形槽位。
        /// </summary>
        private int GetIndex(uint tick)
        {
            return (int)(tick % (uint)_capacity);
        }
    }
}