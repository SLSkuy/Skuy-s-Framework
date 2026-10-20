using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 模拟注册槽：身份、角色、命令边沿；收集时把输入源快照转为命令。
    /// </summary>
    public sealed class SimulationEntity
    {
        private readonly SimulationCommandBuilder _commandBuilder;
        private IInputStateProvider _inputSource;

        #region 属性
        public EntityIdentity Identity { get; }
        public EntityCharacter Character { get; }
        public EntityCommand CurrentTickInput { get; private set; }
        #endregion

        public SimulationEntity(EntityIdentity identity, EntityCharacter character)
        {
            _commandBuilder = new SimulationCommandBuilder();
            Character = character;
            Identity = identity;
        }

        /// <summary>
        /// 获取已经处理了的客户端输入Tick
        /// </summary>
        public uint GetLastProcessInputTick()
        {
            if (_inputSource is AuthorityInputProvider input)
            {
                return input.LastProcessedTick;
            }

            return 0;
        }

        /// <summary>
        /// 绑定或解绑本拍意图来源。
        /// </summary>
        public void SetInputSource(IInputStateProvider inputSource)
        {
            _inputSource = inputSource;
        }

        /// <summary>
        /// 从意图来源取快照并转为命令。权威输入还没到时返回 false，这一拍不步进。
        /// 无来源时按空快照构建。
        /// </summary>
        public bool TryCollectCommand(out EntityCommand command)
        {
            InputState input;
            if (_inputSource is AuthorityInputProvider authority)
            {
                // 远端输入
                if (!authority.TryGetInputState(out input))
                {
                    command = default;
                    return false;
                }
            }
            else
            {
                // 本地输入
                input = _inputSource?.GetInputState() ?? default;
            }

            command = _commandBuilder.Build(input);
            CurrentTickInput = command;
            return true;
        }
    }
}
