using Framework.FiniteStateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家最基础状态，包含所有状态共有属性
    /// 
    /// </summary>
    public abstract class PlayerBaseState : StateBase<PlayerState>
    {
        protected PlayerBaseState(StateMachine<PlayerState> stateMachine) : base(stateMachine) { }
    }
}