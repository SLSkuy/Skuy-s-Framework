using Framework;
using GamePlay.EntitySystem;

namespace GamePlay.Simulator
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
        #endregion

        public RegisteredEntity(EntityObjectIdentity identity, EntityCharacter character)
        {
            Identity = identity;
            Character = character;
            _commandBuilder = new EntityCommandBuilder();
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
        public EntityCommand CollectCommand(uint tick)
        {
            InputState input = _inputSource.GetInputState();
            return _commandBuilder.Build(tick, input);
        }
    }
}
