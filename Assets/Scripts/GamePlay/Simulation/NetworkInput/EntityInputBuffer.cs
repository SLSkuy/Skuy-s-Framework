using System;
using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 角色输入缓冲：服务端按客户端 Tick 做窗口校验与顺序消费，并构建每 Tick 命令
    /// LastProcessedTick 为已消费的客户端输入序号，0 表示尚未消费；下一拍固定取 LastProcessedTick + 1
    /// 下一拍还没到时不推进确认点；连续缺席超过上限后才消费一拍空指令
    /// 环形槽位以 0 表示空，有效输入 Tick 从 1 起
    /// </summary>
    public sealed class EntityInputBuffer
    {
        private const int MaxMissedInputTicks = 8;

        private readonly SimulationCommandQueue<InputCommand> _simulationCommands;
        private readonly SimulationCommandBuilder _commandBuilder;
        private readonly int _maxFutureInputTicks;
        private int _missedInputTicks;

        #region 属性
        public uint LastProcessedTick { get; private set; }
        public bool HasQueued => _simulationCommands.Count > 0;
        #endregion

        public EntityInputBuffer(int capacity, int maxFutureInputTicks)
        {
            if (maxFutureInputTicks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFutureInputTicks));
            }

            int resolvedCapacity = Math.Max(2, capacity);
            _commandBuilder = new SimulationCommandBuilder();
            _simulationCommands = new SimulationCommandQueue<InputCommand>(resolvedCapacity);
            _maxFutureInputTicks = Math.Min(Math.Max(1, maxFutureInputTicks), resolvedCapacity);
        }

        /// <summary>
        /// 将经过合法性校验的输入按客户端 Tick 加入服务端命令缓冲。
        /// 尚未消费过时，以首包序号对齐消费点，避免握手期间丢掉的快通道包把后续命令全部挤出窗口。
        /// </summary>
        public bool Enqueue(uint inputTick, in InputCommand input)
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

            return _simulationCommands.TryEnqueue(inputTick, input);
        }

        /// <summary>
        /// 根据已采样输入构建预测命令，服务端进行预测，用于无输入时的逻辑处理
        /// </summary>
        public EntityCommand BuildPredictedCommand(in InputCommand input)
        {
            return _commandBuilder.Build(input);
        }

        /// <summary>
        /// 清空缓冲并重置消费序号与边沿检测。
        /// </summary>
        public void Reset()
        {
            _simulationCommands.Clear();
            _commandBuilder.Reset();
            LastProcessedTick = 0;
            _missedInputTicks = 0;
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
        /// 消费 LastProcessedTick 之后的下一序号。
        /// 槽位还空着时不推进确认点；连续缺席超过上限后消费一拍空指令并推进。
        /// </summary>
        public bool TryConsumeNext(out InputCommand input)
        {
            uint inputTick = LastProcessedTick + 1;
            if (_simulationCommands.TryDequeue(inputTick, out input))
            {
                LastProcessedTick = inputTick;
                _missedInputTicks = 0;
                return true;
            }

            _missedInputTicks++;
            if (_missedInputTicks <= MaxMissedInputTicks)
            {
                input = default;
                return false;
            }

            input = default;
            LastProcessedTick = inputTick;
            _missedInputTicks = 0;
            return true;
        }
    }
}
