using System;
using System.Collections.Generic;
using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 输入采样、服务端命令队列和每 Tick 命令构建能力。
    /// </summary>
    public sealed class NetworkInputCapability : NetworkObjectCapabilityBase
    {
        private static readonly NetworkObjectCapabilityId[] Dependencies =
        {
            NetworkObjectCapabilityId.Simulation
        };

        private EntityCommandQueue<InputState> _commands;
        private EntityInputCommandBuilder _commandBuilder;
        private PlayerController _controller;

        #region 属性
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.InputCommand;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.InputCommand;
        public override IReadOnlyList<NetworkObjectCapabilityId> RequiredCapabilities => Dependencies;
        public uint LastProcessedTick => _commands?.LastProcessedTick ?? 0;
        #endregion

        /// <inheritdoc />
        public override bool SupportsMode(EntitySimulationMode mode) => mode != EntitySimulationMode.Replica;

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
        /// 采样本地输入并构建预测命令。
        /// </summary>
        public EntityInputCommand BuildPredictedCommand(uint inputTick, out InputState input)
        {
            input = _controller.SampleInput();
            return _commandBuilder.Build(inputTick, input);
        }

        protected override void OnActivated(EntitySimulationMode mode)
        {
            _commands ??= new EntityCommandQueue<InputState>(
                Math.Max(2, SyncConfig.Instance.maxBufferedInputs));
            _commandBuilder ??= new EntityInputCommandBuilder();
            _commands.Clear();
            _commandBuilder.Reset();

            _controller = GetComponent<PlayerController>();
            bool requiresLocalInput = mode == EntitySimulationMode.LocalPlay || mode == EntitySimulationMode.Predict;
            if (!requiresLocalInput)
            {
                if (_controller != null) _controller.enabled = false;
                return;
            }

            BaseEntity entity = GetComponent<BaseEntity>();
            if (entity == null || _controller == null)
            {
                throw new InvalidOperationException(
                    $"网络对象 {name} 的 InputCommand 能力在 {mode} 模式下需要 BaseEntity 和 PlayerController。 ");
            }

            _controller.enabled = true;
            _controller.Init(entity, Identity, mode == EntitySimulationMode.LocalPlay);
        }

        protected override void OnDeactivated()
        {
            _commands?.Clear();
            _commandBuilder?.Reset();
            if (_controller != null) _controller.enabled = false;
            _controller = null;
        }
    }
}
