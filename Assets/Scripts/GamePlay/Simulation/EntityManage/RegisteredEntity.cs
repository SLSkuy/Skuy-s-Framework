using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.Simulation
{
    /// <summary>
    /// 模拟注册槽：身份、角色、命令边沿；收集时把输入源快照转为命令。
    /// </summary>
    public sealed class RegisteredEntity
    {
        private readonly EntityCommandBuilder _commandBuilder;
        private IInputStateProvider _inputSource;

        #region 属性
        public EntityObjectIdentity Identity { get; }
        public EntityCharacter Character { get; }
        public EntityCommand CurTickInput { get; private set; }
        #endregion

        public RegisteredEntity(EntityObjectIdentity identity, EntityCharacter character)
        {
            Identity = identity;
            Character = character;
            _commandBuilder = new EntityCommandBuilder();
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
        /// 从意图来源取快照并转为命令；无来源时按空快照构建。
        /// </summary>
        public EntityCommand CollectCommand()
        {
            InputState input = _inputSource?.GetInputState() ?? default;
            EntityCommand command = _commandBuilder.Build(input);
            CurTickInput = command;
            return command;
        }
    }
}
