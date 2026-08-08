using Framework.StateMachine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体空中基础状态，所有空中状态继承此状态
    /// </summary>
    public abstract class EntityAirborneState : EntityBaseState
    {
        protected EntityAirborneState(ExtendableStateMachine<uint> stateMachine) : base(stateMachine) { }
    }
}