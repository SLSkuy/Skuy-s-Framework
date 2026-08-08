using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体停止状态
    /// </summary>
    public abstract class EntityStopState : EntityGroundedState
    {
        protected EntityStopState(ExtendableStateMachine<uint> stateMachine) : base(stateMachine) { }
    }
}