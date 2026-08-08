using Framework.FiniteStateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家地面基础状态，所有地面状态继承此状态
    /// </summary>
    public abstract class PlayerGroundedState : PlayerBaseState
    {
        protected PlayerGroundedState(StateMachine<PlayerState> stateMachine) : base(stateMachine) { }
    }
}