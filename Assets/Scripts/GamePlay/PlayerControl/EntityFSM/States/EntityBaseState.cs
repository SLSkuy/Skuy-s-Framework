using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体最基础状态，包含所有状态共有属性
    /// </summary>
    public abstract class EntityBaseState : ExtendableStateBase<uint>
    {
        protected EntityBaseState(ExtendableStateMachine<uint> stateMachine) : base(stateMachine) { }
    }
}