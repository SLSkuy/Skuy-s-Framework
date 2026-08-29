using System;
using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 角色输入缓冲：服务端按客户端 Tick 做窗口校验与顺序消费，并构建每 Tick 命令
    /// LastProcessedTick 为已消费的客户端输入序号，0 表示尚未消费；下一拍固定取 LastProcessedTick + 1
    /// 环形槽位以 0 表示空，有效输入 Tick 从 1 起
    /// </summary>
    public sealed class EntityInputBuffer
    {
        private readonly EntityCommandQueue<InputState> _commands;
        private readonly EntityCommandBuilder _commandBuilder;
        private readonly int _maxFutureInputTicks;

        #region 属性
        public uint LastProcessedTick { get; private set; }
        public bool HasQueued => _commands.Count > 0;
        #endregion

        public EntityInputBuffer(int capacity, int maxFutureInputTicks)
        {
            if (maxFutureInputTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFutureInputTicks));
            }

            int resolvedCapacity = Math.Max(2, capacity);
            _commandBuilder = new EntityCommandBuilder();
            _commands = new EntityCommandQueue<InputState>(resolvedCapacity);
            _maxFutureInputTicks = Math.Min(Math.Max(1, maxFutureInputTicks), resolvedCapacity);
        }

        /// <summary>
        /// 将经过合法性校验的输入按客户端 Tick 加入服务端命令缓冲。
        /// 尚未消费过时，以首包序号对齐消费点，避免握手期间丢掉的快通道包把后续命令全部挤出窗口。
        /// </summary>
        public bool Enqueue(uint inputTick, in InputState input)
        {
            if (inputTick == 0) return false;

            if (LastProcessedTick == 0 && !HasQueued && inputTick > 1)
            {
                LastProcessedTick = inputTick - 1;
            }

            if (!IsInReceiveWindow(inputTick))
            {
                return false;
            }

            return _commands.TryEnqueue(inputTick, input);
        }

        /// <summary>
        /// 构建服务端当前 Tick 应执行的命令；按客户端输入序号消费，缺失时使用空输入。
        /// </summary>
        public EntityCommand BuildAuthorityCommand(uint tick)
        {
            InputState input = ConsumeNext();
            return _commandBuilder.Build(tick, input);
        }

        /// <summary>
        /// 根据已采样输入构建预测命令。
        /// </summary>
        public EntityCommand BuildPredictedCommand(uint inputTick, in InputState input)
        {
            return _commandBuilder.Build(inputTick, input);
        }

        /// <summary>
        /// 清空缓冲并重置消费序号与边沿检测。
        /// </summary>
        public void Reset()
        {
            _commands.Clear();
            _commandBuilder.Reset();
            LastProcessedTick = 0;
        }

        /// <summary>
        /// 相对已消费序号做过去/未来窗口校验。环形缓冲不能存放 Tick 0。
        /// </summary>
        private bool IsInReceiveWindow(uint inputTick)
        {
            if (inputTick == 0)
            {
                return false;
            }

            if (inputTick <= LastProcessedTick)
            {
                return false;
            }

            uint offset = inputTick - LastProcessedTick;
            if (offset > (uint)_maxFutureInputTicks)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 消费 LastProcessedTick 之后的下一序号；槽位缺失时返回默认空输入并仍推进确认点。
        /// </summary>
        public InputState ConsumeNext()
        {
            uint inputTick = LastProcessedTick + 1;
            if (!_commands.TryDequeue(inputTick, out InputState input))
            {
                input = default;
            }

            LastProcessedTick = inputTick;
            return input;
        }
    }
}
