using Framework.FiniteStateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 玩家空中基础状态，所有空中状态继承此状态
    /// </summary>
    public abstract class PlayerAirborneState : PlayerBaseState
    {
        protected PlayerAirborneState(StateMachine<PlayerState> stateMachine) : base(stateMachine) { }
    }
}