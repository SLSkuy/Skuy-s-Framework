using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体地面基础状态，所有地面状态继承此状态
    /// </summary>
    public abstract class EntityGroundedState : EntityBaseState
    {
        protected EntityGroundedState(ExtendableStateMachine<uint> stateMachine) : base(stateMachine) { }
    }
}