using System;
using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 角色输入缓冲：服务端命令队列与每 Tick 命令构建。
    /// </summary>
    public sealed class CharacterInputBuffer
    {
        private readonly EntityCommandQueue<InputState> _commands;
        private readonly EntityInputCommandBuilder _commandBuilder = new();

        #region Properties
        public uint LastProcessedTick => _commands.LastProcessedTick;
        #endregion

        public CharacterInputBuffer(int capacity)
        {
            _commands = new EntityCommandQueue<InputState>(Math.Max(2, capacity));
        }

        /// <summary>
        /// 将经过合法性校验的输入加入服务端命令队列。
        /// </summary>
        public bool Enqueue(uint inputTick, uint serverTick, uint maxPastTicks, uint maxFutureTicks,
            in InputState input)
        {
            return _commands.Enqueue(inputTick, serverTick, maxPastTicks, maxFutureTicks, input);
        }

        /// <summary>
        /// 构建服务端当前 Tick 应执行的命令；缺失输入时使用空输入继续模拟。
        /// </summary>
        public EntityInputCommand BuildAuthorityCommand(uint tick)
        {
            _commands.TryDequeueExecutable(tick, out _, out InputState input);
            return _commandBuilder.Build(tick, input);
        }

        /// <summary>
        /// 根据已采样输入构建预测命令。
        /// </summary>
        public EntityInputCommand BuildPredictedCommand(uint inputTick, in InputState input)
        {
            return _commandBuilder.Build(inputTick, input);
        }

        /// <summary>
        /// 清空队列并重置边沿检测。
        /// </summary>
        public void Reset()
        {
            _commands.Clear();
            _commandBuilder.Reset();
        }
    }
}
