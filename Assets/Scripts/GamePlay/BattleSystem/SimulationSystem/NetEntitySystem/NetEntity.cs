using Framework;

namespace GamePlay.BattleSystem
{
    /// <summary>
    /// 模拟注册槽：身份、角色、命令边沿；收集时把输入源快照转为命令。
    /// </summary>
    public sealed class NetEntity
    {
        private readonly SimulationCommandBuilder _commandBuilder;
        private IInputProvider _inputSource;

        #region 属性
        public PlayerIdentity Identity { get; }
        public EntityCharacter Character { get; }
        public EntityControlCommand CurrentTickInput { get; private set; }
        #endregion

        public NetEntity(PlayerIdentity identity, EntityCharacter character)
        {
            _commandBuilder = new SimulationCommandBuilder();
            Character = character;
            Identity = identity;
        }

        /// <summary>
        /// 获取已消费或超时丢弃的客户端输入序号；等待输入时保持不变，与世界 Tick 无关
        /// </summary>
        public uint GetLastProcessInputTick()
        {
            if (_inputSource is NetworkInputProvider input)
            {
                return input.LastProcessedTick;
            }

            return 0;
        }

        /// <summary>
        /// 绑定或解绑本拍意图来源。
        /// </summary>
        public void SetInputSource(IInputProvider inputSource)
        {
            _inputSource = inputSource;
        }

        /// <summary>
        /// 从意图来源取快照并转为本世界 Tick 的命令。
        /// 远端输入未到时使用空命令：停止输入驱动的移动与瞄准，不触发按钮边沿，世界仍步进。
        /// 等待期间保留真实输入的边沿基线，避免恢复输入后重复触发按下；无来源时按空快照构建。
        /// </summary>
        public EntityControlCommand CollectCommand()
        {
            InputCommand input;
            if (_inputSource is NetworkInputProvider authority)
            {
                // 远端输入
                if (!authority.TryGetInputCommand(out input))
                {
                    CurrentTickInput = default;
                    return CurrentTickInput;
                }
            }
            else
            {
                // 本地输入
                input = _inputSource?.GetInputCommand() ?? default;
            }

            CurrentTickInput = _commandBuilder.Build(input);
            return CurrentTickInput;
        }
    }
}
